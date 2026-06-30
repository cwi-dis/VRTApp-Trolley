using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using VRT.Pilots.Common;

namespace VRT.Pilots.Trolley
{
    public class ResearcherSetupController : MonoBehaviour
    {
        [Header("Researcher Panel (root)")]
        public GameObject researcherPanel;

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

        [Header("Scene Transition")]
        public NetworkTrigger beginStudyNetworkTrigger;

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
            if (!VRTPilotConfig.InstanceExists())
                Debug.LogError("ResearcherSetupController: VRTPilotConfig not found.");
            if (TrolleyGameState.Instance == null)
                Debug.LogError("ResearcherSetupController: TrolleyGameState not found.");

            soloButton.onClick.AddListener(()   => SetCondition(TrolleyResearcherConfig.Condition.Solo));
            pairedButton.onClick.AddListener(() => SetCondition(TrolleyResearcherConfig.Condition.Paired));

            var rels = new[]
            {
                TrolleyResearcherConfig.RelationshipType.Stranger,
                TrolleyResearcherConfig.RelationshipType.Close,
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
            beginStudyNetworkTrigger.OnTrigger.AddListener(ExecuteBeginStudy);

            if (relationshipPanel != null) relationshipPanel.SetActive(false);
            researcherPanel.SetActive(true);

            PreFillFromConfig();
        }

        void PreFillFromConfig()
        {
            if (!VRTPilotConfig.InstanceExists()) return;
            if (!VRTPilotConfig.Instance.HasResearcherConfig) return;
            var rc = VRTPilotConfig.Instance.researcherConfig;

            // Condition
            bool isPaired = rc.IsPaired;
            _conditionSelected = true;
            HighlightOne(new[] { soloButton, pairedButton }, isPaired ? 1 : 0);
            if (relationshipPanel != null) relationshipPanel.SetActive(isPaired);

            // Relationship (if paired)
            if (isPaired && !string.IsNullOrEmpty(rc.relationshipType))
            {
                int relIdx = rc.relationshipType == "Stranger" ? 0
                           : rc.relationshipType == "Close"    ? 1
                           : -1;
                if (relIdx >= 0) HighlightOne(relationshipButtons, relIdx);
            }

            // Scenario order — match by label
            for (int i = 0; i < scenarioOrders.Length; i++)
            {
                if (scenarioOrders[i].label == rc.scenarioOrderLabel)
                {
                    _orderSelected = true;
                    HighlightOne(orderButtons, i);
                    break;
                }
            }

            UpdateBeginButton();
        }

        void SetCondition(TrolleyResearcherConfig.Condition condition)
        {
            var rc = VRTPilotConfig.InstanceExists() ? VRTPilotConfig.Instance.researcherConfig : null;
            if (rc != null) rc.condition = condition.ToString();
            _conditionSelected = true;

            HighlightOne(new[] { soloButton, pairedButton },
                condition == TrolleyResearcherConfig.Condition.Solo ? 0 : 1);

            bool isPaired = condition == TrolleyResearcherConfig.Condition.Paired;
            if (relationshipPanel != null) relationshipPanel.SetActive(isPaired);
            if (!isPaired && rc != null)
                rc.relationshipType = "";

            UpdateBeginButton();
        }

        void SetRelationship(TrolleyResearcherConfig.RelationshipType rel, int index)
        {
            var rc = VRTPilotConfig.InstanceExists() ? VRTPilotConfig.Instance.researcherConfig : null;
            if (rc != null)
                rc.relationshipType = rel == TrolleyResearcherConfig.RelationshipType.NotApplicable
                    ? "" : rel.ToString();
            HighlightOne(relationshipButtons, index);
            UpdateBeginButton();
        }

        void SetScenarioOrder(int index)
        {
            var rc = VRTPilotConfig.InstanceExists() ? VRTPilotConfig.Instance.researcherConfig : null;
            if (rc != null && index < scenarioOrders.Length)
            {
                rc.scenarioOrder      = scenarioOrders[index].scenes;
                rc.scenarioOrderLabel = scenarioOrders[index].label;
            }
            _orderSelected = true;
            HighlightOne(orderButtons, index);
            UpdateBeginButton();
        }

        void ToggleExport()
        {
            _exportEnabled = !_exportEnabled;
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
            if (!VRTPilotConfig.InstanceExists()) return;
            var rc = VRTPilotConfig.Instance.researcherConfig;
            bool conditionOk    = _conditionSelected;
            bool orderOk        = _orderSelected;
            bool isPaired       = rc.IsPaired;
            bool relationshipOk = !isPaired || !string.IsNullOrEmpty(rc.relationshipType);
            beginStudyButton.interactable = conditionOk && orderOk && relationshipOk;
        }

        void BeginStudy()
        {
            researcherPanel.SetActive(false);
            beginStudyButton.interactable = false;
            beginStudyNetworkTrigger.Trigger();
        }

        void ExecuteBeginStudy()
        {
            TrolleyGameState.Instance?.ResetSession();
            string next = TrolleyGameState.Instance?.avatarSetupScene ?? "TrolleyAvatarSetup";
            PilotController.Instance.LoadNewScene(next);
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
