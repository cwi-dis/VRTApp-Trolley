using UnityEngine;
using VRT.Orchestrator;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Persistent singleton holding session configuration across scene transitions.
    /// Created in TrolleyAvatarSetup (or on demand); survives all subsequent scene loads.
    ///
    /// Full session flow:
    ///   AvatarSetup → preSequence[0..n] → scenarioOrder[0], Questionnaire, scenarioOrder[1], … → endScene
    /// All scenes call NextScene() to get the next scene name; TrolleyController calls
    /// AdvanceScenario() after each main scenario completes.
    /// </summary>
    public class TrolleyGameState : MonoBehaviour
    {
        public static TrolleyGameState Instance { get; private set; }

        [Header("Fixed Pre-Sequence (after AvatarSetup, before scenarioOrder)")]
        // Driver tutorial first — it's the simpler first-person concept to grasp (Jack's call, issue #59).
        // NOTE: the tutorial narration's "first/second tutorial" wording and the button-teaching (currently
        // only in the bystander tutorial) still assume bystander-first — both need a content pass.
        public string[] preSequence = {
            "TrolleyTutorialDriver",
            "TrolleyTutorialBystander",
            "TrolleyPracticeQuestionnaire",
        };
        int _preSequenceIndex = 0;

        [Header("Main Sequence")]
        public int currentScenarioIndex = 0;

        [Header("Scene Names")]
        public string avatarSetupScene   = "TrolleyAvatarSetup";
        public string questionnaireScene = "TrolleyQuestionnaire";
        public string endScene           = "VRTLoginManager";

        [Header("Introspection")]
        public string lastCompletedScenarioID = "";
        public string lastDecision = "";    // "action" or "inaction"

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // xxxclaude derived from master/non-master; may need revisiting if the
        // orchestrator exposes a stable per-session player slot number.
        public static int LocalAvatarConfigIndex
        {
            get
            {
                var comm = VRTOrchestratorSingleton.Comm;
                bool hasSession = comm != null && comm.SelfUser != null;
                return (hasSession && !comm.UserIsMaster) ? 1 : 0;
            }
        }

        public static int OtherAvatarConfigIndex => 1 - LocalAvatarConfigIndex;

        public string NextScene()
        {
            if (_preSequenceIndex < preSequence.Length)
                return preSequence[_preSequenceIndex++];
            return NextScenarioScene();
        }

        public string NextScenarioScene()
        {
            var order = VRTPilotConfig.InstanceExists() ? VRTPilotConfig.Instance.researcherConfig.scenarioOrder : null;
            if (order == null || currentScenarioIndex >= order.Length) return null;
            return order[currentScenarioIndex];
        }

        public void AdvanceScenario() => currentScenarioIndex++;

        public bool HasMoreScenarios()
        {
            var order = VRTPilotConfig.InstanceExists() ? VRTPilotConfig.Instance.researcherConfig.scenarioOrder : null;
            return order != null && currentScenarioIndex < order.Length;
        }

        public void ResetSession()
        {
            _preSequenceIndex = 0;
            currentScenarioIndex = 0;
            lastCompletedScenarioID = "";
        }
    }
}
