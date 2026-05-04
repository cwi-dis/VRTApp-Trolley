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
        public enum Gender { Male, Female }
        public enum RelationshipType { NotApplicable, Friend, Stranger, Acquaintance, Partner }

        [Header("Session Config (set by researcher before starting)")]
        public Condition condition = Condition.Solo;
        public int participantNumber = 0;
        public RelationshipType relationshipType = RelationshipType.NotApplicable;

        [Header("Scenario Sequence (set by researcher; order is counterbalanced)")]
        public string[] scenarioOrder = { "TrolleyBystander", "TrolleyDriver", "TrolleyOptional" };
        public int currentScenarioIndex = 0;

        [Header("Scene Names")]
        public string questionnaireScene = "TrolleyQuestionnaire";
        public string endScene = "VRTLoginManager";

        [Header("Introspection")]
        public string lastCompletedScenarioID = "";
        public string lastDecision = "";   // "action" or "inaction"
        public Gender localGender = Gender.Male;
        public string scenarioOrderLabel = "";

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public string NextScenarioScene()
        {
            if (currentScenarioIndex < scenarioOrder.Length)
                return scenarioOrder[currentScenarioIndex];
            return null;
        }

        public void AdvanceScenario()
        {
            currentScenarioIndex++;
        }

        public bool HasMoreScenarios() => currentScenarioIndex < scenarioOrder.Length;

        public void ResetSession()
        {
            currentScenarioIndex = 0;
            lastCompletedScenarioID = "";
        }
    }
}
