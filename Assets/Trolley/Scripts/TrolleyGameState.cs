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
        public enum RelationshipType { NotApplicable, Stranger, Colleague, Friend, RomanticPartner }
        public enum AvatarBodyType { Masculine, Feminine }
        public enum AvatarHeight { Short, Medium, Tall }

        [Header("Session Config (set by researcher before starting)")]
        public Condition condition = Condition.Solo;
        public int participantNumber = 0;
        public RelationshipType relationshipType = RelationshipType.NotApplicable;

        [Header("Scenario Sequence (set by researcher; order is counterbalanced)")]
        public string[] scenarioOrder = { "TrolleyBystander", "TrolleyDriver", "TrolleySelfHarm" };
        public int currentScenarioIndex = 0;

        [Header("Scene Names")]
        public string avatarSetupScene   = "TrolleyAvatarSetup";
        public string questionnaireScene = "TrolleyQuestionnaire";
        public string endScene = "VRTLoginManager";

        [Header("Avatar Configuration (set during avatar selection)")]
        public AvatarBodyType avatarBodyType = AvatarBodyType.Masculine;
        public int skinToneIndex = 0;    // 0–5
        public int hairColorIndex = 0;   // 0–5
        public AvatarHeight avatarHeight = AvatarHeight.Medium;

        [Header("Self-harm Paired Control (counterbalanced per pair)")]
        [Tooltip("0 = Master player has the action control; 1 = Non-master player has it.")]
        public int selfHarmControllerSlot = 0;

        [Header("Introspection")]
        public string lastCompletedScenarioID = "";
        public string lastDecision = "";    // "action" or "inaction"
        public string scenarioOrderLabel = "";

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public string NextScenarioScene() =>
            currentScenarioIndex < scenarioOrder.Length ? scenarioOrder[currentScenarioIndex] : null;

        public void AdvanceScenario() => currentScenarioIndex++;

        public bool HasMoreScenarios() => currentScenarioIndex < scenarioOrder.Length;

        public void ResetSession()
        {
            currentScenarioIndex = 0;
            lastCompletedScenarioID = "";
        }

        /// <summary>Returns true if the local client controls the shared action in the self-harm scenario.</summary>
        public bool IsSelfHarmController(bool isMaster) =>
            (selfHarmControllerSlot == 0) == isMaster;

        public string AvatarConfigString() =>
            $"body:{avatarBodyType},skin:{skinToneIndex},hair:{hairColorIndex},height:{avatarHeight}";
    }
}
