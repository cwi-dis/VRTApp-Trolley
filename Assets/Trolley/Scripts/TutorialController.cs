using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace VRT.Pilots.Trolley
{
    public class TutorialController : MonoBehaviour
    {
        [Header("Researcher Panel (root)")]
        public GameObject researcherPanel;

        [Header("Participant Number")]
        public Button participantMinusButton;
        public Button participantPlusButton;
        public TextMeshProUGUI participantDisplay;

        [Header("Avatar")]
        public AvatarSelector avatarSelector;

        [Header("Condition")]
        public Button soloButton;
        public Button pairedButton;

        [Header("Relationship — shown only in Paired mode")]
        public GameObject relationshipPanel;
        public Button[] relationshipButtons;

        [Header("Scenario Order")]
        public Button[] orderButtons;
        public ScenarioOrder[] scenarioOrders;

        [Header("Export & Begin")]
        public Button exportToggleButton;
        public TextMeshProUGUI exportToggleLabel;
        public Button beginStudyButton;

        [System.Serializable]
        public struct ScenarioOrder
        {
            public string label;
            public string[] scenes;
        }

        static readonly Color DefaultColor  = new Color(0.2f, 0.2f, 0.5f);
        static readonly Color SelectedColor = new Color(0.1f, 0.6f, 0.1f);
        static readonly Color ExportOnColor = new Color(0.6f, 0.35f, 0.0f);

        bool _conditionSelected = false;
        bool _orderSelected     = false;
        bool _exportEnabled     = false;

        void Start()
        {
            if (TrolleyGameState.Instance == null)
                Debug.LogError("TutorialController: TrolleyGameState not found.");

            participantMinusButton.onClick.AddListener(DecrementParticipant);
            participantPlusButton.onClick.AddListener(IncrementParticipant);
            UpdateParticipantDisplay();

            soloButton.onClick.AddListener(()   => SetCondition(TrolleyGameState.Condition.Solo));
            pairedButton.onClick.AddListener(() => SetCondition(TrolleyGameState.Condition.Paired));

            var rels = new[]
            {
                TrolleyGameState.RelationshipType.Friend,
                TrolleyGameState.RelationshipType.Stranger,
                TrolleyGameState.RelationshipType.Acquaintance,
                TrolleyGameState.RelationshipType.Partner,
            };
            for (int i = 0; i < relationshipButtons.Length && i < rels.Length; i++)
            {
                var rel = rels[i];
                int idx = i;
                relationshipButtons[i].onClick.AddListener(() => SetRelationship(rel, idx));
            }

            for (int i = 0; i < orderButtons.Length; i++)
            {
                int captured = i;
                orderButtons[i].onClick.AddListener(() => SetScenarioOrder(captured));
            }

            exportToggleButton.onClick.AddListener(ToggleExport);
            UpdateExportButton();

            beginStudyButton.onClick.AddListener(BeginStudy);
            beginStudyButton.interactable = false;

            if (relationshipPanel != null) relationshipPanel.SetActive(false);
            researcherPanel.SetActive(true);
        }

        void IncrementParticipant()
        {
            int n = Mathf.Min((TrolleyGameState.Instance?.participantNumber ?? 0) + 1, 30);
            if (TrolleyGameState.Instance != null) TrolleyGameState.Instance.participantNumber = n;
            UpdateParticipantDisplay();
            UpdateBeginButton();
        }

        void DecrementParticipant()
        {
            int n = Mathf.Max((TrolleyGameState.Instance?.participantNumber ?? 0) - 1, 0);
            if (TrolleyGameState.Instance != null) TrolleyGameState.Instance.participantNumber = n;
            UpdateParticipantDisplay();
            UpdateBeginButton();
        }

        void UpdateParticipantDisplay()
        {
            int n = TrolleyGameState.Instance?.participantNumber ?? 0;
            if (participantDisplay != null)
                participantDisplay.text = n > 0 ? n.ToString() : "—";
        }

        void SetCondition(TrolleyGameState.Condition condition)
        {
            if (TrolleyGameState.Instance != null)
                TrolleyGameState.Instance.condition = condition;
            _conditionSelected = true;

            HighlightOne(new[] { soloButton, pairedButton },
                condition == TrolleyGameState.Condition.Solo ? 0 : 1);

            bool isPaired = condition == TrolleyGameState.Condition.Paired;
            if (relationshipPanel != null) relationshipPanel.SetActive(isPaired);
            if (!isPaired && TrolleyGameState.Instance != null)
                TrolleyGameState.Instance.relationshipType =
                    TrolleyGameState.RelationshipType.NotApplicable;

            UpdateBeginButton();
        }

        void SetRelationship(TrolleyGameState.RelationshipType rel, int index)
        {
            if (TrolleyGameState.Instance != null)
                TrolleyGameState.Instance.relationshipType = rel;
            HighlightOne(relationshipButtons, index);
            UpdateBeginButton();
        }

        void SetScenarioOrder(int index)
        {
            if (TrolleyGameState.Instance != null && index < scenarioOrders.Length)
            {
                TrolleyGameState.Instance.scenarioOrder      = scenarioOrders[index].scenes;
                TrolleyGameState.Instance.scenarioOrderLabel = scenarioOrders[index].label;
            }
            _orderSelected = true;
            HighlightOne(orderButtons, index);
            UpdateBeginButton();
        }

        void ToggleExport()
        {
            _exportEnabled = !_exportEnabled;
            DataLogger.Instance?.SetExportEnabled(_exportEnabled);
            UpdateExportButton();
        }

        void UpdateExportButton()
        {
            var img = exportToggleButton?.GetComponent<Image>();
            if (img != null)
                img.color = _exportEnabled ? ExportOnColor : DefaultColor;
            if (exportToggleLabel != null)
                exportToggleLabel.text = _exportEnabled ? "Export: ON" : "Export: OFF";
        }

        void UpdateBeginButton()
        {
            if (TrolleyGameState.Instance == null) return;
            bool participantOk  = TrolleyGameState.Instance.participantNumber > 0;
            bool conditionOk    = _conditionSelected;
            bool orderOk        = _orderSelected;
            bool isPaired       = TrolleyGameState.Instance.condition == TrolleyGameState.Condition.Paired;
            bool relationshipOk = !isPaired ||
                TrolleyGameState.Instance.relationshipType != TrolleyGameState.RelationshipType.NotApplicable;
            beginStudyButton.interactable = participantOk && conditionOk && orderOk && relationshipOk;
        }

        void BeginStudy()
        {
            DataLogger.Instance?.StartSession();
            researcherPanel.SetActive(false);
            TrolleyGameState.Instance?.ResetSession();
            string first = TrolleyGameState.Instance?.NextScenarioScene();
            if (string.IsNullOrEmpty(first))
            {
                Debug.LogError("TutorialController: no scenario scene configured.");
                return;
            }
            SceneManager.LoadScene(first);
        }

        static void HighlightOne(Button[] buttons, int selectedIndex)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                var img = buttons[i]?.GetComponent<Image>();
                if (img != null)
                    img.color = i == selectedIndex ? SelectedColor : DefaultColor;
            }
        }
    }
}
