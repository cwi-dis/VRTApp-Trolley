using UnityEngine;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Persistent singleton holding session configuration across scene transitions.
    /// Placed in the Tutorial scene; survives all subsequent scene loads.
    /// </summary>
    public class TrolleyGameState : MonoBehaviour
    {
        public static TrolleyGameState Instance { get; private set; }

        public enum Condition { Solo, Paired }
        public enum RelationshipType { NotApplicable, Stranger, Close }
        public enum AvatarBodyType { Masculine, Feminine }

        [Header("Scenario Sequence")]
        public int currentScenarioIndex = 0;

        [Header("Scene Names")]
        public string avatarSetupScene   = "TrolleyAvatarSetup";
        public string questionnaireScene = "TrolleyQuestionnaire";
        public string endScene = "VRTLoginManager";

        [Header("Local Avatar Configuration (set during avatar selection)")]
        public AvatarBodyType avatarBodyType = AvatarBodyType.Masculine;
        public int skinToneIndex = 0;    // 0–5
        public int hairColorIndex = 0;   // 0–5

        [Header("Remote Avatar Configuration (received from partner on confirm)")]
        public AvatarBodyType remoteAvatarBodyType = AvatarBodyType.Masculine;
        public int remoteSkinToneIndex = 0;
        public int remoteHairColorIndex = 0;

        [Header("Introspection")]
        public string lastCompletedScenarioID = "";
        public string lastDecision = "";    // "action" or "inaction"

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
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
            currentScenarioIndex = 0;
            lastCompletedScenarioID = "";
        }

        public string AvatarConfigString() =>
            $"body:{avatarBodyType},skin:{skinToneIndex},hair:{hairColorIndex}";
    }
}
