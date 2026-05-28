using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRT.Orchestrator;
using VRT.OrchestratorComm;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Controls a single trolley scenario scene. State machine:
    /// Idle -> Narration -> Decision -> Outcome -> Transition
    ///
    /// Decision sync: the client who triggers the physical action sends a
    /// TrolleyActionMessage (via master if non-master) and also applies the outcome
    /// locally. The other client applies it on receipt.
    /// Attempt logging: the TrolleyActionMessage carries the timestamp so both
    /// clients accumulate the full attempt list for competition detection.
    /// Timer start: the master sends TrolleyTimerStartMessage to all after narration
    /// and starts locally; non-master starts on receipt.
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
        [SerializeField] TrolleyInteractable interactable;        // single-trigger (Driver / Selfharm)
        [SerializeField] TrolleyToggleDecision toggleDecision;    // toggle A/B (Bystander)
        [SerializeField] CCTVBlackout cctvBlackout;

        [Header("Scenario")]
        [Tooltip("Identifier written to the data log: bystander | driver | selfharm")]
        public string scenarioID = "unknown";

        State _state = State.Idle;

        // Timestamp tracking for logging
        DateTime _narrationEndTime;
        DateTime _windowStartTime;

        // Interaction attempt tracking for competition detection
        readonly List<InteractionAttempt> _interactionAttempts = new List<InteractionAttempt>();

        void Awake()
        {
            var comm = VRTOrchestratorSingleton.Comm;
            if (comm == null) return;
            comm.RegisterEventType((MessageTypeID)TrolleyMsgID.TimerStart, typeof(TrolleyTimerStartMessage));
            comm.RegisterEventType((MessageTypeID)TrolleyMsgID.Action,     typeof(TrolleyActionMessage));
        }

        void OnEnable()
        {
            VRTOrchestratorSingleton.Comm?.Subscribe<TrolleyTimerStartMessage>(OnTimerStart);
            VRTOrchestratorSingleton.Comm?.Subscribe<TrolleyActionMessage>(OnRemoteAction);
        }

        void OnDisable()
        {
            VRTOrchestratorSingleton.Comm?.Unsubscribe<TrolleyTimerStartMessage>(OnTimerStart);
            VRTOrchestratorSingleton.Comm?.Unsubscribe<TrolleyActionMessage>(OnRemoteAction);
        }

        void Start()
        {
            // Allow testing individual scenes without going through Tutorial first
            if (TrolleyGameState.Instance == null)
            {
                new GameObject("TrolleyGameState").AddComponent<TrolleyGameState>();
                Debug.LogWarning("[TrolleyController] TrolleyGameState not found — created for standalone test.");
            }
            if (DataLogger.Instance == null)
            {
                new GameObject("DataLogger").AddComponent<DataLogger>();
                Debug.LogWarning("[TrolleyController] DataLogger not found — created for standalone test.");
            }

            narrationPlayer.OnNarrationComplete += OnNarrationComplete;
            decisionTimer.OnTimerExpired += OnWindowClose;

            if (interactable != null)
            {
                interactable.OnTriggered += OnLocalActionTriggered;
                interactable.SetActive(false);
            }

            toggleDecision?.SetInteractionEnabled(false);

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
            narrationPlayer.Play();
        }

        // ── Narration complete ─────────────────────────────────────────────

        void OnNarrationComplete()
        {
            _narrationEndTime = DateTime.Now;
            _windowStartTime  = DateTime.Now;
            _state = State.Decision;
            trainController?.StartApproach();
            interactable?.SetActive(true);
            toggleDecision?.SetInteractionEnabled(true);
            var comm = VRTOrchestratorSingleton.Comm;
            bool hasSession = comm != null && comm.SelfUser != null;
            if (!hasSession || comm.UserIsMaster)
            {
                // Solo (no session) or network master: own the timer
                if (hasSession) comm.SendTypeEventToAll(new TrolleyTimerStartMessage());
                decisionTimer.StartCountdown();
            }
            // non-master starts timer on receipt of TrolleyTimerStartMessage
        }

        // ── Local physical interaction ─────────────────────────────────────

        void OnLocalActionTriggered()
        {
            if (_state != State.Decision) return;
            var  comm  = VRTOrchestratorSingleton.Comm;
            string myId  = comm?.SelfUser?.userId ?? "solo";
            long   nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            _interactionAttempts.Add(new InteractionAttempt { participantId = myId, unixMs = nowMs });
            var actionMsg = new TrolleyActionMessage { triggeredByPlayerId = myId, unixMs = nowMs };
            if (comm == null || comm.UserIsMaster)
                comm?.SendTypeEventToAll(actionMsg);
            else
                comm.SendTypeEventToMaster(actionMsg);
            ApplyAction(myId);
        }

        // ── Timer expired — read final toggle state or default to inaction ──

        void OnWindowClose()
        {
            if (_state != State.Decision) return;
            Debug.Log($"[TrolleyController] OnWindowClose — toggleDecision={toggleDecision}, isAction={toggleDecision?.IsAction}");
            if (toggleDecision != null && toggleDecision.IsAction)
            {
                string playerId = VRTOrchestratorSingleton.Comm?.SelfUser?.userId ?? "solo";
                ApplyAction(playerId);
            }
            else
            {
                ApplyInaction();
            }
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
            interactable?.SetActive(false);
            toggleDecision?.SetInteractionEnabled(false);
            cctvBlackout?.Blackout();
            trainController.ExecuteAction();
            if (TrolleyGameState.Instance != null) TrolleyGameState.Instance.lastDecision = "action";

            DataLogger.Instance?.LogDecision(
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
            interactable?.SetActive(false);
            toggleDecision?.SetInteractionEnabled(false);
            cctvBlackout?.Blackout();
            trainController.ExecuteInaction();
            if (TrolleyGameState.Instance != null) TrolleyGameState.Instance.lastDecision = "inaction";

            DataLogger.Instance?.LogDecision(
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

        void OnTimerStart(TrolleyTimerStartMessage msg)
        {
            if (msg.SenderId == VRTOrchestratorSingleton.Comm.SelfUser?.userId) return;
            decisionTimer.StartCountdown();
        }

        void OnRemoteAction(TrolleyActionMessage msg)
        {
            if (VRTOrchestratorSingleton.Comm.UserIsMaster)
                VRTOrchestratorSingleton.Comm.SendTypeEventToAll(msg, true);
            if (msg.SenderId == VRTOrchestratorSingleton.Comm.SelfUser?.userId) return;
            _interactionAttempts.Add(new InteractionAttempt { participantId = msg.triggeredByPlayerId, unixMs = msg.unixMs });
            ApplyAction(msg.triggeredByPlayerId);
        }
    }
}
