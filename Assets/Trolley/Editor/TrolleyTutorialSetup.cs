using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using VRT.Pilots.Common;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Run once via menu: Trolley > Wire Tutorial Scene
    /// Single combined researcher canvas: participant #, avatar, condition,
    /// relationship (paired only), scenario order, Begin Study.
    /// </summary>
    public static class TrolleyTutorialSetup
    {
        const string ScenePath = "Assets/Trolley/Scenes/TrolleyTutorial.unity";

        static readonly string[] OrderLabels = { "B→D→O", "B→O→D", "D→B→O", "D→O→B", "O→B→D", "O→D→B" };
        static readonly string[][] OrderScenes =
        {
            new[] { "TrolleyBystander", "TrolleyDriver",    "TrolleyOptional"  },
            new[] { "TrolleyBystander", "TrolleyOptional",  "TrolleyDriver"    },
            new[] { "TrolleyDriver",    "TrolleyBystander", "TrolleyOptional"  },
            new[] { "TrolleyDriver",    "TrolleyOptional",  "TrolleyBystander" },
            new[] { "TrolleyOptional",  "TrolleyBystander", "TrolleyDriver"    },
            new[] { "TrolleyOptional",  "TrolleyDriver",    "TrolleyBystander" },
        };

        static readonly string[] RelationshipLabels = { "Friend", "Stranger", "Acquaintance", "Partner" };

        [MenuItem("Trolley/Wire Tutorial Scene")]
        public static void WireTutorialScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            foreach (string name in new[] {
                "TutorialController", "AvatarSelector",
                "ResearcherCanvas", "AvatarCanvas",
                "PracticeLever", "PracticeButton" })
            {
                var existing = GameObject.Find(name);
                if (existing != null) Object.DestroyImmediate(existing);
            }

            // ── Single combined canvas ─────────────────────────────────────────
            var canvas = BuildCanvas("ResearcherCanvas",
                position: new Vector3(0f, 1.6f, 2f),
                size: new Vector2(960f, 860f));

            var panel = CreatePanel("ResearcherPanel", canvas, new Color(0.08f, 0.08f, 0.08f, 0.97f));

            // Title
            CreateLabel("Title", panel, "STUDY SETUP",
                new Vector2(0f, 0.93f), new Vector2(1f, 1f), 46);

            // ── Row: Participant number (+/-) ──────────────────────────────────
            CreateLabel("ParticipantLabel", panel, "Participant #",
                new Vector2(0.02f, 0.845f), new Vector2(0.25f, 0.91f), 26);
            var minusBtn = CreateButton("ParticipantMinus", panel, "−",
                new Vector2(0.26f, 0.848f), new Vector2(0.38f, 0.908f), 40);
            var participantDisplay = CreateTMPLabel("ParticipantDisplay", panel, "—",
                new Vector2(0.39f, 0.848f), new Vector2(0.57f, 0.908f), 34);
            participantDisplay.alignment = TMPro.TextAlignmentOptions.Center;
            var plusBtn = CreateButton("ParticipantPlus", panel, "+",
                new Vector2(0.58f, 0.848f), new Vector2(0.70f, 0.908f), 40);

            // ── Row: Avatar ───────────────────────────────────────────────────
            CreateLabel("AvatarLabel", panel, "Avatar",
                new Vector2(0.02f, 0.775f), new Vector2(0.25f, 0.84f), 26);
            var maleBtn   = CreateButton("MaleButton",   panel, "Male",
                new Vector2(0.26f, 0.775f), new Vector2(0.52f, 0.84f));
            var femaleBtn = CreateButton("FemaleButton", panel, "Female",
                new Vector2(0.54f, 0.775f), new Vector2(0.80f, 0.84f));

            // ── Row: Condition ────────────────────────────────────────────────
            CreateLabel("ConditionLabel", panel, "Condition",
                new Vector2(0.02f, 0.705f), new Vector2(0.25f, 0.77f), 26);
            var soloBtn   = CreateButton("SoloButton",   panel, "Solo",
                new Vector2(0.26f, 0.705f), new Vector2(0.52f, 0.77f));
            var pairedBtn = CreateButton("PairedButton", panel, "Paired",
                new Vector2(0.54f, 0.705f), new Vector2(0.80f, 0.77f));

            // ── Row: Relationship (hidden until Paired) ───────────────────────
            var relPanel = new GameObject("RelationshipPanel");
            relPanel.transform.SetParent(panel.transform, false);
            var relRect = relPanel.AddComponent<RectTransform>();
            relRect.anchorMin = new Vector2(0.02f, 0.635f);
            relRect.anchorMax = new Vector2(0.98f, 0.70f);
            relRect.offsetMin = relRect.offsetMax = Vector2.zero;

            CreateLabel("RelLabel", relPanel, "Relationship",
                new Vector2(0f, 0f), new Vector2(0.26f, 1f), 26);
            var relButtons = new Button[4];
            for (int i = 0; i < 4; i++)
            {
                float x0 = 0.27f + i * 0.185f;
                relButtons[i] = CreateButton($"Rel_{i}", relPanel, RelationshipLabels[i],
                    new Vector2(x0, 0f), new Vector2(x0 + 0.175f, 1f), 24);
            }

            // ── Row: Scenario order (2 rows of 3) ─────────────────────────────
            CreateLabel("OrderLabel", panel, "Scenario Order",
                new Vector2(0.02f, 0.575f), new Vector2(0.98f, 0.63f), 26);
            var orderButtons = new Button[6];
            for (int i = 0; i < 6; i++)
            {
                int col = i % 3, row = i / 3;
                float x0 = 0.02f + col * 0.328f;
                float y1 = 0.575f - row * 0.075f;
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

            // ── AvatarSelector component ──────────────────────────────────────
            var avatarSelectorGO = new GameObject("AvatarSelector");
            var avatarSelector = avatarSelectorGO.AddComponent<AvatarSelector>();
            var playersManager = Object.FindObjectOfType<SessionPlayersManager>();
            if (playersManager == null)
                Debug.LogWarning("WireTutorialScene: SessionPlayersManager not found — wire manually.");

            var asSO = new SerializedObject(avatarSelector);
            asSO.FindProperty("selectionPanel").objectReferenceValue  = panel; // whole panel stays visible
            asSO.FindProperty("maleButton").objectReferenceValue      = maleBtn;
            asSO.FindProperty("femaleButton").objectReferenceValue    = femaleBtn;
            asSO.FindProperty("playersManager").objectReferenceValue  = playersManager;
            asSO.ApplyModifiedProperties();

            // ── TutorialController ─────────────────────────────────────────────
            var controllerGO = new GameObject("TutorialController");
            var controller   = controllerGO.AddComponent<TutorialController>();

            controller.researcherPanel        = panel;
            controller.soloButton             = soloBtn;
            controller.pairedButton           = pairedBtn;
            controller.beginStudyButton       = beginBtn;
            controller.avatarSelector         = avatarSelector;
            controller.relationshipPanel      = relPanel;
            controller.participantMinusButton = minusBtn;
            controller.participantPlusButton  = plusBtn;
            controller.participantDisplay     = participantDisplay;
            controller.exportToggleButton     = exportBtn;
            controller.exportToggleLabel      = exportLabel;
            controller.relationshipButtons    = relButtons;
            controller.orderButtons           = orderButtons;

            controller.scenarioOrders = new TutorialController.ScenarioOrder[6];
            for (int i = 0; i < 6; i++)
            {
                controller.scenarioOrders[i] = new TutorialController.ScenarioOrder
                {
                    label  = OrderLabels[i],
                    scenes = (string[])OrderScenes[i].Clone(),
                };
            }

            EditorUtility.SetDirty(controller);

            // ── Practice Lever ────────────────────────────────────────────────
            var practiceLever = new GameObject("PracticeLever");
            practiceLever.transform.position = new Vector3(-1.5f, 0.9f, 1f);
            var leverPivot = new GameObject("LeverPivot");
            leverPivot.transform.SetParent(practiceLever.transform, false);
            var leverMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leverMesh.name = "LeverMesh";
            leverMesh.transform.SetParent(leverPivot.transform, false);
            leverMesh.transform.localScale    = new Vector3(0.07f, 0.45f, 0.07f);
            leverMesh.transform.localPosition = new Vector3(0f, 0.225f, 0f);
            Object.DestroyImmediate(leverMesh.GetComponent<BoxCollider>());
            practiceLever.AddComponent<XRGrabInteractable>();

            // ── Practice Button ────────────────────────────────────────────────
            var practiceButton = new GameObject("PracticeButton");
            practiceButton.transform.position = new Vector3(1.5f, 0.9f, 1f);
            var btnMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            btnMesh.name = "ButtonMesh";
            btnMesh.transform.SetParent(practiceButton.transform, false);
            btnMesh.transform.localScale    = new Vector3(0.15f, 0.04f, 0.15f);
            btnMesh.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            practiceButton.AddComponent<XRSimpleInteractable>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("TrolleyTutorialSetup: TrolleyTutorial scene wired and saved.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        static GameObject BuildCanvas(string name, Vector3 position, Vector2 size)
        {
            var go = new GameObject(name);
            go.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
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
