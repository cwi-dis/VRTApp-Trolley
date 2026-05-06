using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRT.Orchestrator.Wrapping;
using VRT.Orchestrator.Responses;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Controls a single trolley scenario scene. State machine:
    /// Idle -> Narration -> Decision -> Outcome -> Transition
    ///
    /// Decision sync: the client who triggers the physical action broadcasts
    /// "decision:action:<playerID>" via SendMessageToAll and also applies the
    /// outcome locally. The other client applies it on receipt.
    /// Attempt logging: every interaction attempt broadcasts "interaction:attempt:<playerID>:<unixMs>"
    /// so both clients accumulate the full attempt list for competition detection.
    /// Timer start: the master broadcasts "timer:start" after narration; master
    /// also starts locally (SendMessageToAll does not echo to sender).
    /// Inaction: each client handles timer expiry locally — both timers run in
    /// lockstep because they start from the same master broadcast.
    /// </summary>
    public class TrolleyController : MonoBehaviour
    {
        enum State { Idle, Narration, Decision, Outcome, Transition }

        [Header("Scene References")]
        [SerializeField] NarrationPlayer narrationPlayer;
        [SerializeField] DecisionTimer decisionTimer;
        [SerializeField] TrainController trainController;
        [SerializeField] TrolleyInteractable interactable;

        [Header("Scenario")]
        [Tooltip("Identifier written to the data log: bystander | driver | selfharm")]
        public string scenarioID = "unknown";

        State _state = State.Idle;

        // Timestamp tracking for logging
        DateTime _narrationEndTime;
        DateTime _windowStartTime;

        // Interaction attempt tracking for competition detection
        readonly List<InteractionAttempt> _interactionAttempts = new List<InteractionAttempt>();

        void Start()
        {
            OrchestratorController.Instance.OnUserMessageReceivedEvent += OnNetworkMessage;
            narrationPlayer.OnNarrationComplete += OnNarrationComplete;
            decisionTimer.OnTimerExpired += OnInaction;
            interactable.OnTriggered += OnLocalActionTriggered;

            // Self-harm asymmetric control: disable interactable for the non-controlling participant.
            bool isSelfHarm = scenarioID == "selfharm";
            bool isPaired   = TrolleyGameState.Instance?.condition == TrolleyGameState.Condition.Paired;
            bool isMaster   = OrchestratorController.Instance.UserIsMaster;
            bool hasControl = !isSelfHarm || !isPaired || TrolleyGameState.Instance.IsSelfHarmController(isMaster);
            interactable.SetActive(false);
            if (!hasControl) interactable.enabled = false;

            _state = State.Narration;

            if (SceneFader.Instance != null)
                SceneFader.Instance.OnFadeInComplete += BeginNarration;
            else
                StartCoroutine(FallbackBegin());
        }

        IEnumerator FallbackBegin()
        {
            yield return new WaitForSeconds(2f);
            BeginNarration();
        }

        void BeginNarration()
        {
            if (SceneFader.Instance != null)
                SceneFader.Instance.OnFadeInComplete -= BeginNarration;
            trainController.StartApproach(narrationPlayer.TotalDuration);
            narrationPlayer.Play();
        }

        void OnDestroy()
        {
            if (OrchestratorController.Instance != null)
                OrchestratorController.Instance.OnUserMessageReceivedEvent -= OnNetworkMessage;
        }

        // ── Narration complete ─────────────────────────────────────────────

        void OnNarrationComplete()
        {
            _narrationEndTime = DateTime.Now;
            _windowStartTime  = DateTime.Now;
            _state = State.Decision;
            interactable.SetActive(true);
            if (OrchestratorController.Instance.UserIsMaster)
                OrchestratorController.Instance.SendMessageToAll("timer:start");
            decisionTimer.StartCountdown();
        }

        // ── Local physical interaction ─────────────────────────────────────

        void OnLocalActionTriggered()
        {
            if (_state != State.Decision) return;
            string myId  = OrchestratorController.Instance.SelfUser.userId;
            long   nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Record and broadcast this attempt before applying the decision,
            // so the remote client can include it in competition detection.
            _interactionAttempts.Add(new InteractionAttempt { participantId = myId, unixMs = nowMs });
            OrchestratorController.Instance.SendMessageToAll($"interaction:attempt:{myId}:{nowMs}");
            OrchestratorController.Instance.SendMessageToAll($"decision:action:{myId}");
            ApplyAction(myId);
        }

        // ── Timer expired (inaction) ───────────────────────────────────────

        void OnInaction()
        {
            if (_state != State.Decision) return;
            ApplyInaction();
        }

        // ── Outcome application ────────────────────────────────────────────

        void ApplyAction(string triggeredByPlayerId)
        {
            if (_state == State.Outcome || _state == State.Transition) return;
            _state = State.Outcome;

            DateTime windowEndTime = DateTime.Now;
            float rt = decisionTimer.GetElapsedTime();

            bool competitionFlag = false;
            if (_interactionAttempts.Count >= 2)
            {
                long t0 = _interactionAttempts[0].unixMs;
                long t1 = _interactionAttempts[1].unixMs;
                competitionFlag = Math.Abs(t1 - t0) <= 500;
            }

            decisionTimer.Stop();
            interactable.SetActive(false);
            trainController.ExecuteAction();
            if (TrolleyGameState.Instance != null) TrolleyGameState.Instance.lastDecision = "action";

            DataLogger.Instance.LogDecision(
                scenarioID, "action", triggeredByPlayerId, rt,
                _narrationEndTime, _windowStartTime, windowEndTime,
                _interactionAttempts, competitionFlag);

            Invoke(nameof(TransitionOut), 5f);
        }

        void ApplyInaction()
        {
            if (_state == State.Outcome || _state == State.Transition) return;
            _state = State.Outcome;

            DateTime windowEndTime = DateTime.Now;
            float rt = decisionTimer.GetElapsedTime();

            decisionTimer.Stop();
            interactable.SetActive(false);
            trainController.ExecuteInaction();
            if (TrolleyGameState.Instance != null) TrolleyGameState.Instance.lastDecision = "inaction";

            DataLogger.Instance.LogDecision(
                scenarioID, "inaction", "", rt,
                _narrationEndTime, _windowStartTime, windowEndTime,
                _interactionAttempts, false);

            Invoke(nameof(TransitionOut), 5f);
        }

        // ── Scene transition ───────────────────────────────────────────────

        void TransitionOut()
        {
            if (_state == State.Transition) return;
            _state = State.Transition;
            if (TrolleyGameState.Instance != null)
            {
                TrolleyGameState.Instance.lastCompletedScenarioID = scenarioID;
                TrolleyGameState.Instance.AdvanceScenario();
            }
            string next = TrolleyGameState.Instance?.questionnaireScene ?? "TrolleyQuestionnaire";
            if (SceneFader.Instance == null)
                new GameObject("SceneFader").AddComponent<SceneFader>();
            SceneFader.Instance.FadeToBlack(() => SceneManager.LoadScene(next));
        }

        // ── Network messages ──────────────────────────────────────────────

        void OnNetworkMessage(UserMessage msg)
        {
            if (msg.message.StartsWith("decision:action:"))
            {
                string playerID = msg.message.Substring("decision:action:".Length);
                ApplyAction(playerID);
            }
            else if (msg.message.StartsWith("interaction:attempt:"))
            {
                // Log remote attempt for competition detection; does not trigger the decision.
                string[] parts = msg.message.Split(':');
                if (parts.Length >= 4 && long.TryParse(parts[3], out long remoteMs))
                {
                    _interactionAttempts.Add(new InteractionAttempt
                    {
                        participantId = parts[2],
                        unixMs        = remoteMs
                    });
                }
            }
            else if (msg.message == "timer:start")
            {
                decisionTimer.StartCountdown();
            }
        }
    }
}
