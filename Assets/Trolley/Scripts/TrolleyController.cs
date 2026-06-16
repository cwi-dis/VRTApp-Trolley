using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Orchestrator;
using VRT.OrchestratorComm;
using VRT.Pilots.Common;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Controls a single trolley scenario scene. State machine:
    /// Idle -> Narration -> Decision -> Outcome -> Transition
    ///
    /// Timer start: master sends TrolleyTimerStartMessage to all after narration
    /// and starts locally; non-master starts on receipt.
    /// Decision: timer expiry reads final toggleDecision.IsAction state.
    /// Inaction: each client handles timer expiry locally — both timers run in
    /// lockstep because they start from the same master broadcast.
    /// </summary>
    public class TrolleyController : MonoBehaviour
    {
        enum State { Idle, Narration, Decision, Outcome, Transition }

        [Header("Scene References")]
        [SerializeField] NarrationPlayer narrationPlayer;
        [SerializeField] DecisionTimer decisionTimer;
        [SerializeField] TrainControllerBase trainController;
        [SerializeField] TrolleyToggleDecision toggleDecision;
        [SerializeField] CCTVBlackout cctvBlackout;

        [Header("Scenario")]
        [Tooltip("Identifier written to the data log: bystander | driver | selfharm")]
        public string scenarioID = "unknown";

        [Tooltip("Tutorial/practice scene: no data is logged, and the outcome leads straight " +
                 "to the first real scenario instead of the questionnaire.")]
        public bool isTutorial = false;

        [Header("CCTV")]
        [Tooltip("Seconds after decision window closes before CCTV blackout triggers.")]
        [SerializeField] float blackoutDelay = 2f;

        [Header("Scene Transition")]
        [SerializeField] NetworkTrigger readyTrigger;
        [SerializeField] BarrierController transitionBarrier;
        [SerializeField] NetworkTrigger proceedTrigger;

        State _state = State.Idle;

        // Timestamp tracking for logging
        DateTime _narrationEndTime;
        DateTime _windowStartTime;

        // All button press attempts during the decision window
        readonly List<(string choice, long unixMs)> _attempts = new List<(string, long)>();

        void Awake()
        {
            var comm = VRTOrchestratorSingleton.Comm;
            if (comm == null) return;
            comm.RegisterEventType((MessageTypeID)TrolleyMsgID.TimerStart, typeof(TrolleyTimerStartMessage));
        }

        void OnEnable()
        {
            VRTOrchestratorSingleton.Comm?.Subscribe<TrolleyTimerStartMessage>(OnTimerStart);
        }

        void OnDisable()
        {
            VRTOrchestratorSingleton.Comm?.Unsubscribe<TrolleyTimerStartMessage>(OnTimerStart);
        }

        void Start()
        {
            // Allow testing individual scenes without going through Tutorial first
            if (!VRTPilotConfig.InstanceExists())
            {
                new GameObject("VRTPilotConfig").AddComponent<VRTPilotConfig>();
                Debug.LogWarning("[TrolleyController] VRTPilotConfig not found — created with defaults for standalone test.");
            }
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

            readyTrigger.OnTrigger.AddListener(transitionBarrier.Trigger);
            transitionBarrier.OnAllReady.AddListener(proceedTrigger.Trigger);
            proceedTrigger.OnTrigger.AddListener(ExecuteSceneLoad);

            narrationPlayer.OnNarrationComplete += OnNarrationComplete;
            decisionTimer.OnTimerExpired += OnWindowClose;

            if (toggleDecision != null)
                toggleDecision.OnToggled += isAction =>
                    _attempts.Add((isAction ? "B" : "A", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));

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
            _attempts.Clear();
            _state = State.Decision;
            trainController?.StartApproach();
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

        // ── Timer expired — read final toggle state or default to inaction ──

        void OnWindowClose()
        {
            if (_state != State.Decision) return;
            Debug.Log($"[TrolleyController] OnWindowClose — toggleDecision={toggleDecision}, isAction={toggleDecision?.IsAction}");
            if (cctvBlackout != null)
                Invoke(nameof(TriggerBlackout), blackoutDelay);
            if (toggleDecision != null && toggleDecision.IsAction)
                ApplyAction();
            else
                ApplyInaction();
        }

        void TriggerBlackout() => cctvBlackout.Blackout();

        // ── Outcome application ────────────────────────────────────────────

        void ApplyAction()
        {
            if (_state == State.Outcome || _state == State.Transition) return;
            _state = State.Outcome;

            DateTime windowEndTime = DateTime.Now;
            float rt = decisionTimer.GetElapsedTime();

            decisionTimer.Stop();
            toggleDecision?.SetInteractionEnabled(false);
            trainController.ExecuteAction();
            if (TrolleyGameState.Instance != null) TrolleyGameState.Instance.lastDecision = "action";

            if (!isTutorial)
                DataLogger.Instance?.LogDecision(
                    scenarioID, "action", rt,
                    _narrationEndTime, _windowStartTime, windowEndTime, _attempts);

            Invoke(nameof(TransitionOut), 5f);
        }

        void ApplyInaction()
        {
            if (_state == State.Outcome || _state == State.Transition) return;
            _state = State.Outcome;

            DateTime windowEndTime = DateTime.Now;
            float rt = decisionTimer.GetElapsedTime();

            decisionTimer.Stop();
            toggleDecision?.SetInteractionEnabled(false);
            trainController.ExecuteInaction();
            if (TrolleyGameState.Instance != null) TrolleyGameState.Instance.lastDecision = "inaction";

            if (!isTutorial)
                DataLogger.Instance?.LogDecision(
                    scenarioID, "inaction", rt,
                    _narrationEndTime, _windowStartTime, windowEndTime, _attempts);

            Invoke(nameof(TransitionOut), 5f);
        }

        // ── Scene transition ───────────────────────────────────────────────

        void TransitionOut()
        {
            if (_state == State.Transition) return;
            _state = State.Transition;
            // Tutorial is a practice run: it does not count as a completed scenario,
            // so the scenario index is left untouched (the first real scenario follows).
            if (!isTutorial && TrolleyGameState.Instance != null)
            {
                TrolleyGameState.Instance.lastCompletedScenarioID = scenarioID;
                TrolleyGameState.Instance.AdvanceScenario();
            }
            readyTrigger.Trigger();
        }

        void ExecuteSceneLoad()
        {
            string next;
            if (isTutorial)
            {
                // After practice, go to the first real scenario (index was not advanced).
                next = TrolleyGameState.Instance?.NextScenarioScene();
                if (string.IsNullOrEmpty(next))
                {
                    Debug.LogWarning("[TrolleyController] Tutorial finished but no next scenario in the order — " +
                                     "staying put (standalone test?).");
                    return;
                }
            }
            else
            {
                next = TrolleyGameState.Instance?.questionnaireScene ?? "TrolleyQuestionnaire";
            }
            PilotController.Instance.LoadNewScene(next);
        }

        // ── Network messages ──────────────────────────────────────────────

        void OnTimerStart(TrolleyTimerStartMessage msg)
        {
            if (msg.SenderId == VRTOrchestratorSingleton.Comm.SelfUser?.userId) return;
            decisionTimer.StartCountdown();
        }

    }
}
