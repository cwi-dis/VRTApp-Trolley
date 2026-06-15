using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using TMPro;
using VRT.Pilots.Common;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Run once via menu: Trolley > Wire Researcher Setup Scene
    /// Single combined researcher canvas: condition,
    /// relationship (paired only), scenario order, Begin Study.
    /// </summary>
    public static class TrolleyResearcherSetupSceneSetup
    {
        const string ScenePath = "Assets/Trolley/Scenes/TrolleyResearcherSetup.unity";

        static readonly string[] OrderLabels = { "B→D→S", "B→S→D", "D→B→S", "D→S→B", "S→B→D", "S→D→B" };
        static readonly string[][] OrderScenes =
        {
            new[] { "TrolleyBystander", "TrolleyDriver",    "TrolleySelfharm"  },
            new[] { "TrolleyBystander", "TrolleySelfharm",  "TrolleyDriver"    },
            new[] { "TrolleyDriver",    "TrolleyBystander", "TrolleySelfharm"  },
            new[] { "TrolleyDriver",    "TrolleySelfharm",  "TrolleyBystander" },
            new[] { "TrolleySelfharm",  "TrolleyBystander", "TrolleyDriver"    },
            new[] { "TrolleySelfharm",  "TrolleyDriver",    "TrolleyBystander" },
        };

        static readonly string[] RelationshipLabels = { "Stranger", "Close" };

        [MenuItem("Trolley/Wire Researcher Setup Scene")]
        public static void WireTutorialScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            foreach (string name in new[] {
                "ResearcherSetupController", "AvatarSelector",
                "ResearcherCanvas", "AvatarCanvas",
                "PracticeButton",
                "TrolleyGameState", "DataLogger",
                "BeginStudyNetworkTrigger",
                "TutorialController" })  // legacy name from before scene was renamed
            {
                var existing = GameObject.Find(name);
                if (existing != null) Object.DestroyImmediate(existing);
            }

            // Defensive sweep: destroy any stray ResearcherSetupController components
            // left on GameObjects that don't appear in the teardown list above.
            foreach (var stray in Object.FindObjectsByType<ResearcherSetupController>(FindObjectsSortMode.None))
                Object.DestroyImmediate(stray.gameObject);

            // ── Persistent singletons ─────────────────────────────────────────
            var gameStateGO = new GameObject("TrolleyGameState");
            gameStateGO.AddComponent<ManagedBySetupScript>().menuItem = "Trolley/Wire Researcher Setup Scene";
            gameStateGO.AddComponent<TrolleyGameState>();

            var dataLoggerGO = new GameObject("DataLogger");
            dataLoggerGO.AddComponent<ManagedBySetupScript>().menuItem = "Trolley/Wire Researcher Setup Scene";
            dataLoggerGO.AddComponent<DataLogger>();

            // ── Single combined canvas ─────────────────────────────────────────
            var canvas = BuildCanvas("ResearcherCanvas",
                position: new Vector3(0f, 1.6f, 2f),
                size: new Vector2(960f, 860f));
            canvas.AddComponent<ManagedBySetupScript>().menuItem = "Trolley/Wire Researcher Setup Scene";

            var panel = CreatePanel("ResearcherPanel", canvas, new Color(0.08f, 0.08f, 0.08f, 0.97f));

            // Title
            CreateLabel("Title", panel, "STUDY SETUP",
                new Vector2(0f, 0.93f), new Vector2(1f, 1f), 46);

            // ── Row: Condition ────────────────────────────────────────────────
            CreateLabel("ConditionLabel", panel, "Condition",
                new Vector2(0.02f, 0.840f), new Vector2(0.25f, 0.905f), 26);
            var soloBtn   = CreateButton("SoloButton",   panel, "Solo",
                new Vector2(0.26f, 0.840f), new Vector2(0.52f, 0.905f));
            var pairedBtn = CreateButton("PairedButton", panel, "Paired",
                new Vector2(0.54f, 0.840f), new Vector2(0.80f, 0.905f));

            // ── Row: Relationship (hidden until Paired) ───────────────────────
            var relPanel = new GameObject("RelationshipPanel");
            relPanel.transform.SetParent(panel.transform, false);
            var relRect = relPanel.AddComponent<RectTransform>();
            relRect.anchorMin = new Vector2(0.02f, 0.770f);
            relRect.anchorMax = new Vector2(0.98f, 0.835f);
            relRect.offsetMin = relRect.offsetMax = Vector2.zero;

            CreateLabel("RelLabel", relPanel, "Relationship",
                new Vector2(0f, 0f), new Vector2(0.26f, 1f), 26);
            var relButtons = new Button[2];
            for (int i = 0; i < 2; i++)
            {
                float x0 = 0.27f + i * 0.37f;
                relButtons[i] = CreateButton($"Rel_{i}", relPanel, RelationshipLabels[i],
                    new Vector2(x0, 0f), new Vector2(x0 + 0.35f, 1f), 26);
            }

            // ── Row: Scenario order (2 rows of 3) ─────────────────────────────
            CreateLabel("OrderLabel", panel, "Scenario Order",
                new Vector2(0.02f, 0.710f), new Vector2(0.98f, 0.765f), 26);
            var orderButtons = new Button[6];
            for (int i = 0; i < 6; i++)
            {
                int col = i % 3, row = i / 3;
                float x0 = 0.02f + col * 0.328f;
                float y1 = 0.710f - row * 0.075f;
                orderButtons[i] = CreateButton($"Order_{i}", panel, OrderLabels[i],
                    new Vector2(x0, y1 - 0.065f), new Vector2(x0 + 0.318f, y1), 24);
            }

            // ── Export toggle + Begin Study ───────────────────────────────────
            var exportBtn = CreateButton("ExportToggleButton", panel, "Export: OFF",
                new Vector2(0.02f, 0.02f), new Vector2(0.38f, 0.1f), fontSize: 26);
            var exportLabel = exportBtn.GetComponentInChildren<TMPro.TextMeshProUGUI>();

            var beginBtn = CreateButton("BeginStudyButton", panel, "BEGIN STUDY",
                new Vector2(0.42f, 0.02f), new Vector2(0.98f, 0.1f),
                bgColor: new Color(0.1f, 0.55f, 0.1f), fontSize: 36);

            // ── ResearcherSetupController ─────────────────────────────────────────────
            var controllerGO = new GameObject("ResearcherSetupController");
            controllerGO.AddComponent<ManagedBySetupScript>().menuItem = "Trolley/Wire Researcher Setup Scene";
            var controller   = controllerGO.AddComponent<ResearcherSetupController>();

            controller.researcherPanel   = panel;
            controller.soloButton        = soloBtn;
            controller.pairedButton      = pairedBtn;
            controller.beginStudyButton  = beginBtn;
            controller.relationshipPanel = relPanel;
            controller.exportToggleButton = exportBtn;
            controller.exportToggleLabel      = exportLabel;
            controller.relationshipButtons    = relButtons;
            controller.orderButtons           = orderButtons;

            controller.scenarioOrders = new ResearcherSetupController.ScenarioOrder[6];
            for (int i = 0; i < 6; i++)
            {
                controller.scenarioOrders[i] = new ResearcherSetupController.ScenarioOrder
                {
                    label  = OrderLabels[i],
                    scenes = (string[])OrderScenes[i].Clone(),
                };
            }

            EditorUtility.SetDirty(controller);

            // ── NetworkTrigger for coordinated Begin Study transition ──────────
            var triggerGO = new GameObject("BeginStudyNetworkTrigger");
            triggerGO.AddComponent<ManagedBySetupScript>().menuItem = "Trolley/Wire Researcher Setup Scene";
            var networkTrigger = triggerGO.AddComponent<NetworkTrigger>();
            controller.beginStudyNetworkTrigger = networkTrigger;
            EditorUtility.SetDirty(networkTrigger);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("TrolleyResearcherSetupSceneSetup: TrolleyResearcherSetup scene wired and saved.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        static GameObject BuildCanvas(string name, Vector3 position, Vector2 size)
        {
            var go = new GameObject(name);
            go.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            go.AddComponent<TrackedDeviceGraphicRaycaster>();
            go.GetComponent<RectTransform>().sizeDelta = size;
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            go.transform.localScale = Vector3.one * 0.003f;
            return go;
        }

        static GameObject CreatePanel(string name, GameObject parent, Color bg)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            go.AddComponent<Image>().color = bg;
            return go;
        }

        static void CreateLabel(string name, GameObject parent, string text,
            Vector2 min, Vector2 max, float fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = min; r.anchorMax = max;
            r.offsetMin = r.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
        }

        static TMPro.TMP_Dropdown CreateDropdown(string name, GameObject parent,
            Vector2 min, Vector2 max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = min; r.anchorMax = max;
            r.offsetMin = r.offsetMax = Vector2.zero;
            go.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.4f);
            var dropdown = go.AddComponent<TMPro.TMP_Dropdown>();
            // Label child
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var lr = labelGO.AddComponent<RectTransform>();
            lr.anchorMin = new Vector2(0.05f, 0f); lr.anchorMax = new Vector2(0.85f, 1f);
            lr.offsetMin = lr.offsetMax = Vector2.zero;
            var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text = "—"; labelTMP.fontSize = 28; labelTMP.color = Color.white;
            labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
            dropdown.captionText = labelTMP;
            return dropdown;
        }

        static TextMeshProUGUI CreateTMPLabel(string name, GameObject parent, string text,
            Vector2 min, Vector2 max, float fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = min; r.anchorMax = max;
            r.offsetMin = r.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fontSize;
            tmp.color = Color.white;
            return tmp;
        }

        static Button CreateButton(string name, GameObject parent, string label,
            Vector2 min, Vector2 max, float fontSize = 30, Color? bgColor = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = min; r.anchorMax = max;
            r.offsetMin = r.offsetMax = Vector2.zero;
            go.AddComponent<Image>().color = bgColor ?? new Color(0.2f, 0.2f, 0.5f);
            var btn = go.AddComponent<Button>();
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var lr = labelGO.AddComponent<RectTransform>();
            lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
            lr.offsetMin = lr.offsetMax = Vector2.zero;
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = fontSize;
            tmp.color = Color.white; tmp.alignment = TextAlignmentOptions.Center;
            return btn;
        }

        static void SetButtonArray(SerializedObject so, string field, Button[] buttons)
        {
            var prop = so.FindProperty(field);
            prop.arraySize = buttons.Length;
            for (int i = 0; i < buttons.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
        }
    }
}
