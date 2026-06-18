using UnityEngine;
using VRT.Orchestrator;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Persistent singleton holding session configuration across scene transitions.
    /// Placed in the Tutorial scene; survives all subsequent scene loads.
    /// </summary>
    public class TrolleyGameState : MonoBehaviour
    {
        public static TrolleyGameState Instance { get; private set; }

        [Header("Scenario Sequence")]
        public int currentScenarioIndex = 0;

        [Header("Scene Names")]
        public string avatarSetupScene   = "TrolleyAvatarSetup";
        [Tooltip("Participant practice scene, loaded once after avatar setup and before the first real scenario. Leave empty to skip.")]
        public string tutorialScene      = "TrolleyTutorialBystander";
        public string questionnaireScene = "TrolleyQuestionnaire";
        public string endScene = "VRTLoginManager";

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
            currentScenarioIndex = 0;
            lastCompletedScenarioID = "";
        }
    }
}
