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

        void Awake()
        {
            VRTOrchestratorSingleton.Comm.RegisterEventType((MessageTypeID)TrolleyMsgID.TimerStart, typeof(TrolleyTimerStartMessage));
            VRTOrchestratorSingleton.Comm.RegisterEventType((MessageTypeID)TrolleyMsgID.Action,     typeof(TrolleyActionMessage));
        }

        void OnEnable()
        {
            VRTOrchestratorSingleton.Comm.Subscribe<TrolleyTimerStartMessage>(OnTimerStart);
            VRTOrchestratorSingleton.Comm.Subscribe<TrolleyActionMessage>(OnRemoteAction);
        }

        void OnDisable()
        {
            VRTOrchestratorSingleton.Comm?.Unsubscribe<TrolleyTimerStartMessage>(OnTimerStart);
            VRTOrchestratorSingleton.Comm?.Unsubscribe<TrolleyActionMessage>(OnRemoteAction);
        }

        void Start()
        {
            narrationPlayer.OnNarrationComplete += OnNarrationComplete;
            decisionTimer.OnTimerExpired += OnInaction;
            interactable.OnTriggered += OnLocalActionTriggered;

            // Self-harm asymmetric control: disable interactable for the non-controlling participant.
            bool isSelfHarm = scenarioID == "selfharm";
            bool isPaired   = TrolleyGameState.Instance?.condition == TrolleyGameState.Condition.Paired;
            bool isMaster   = VRTOrchestratorSingleton.Comm.UserIsMaster;
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

        // ── Narration complete ─────────────────────────────────────────────

        void OnNarrationComplete()
        {
            _narrationEndTime = DateTime.Now;
            _windowStartTime  = DateTime.Now;
            _state = State.Decision;
            interactable.SetActive(true);
            if (VRTOrchestratorSingleton.Comm.UserIsMaster)
            {
                VRTOrchestratorSingleton.Comm.SendTypeEventToAll(new TrolleyTimerStartMessage());
                decisionTimer.StartCountdown();
            }
            // non-master starts timer on receipt of TrolleyTimerStartMessage
        }

        // ── Local physical interaction ─────────────────────────────────────

        void OnLocalActionTriggered()
        {
            if (_state != State.Decision) return;
            string myId  = VRTOrchestratorSingleton.Comm.SelfUser.userId;
            long   nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Record and broadcast this attempt before applying the decision,
            // so the remote client can include it in competition detection.
            _interactionAttempts.Add(new InteractionAttempt { participantId = myId, unixMs = nowMs });
            var actionMsg = new TrolleyActionMessage { triggeredByPlayerId = myId, unixMs = nowMs };
            if (VRTOrchestratorSingleton.Comm.UserIsMaster)
                VRTOrchestratorSingleton.Comm.SendTypeEventToAll(actionMsg);
            else
                VRTOrchestratorSingleton.Comm.SendTypeEventToMaster(actionMsg);
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
