using System.Collections;
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
    /// Decision sync: the client who triggers the physical action broadcasts
    /// "decision:action:<playerID>" via SendMessageToAll and also applies the
    /// outcome locally. The other client applies it on receipt.
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
        [Tooltip("Identifier written to the data log: bystander | driver | optional")]
        public string scenarioID = "unknown";

        State _state = State.Idle;

        void Start()
        {
#if xxxjack_needs_fixing
            VRTOrchestratorSingleton.Comm.OnUserMessageReceivedEvent += OnNetworkMessage;
#endif
            narrationPlayer.OnNarrationComplete += OnNarrationComplete;
            decisionTimer.OnTimerExpired += OnInaction;
            interactable.OnTriggered += OnLocalActionTriggered;
            interactable.SetActive(false);
            _state = State.Narration;

            // Wait for fade-in to complete before starting narration + train
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
#if xxxjack_needs_fixing
            if (VRTOrchestratorSingleton.Comm != null)
                VRTOrchestratorSingleton.Comm.OnUserMessageReceivedEvent -= OnNetworkMessage;
#endif
        }

        // ── Narration complete ─────────────────────────────────────────────

        void OnNarrationComplete()
        {
            _state = State.Decision;
            interactable.SetActive(true);
#if xxxjack_needs_fixing
            if (VRTOrchestratorSingleton.Comm.UserIsMaster)
                VRTOrchestratorSingleton.Comm.SendMessageToAll("timer:start");
#endif
            decisionTimer.StartCountdown();
        }

        // ── Local physical interaction ─────────────────────────────────────

        void OnLocalActionTriggered()
        {
            if (_state != State.Decision) return;
            string myId = VRTOrchestratorSingleton.Comm.SelfUser.userId;
#if xxxjack_needs_fixing
            VRTOrchestratorSingleton.Comm.SendMessageToAll($"decision:action:{myId}");
#endif
            ApplyAction(myId);
        }

        // ── Timer expired (inaction) ───────────────────────────────────────

        void OnInaction()
        {
            if (_state != State.Decision) return;
            ApplyInaction();
        }

        // ── Outcome application (called locally and on network receipt) ────

        void ApplyAction(string triggeredByPlayerId)
        {
            if (_state == State.Outcome || _state == State.Transition) return;
            _state = State.Outcome;
            float rt = decisionTimer.GetElapsedTime();
            decisionTimer.Stop();
            interactable.SetActive(false);
            trainController.ExecuteAction();
            if (TrolleyGameState.Instance != null) TrolleyGameState.Instance.lastDecision = "action";
            DataLogger.Instance.LogDecision(scenarioID, "action", triggeredByPlayerId, rt);
            Invoke(nameof(TransitionOut), 5f);
        }

        void ApplyInaction()
        {
            if (_state == State.Outcome || _state == State.Transition) return;
            _state = State.Outcome;
            decisionTimer.Stop();
            interactable.SetActive(false);
            trainController.ExecuteInaction();
            if (TrolleyGameState.Instance != null) TrolleyGameState.Instance.lastDecision = "inaction";
            DataLogger.Instance.LogDecision(scenarioID, "inaction", "", 5f);
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
            else if (msg.message == "timer:start")
            {
                decisionTimer.StartCountdown();
            }
        }

    }
}
