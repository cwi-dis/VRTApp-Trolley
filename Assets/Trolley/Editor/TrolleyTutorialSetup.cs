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
    /// Adds the researcher setup panel, avatar selector, and practice interactables.
    /// TrolleyGameState and DataLogger are already in the scene from Day 1 — not touched here.
    /// </summary>
    public static class TrolleyTutorialSetup
    {
        const string ScenePath = "Assets/Trolley/Scenes/TrolleyTutorial.unity";

        static readonly string[] OrderLabels = { "B→D→O", "B→O→D", "D→B→O", "D→O→B", "O→B→D", "O→D→B" };
        static readonly string[][] OrderScenes =
        {
            new[] { "TrolleyBystander", "TrolleyDriver",   "TrolleyOptional"  },
            new[] { "TrolleyBystander", "TrolleyOptional", "TrolleyDriver"    },
            new[] { "TrolleyDriver",    "TrolleyBystander","TrolleyOptional"  },
            new[] { "TrolleyDriver",    "TrolleyOptional", "TrolleyBystander" },
            new[] { "TrolleyOptional",  "TrolleyBystander","TrolleyDriver"   },
            new[] { "TrolleyOptional",  "TrolleyDriver",   "TrolleyBystander" },
        };

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

            // ── Researcher canvas ─────────────────────────────────────────────
            // Positioned in front of the player at head height, faces them.
            var researcherCanvas = BuildCanvas("ResearcherCanvas",
                position: new Vector3(0f, 1.6f, 2f),
                size: new Vector2(900f, 620f));

            var resPanel = CreatePanel("ResearcherPanel", researcherCanvas,
                new Color(0.08f, 0.08f, 0.08f, 0.96f));

            CreateText("Title", resPanel, "STUDY SETUP",
                new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.98f), fontSize: 52);

            CreateText("ConditionLabel", resPanel, "Condition",
                new Vector2(0.05f, 0.73f), new Vector2(0.95f, 0.84f), fontSize: 30);

            var soloBtn   = CreateButton("SoloButton",   resPanel, "Solo",
                new Vector2(0.05f, 0.58f), new Vector2(0.44f, 0.73f));
            var pairedBtn = CreateButton("PairedButton", resPanel, "Paired",
                new Vector2(0.55f, 0.58f), new Vector2(0.94f, 0.73f));

            CreateText("OrderLabel", resPanel, "Scenario Order",
                new Vector2(0.05f, 0.47f), new Vector2(0.95f, 0.58f), fontSize: 30);

            var orderButtons = new Button[6];
            for (int i = 0; i < 6; i++)
            {
                int col = i % 3;
                int row = i / 3;
                float x0 = 0.04f + col * 0.325f;
                float x1 = x0 + 0.29f;
                float y1 = 0.46f - row * 0.155f;
                float y0 = y1 - 0.135f;
                orderButtons[i] = CreateButton($"OrderButton_{i}", resPanel, OrderLabels[i],
                    new Vector2(x0, y0), new Vector2(x1, y1), fontSize: 26);
            }

            var beginBtn = CreateButton("BeginStudyButton", resPanel, "BEGIN STUDY",
                new Vector2(0.2f, 0.03f), new Vector2(0.8f, 0.155f),
                bgColor: new Color(0.1f, 0.55f, 0.1f), fontSize: 36);

            // ── Avatar canvas ─────────────────────────────────────────────────
            var avatarCanvas = BuildCanvas("AvatarCanvas",
                position: new Vector3(2.2f, 1.6f, 2f),
                size: new Vector2(520f, 340f));

            var avatarPanel = CreatePanel("AvatarSelectionPanel", avatarCanvas,
                new Color(0.08f, 0.08f, 0.08f, 0.96f));

            CreateText("AvatarTitle", avatarPanel, "SELECT AVATAR",
                new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.95f), fontSize: 40);

            var maleBtn   = CreateButton("MaleButton",   avatarPanel, "Male",
                new Vector2(0.05f, 0.08f), new Vector2(0.46f, 0.7f));
            var femaleBtn = CreateButton("FemaleButton", avatarPanel, "Female",
                new Vector2(0.54f, 0.08f), new Vector2(0.95f, 0.7f));

            // ── AvatarSelector component ──────────────────────────────────────
            var avatarSelectorGO = new GameObject("AvatarSelector");
            var avatarSelector = avatarSelectorGO.AddComponent<AvatarSelector>();

            var playersManager = Object.FindObjectOfType<SessionPlayersManager>();
            if (playersManager == null)
                Debug.LogWarning("WireTutorialScene: SessionPlayersManager not found — wire manually after verifying VR2Gather objects in scene.");

            var asSO = new SerializedObject(avatarSelector);
            asSO.FindProperty("selectionPanel").objectReferenceValue  = avatarPanel;
            asSO.FindProperty("maleButton").objectReferenceValue      = maleBtn;
            asSO.FindProperty("femaleButton").objectReferenceValue    = femaleBtn;
            asSO.FindProperty("playersManager").objectReferenceValue  = playersManager;
            // maleSelfPrefab / femaleSelfPrefab: assign Mixamo prefabs manually when ready.
            asSO.ApplyModifiedProperties();

            // ── TutorialController ─────────────────────────────────────────────
            var controllerGO = new GameObject("TutorialController");
            var controller = controllerGO.AddComponent<TutorialController>();

            var tcSO = new SerializedObject(controller);
            tcSO.FindProperty("researcherPanel").objectReferenceValue  = resPanel;
            tcSO.FindProperty("soloButton").objectReferenceValue       = soloBtn;
            tcSO.FindProperty("pairedButton").objectReferenceValue     = pairedBtn;
            tcSO.FindProperty("beginStudyButton").objectReferenceValue = beginBtn;
            tcSO.FindProperty("avatarSelector").objectReferenceValue   = avatarSelector;

            var orderButtonsProp = tcSO.FindProperty("orderButtons");
            orderButtonsProp.arraySize = 6;
            for (int i = 0; i < 6; i++)
                orderButtonsProp.GetArrayElementAtIndex(i).objectReferenceValue = orderButtons[i];

            var ordersProp = tcSO.FindProperty("scenarioOrders");
            ordersProp.arraySize = 6;
            for (int i = 0; i < 6; i++)
            {
                var elem = ordersProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("label").stringValue = OrderLabels[i];
                var scenesProp = elem.FindPropertyRelative("scenes");
                scenesProp.arraySize = 3;
                for (int j = 0; j < 3; j++)
                    scenesProp.GetArrayElementAtIndex(j).stringValue = OrderScenes[i][j];
            }

            tcSO.ApplyModifiedProperties();

            // ── Practice Lever ────────────────────────────────────────────────
            // Raw XRGrabInteractable only — no TrolleyLever, so no game consequences.
            var practiceLever = new GameObject("PracticeLever");
            practiceLever.transform.position = new Vector3(-1.5f, 0.9f, 1f);

            var leverPivot = new GameObject("LeverPivot");
            leverPivot.transform.SetParent(practiceLever.transform, false);

            var leverMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leverMesh.name = "LeverMesh";
            leverMesh.transform.SetParent(leverPivot.transform, false);
            leverMesh.transform.localScale = new Vector3(0.07f, 0.45f, 0.07f);
            leverMesh.transform.localPosition = new Vector3(0f, 0.225f, 0f);
            Object.DestroyImmediate(leverMesh.GetComponent<BoxCollider>());

            practiceLever.AddComponent<XRGrabInteractable>();

            CreateText("PracticeLeverLabel", CreateWorldLabel(
                position: practiceLever.transform.position + new Vector3(0f, 0.7f, 0f)),
                "Practice\nLever", Vector2.zero, Vector2.one, fontSize: 28);

            // ── Practice Button ────────────────────────────────────────────────
            var practiceButton = new GameObject("PracticeButton");
            practiceButton.transform.position = new Vector3(1.5f, 0.9f, 1f);

            var btnMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            btnMesh.name = "ButtonMesh";
            btnMesh.transform.SetParent(practiceButton.transform, false);
            btnMesh.transform.localScale = new Vector3(0.15f, 0.04f, 0.15f);
            btnMesh.transform.localPosition = new Vector3(0f, 0.04f, 0f);

            practiceButton.AddComponent<XRSimpleInteractable>();

            CreateText("PracticeButtonLabel", CreateWorldLabel(
                position: practiceButton.transform.position + new Vector3(0f, 0.25f, 0f)),
                "Practice\nButton", Vector2.zero, Vector2.one, fontSize: 28);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("TrolleyTutorialSetup: TrolleyTutorial scene wired and saved.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        static GameObject BuildCanvas(string name, Vector3 position, Vector2 size)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            go.GetComponent<RectTransform>().sizeDelta = size;
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            go.transform.localScale = Vector3.one * 0.003f;
            return go;
        }

        static GameObject CreatePanel(string name, GameObject parent, Color bgColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = bgColor;
            return go;
        }

        static TextMeshProUGUI CreateText(string name, GameObject parent,
            string text, Vector2 anchorMin, Vector2 anchorMax, float fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
            return tmp;
        }

        static Button CreateButton(string name, GameObject parent, string label,
            Vector2 anchorMin, Vector2 anchorMax,
            Color? bgColor = null, float fontSize = 32)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = bgColor ?? new Color(0.2f, 0.2f, 0.5f);

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.35f, 0.35f, 0.75f);
            colors.pressedColor     = new Color(0.1f, 0.1f, 0.3f);
            btn.colors = colors;

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        // Tiny world-space canvas for practice item labels.
        static GameObject CreateWorldLabel(Vector3 position)
        {
            var go = new GameObject("Label_Canvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 100f);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            go.transform.localScale = Vector3.one * 0.003f;
            return go;
        }
    }
}
