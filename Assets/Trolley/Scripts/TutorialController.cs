using UnityEngine;
using UnityEngine.UI;
using VRT.Pilots.Common;
using VRT.Orchestrator.Wrapping;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Controls the tutorial scene. Manages:
    ///   1. Researcher setup panel (condition + scenario order).
    ///   2. Avatar gender selection for each participant.
    ///   3. Practice interactables (lever + button with no consequences).
    ///   4. "Begin Study" button that loads the first scenario.
    ///
    /// The researcher configures the session on one headset; scenario order
    /// and condition are stored in TrolleyGameState (DontDestroyOnLoad).
    /// </summary>
    public class TutorialController : PilotController
    {
        [Header("Researcher Setup Panel")]
        [SerializeField] GameObject researcherPanel;
        [SerializeField] Button soloButton;
        [SerializeField] Button pairedButton;
        [Tooltip("Six buttons mapping to all permutations of 3 scenario names (ABC, ACB, BAC, BCA, CAB, CBA)")]
        [SerializeField] Button[] orderButtons;
        [Tooltip("Matching scenario orders for each order button")]
        [SerializeField] ScenarioOrder[] scenarioOrders;

        [Header("Avatar Selection Panel")]
        [SerializeField] AvatarSelector avatarSelector;

        [Header("Begin Study")]
        [SerializeField] Button beginStudyButton;

        [System.Serializable]
        public struct ScenarioOrder
        {
            public string label;
            public string[] scenes;
        }

        public override void Start()
        {
            base.Start();

            if (TrolleyGameState.Instance == null)
                Debug.LogError("TutorialController: TrolleyGameState not found. Add it to this scene.");

            soloButton.onClick.AddListener(() => SetCondition(TrolleyGameState.Condition.Solo));
            pairedButton.onClick.AddListener(() => SetCondition(TrolleyGameState.Condition.Paired));

            for (int i = 0; i < orderButtons.Length; i++)
            {
                int captured = i;
                orderButtons[i].onClick.AddListener(() => SetScenarioOrder(captured));
            }

            beginStudyButton.onClick.AddListener(BeginStudy);
            beginStudyButton.interactable = false;
            researcherPanel.SetActive(true);
        }

        void SetCondition(TrolleyGameState.Condition condition)
        {
            if (TrolleyGameState.Instance != null)
                TrolleyGameState.Instance.condition = condition;
            UpdateBeginButton();
        }

        void SetScenarioOrder(int index)
        {
            if (TrolleyGameState.Instance != null && index < scenarioOrders.Length)
                TrolleyGameState.Instance.scenarioOrder = scenarioOrders[index].scenes;
            UpdateBeginButton();
        }

        void UpdateBeginButton()
        {
            if (TrolleyGameState.Instance == null) return;
            bool ready = TrolleyGameState.Instance.scenarioOrder != null
                         && TrolleyGameState.Instance.scenarioOrder.Length > 0;
            beginStudyButton.interactable = ready;
        }

        void BeginStudy()
        {
            researcherPanel.SetActive(false);
            TrolleyGameState.Instance?.ResetSession();
            string first = TrolleyGameState.Instance?.NextScenarioScene();
            if (string.IsNullOrEmpty(first))
            {
                Debug.LogError("TutorialController: no scenario scene configured.");
                return;
            }
            LoadNewScene(first);
        }
    }
}
