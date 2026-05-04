using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRT.Pilots.Trolley.Editor
{
    public static class TrolleyQuestionnaireSetup
    {
        const string ScenePath      = "Assets/Trolley/Scenes/TrolleyQuestionnaire.unity";
        const string QuestionSetPath = "Assets/Trolley/TrolleyQuestions.asset";

        static readonly Vector3 BoothACenter  = new Vector3(0f,  0f,   0f);
        static readonly Vector3 BoothBCenter  = new Vector3(0f,  0f, -30f);
        static readonly Vector3 DividerCenter = new Vector3(0f,  2f, -15f);

        static readonly Color BtnDefault = new Color(0.2f, 0.2f, 0.8f);
        static readonly Color BtnGreen   = new Color(0.1f, 0.55f, 0.1f);

        struct BoothRefs
        {
            public GameObject refPanel;
            public TextMeshProUGUI refPrompt, refTimer;
            public Button recordButton, stopButton;
            public GameObject qPanel;
            public TextMeshProUGUI qBody;
            public Button[] buttons;
            public TextMeshProUGUI[] labels;
            public Button nextButton;
            public TextMeshProUGUI scaleMinLabel, scaleMaxLabel;
            public GameObject waitPanel;
            public TextMeshProUGUI waitText;
            public GameObject transitionPanel;
            public TextMeshProUGUI transitionText;
            public Button startButton;
        }

        [MenuItem("Trolley/Wire Questionnaire Scene")]
        public static void WireQuestionnaireScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            foreach (string name in new[] {
                "QuestionnaireController", "Environment",
                "BoothA_Canvas", "BoothB_Canvas" })
            {
                var existing = GameObject.Find(name);
                if (existing != null) Object.DestroyImmediate(existing);
            }

            // ── Environment ───────────────────────────────────────────────────
            var envGO = new GameObject("Environment");

            CreateBlackCube("Floor",    envGO, new Vector3(0f, -0.05f, -15f), new Vector3(12f, 0.1f,  40f));
            CreateBlackCube("Ceiling",  envGO, new Vector3(0f,  3.1f,  -15f), new Vector3(12f, 0.1f,  40f));
            CreateBlackCube("WallLeft", envGO, new Vector3(-6f, 1.5f,  -15f), new Vector3(0.1f, 3f,   40f));
            CreateBlackCube("WallRight",envGO, new Vector3( 6f, 1.5f,  -15f), new Vector3(0.1f, 3f,   40f));
            CreateBlackCube("WallEndA", envGO, new Vector3(0f,  1.5f,    5f), new Vector3(12f,  3f,  0.1f));
            CreateBlackCube("WallEndB", envGO, new Vector3(0f,  1.5f,  -35f), new Vector3(12f,  3f,  0.1f));
            CreateBlackCube("Divider",  envGO, DividerCenter,                  new Vector3(12f,  4f,  0.3f));

            CreateDimLight("LightBoothA", envGO, BoothACenter + new Vector3(0f, 2.8f, 0f));
            CreateDimLight("LightBoothB", envGO, BoothBCenter + new Vector3(0f, 2.8f, 0f));

            // ── Booth canvases ─────────────────────────────────────────────────
            var a = BuildBoothCanvas("BoothA_Canvas", BoothACenter);
            var b = BuildBoothCanvas("BoothB_Canvas", BoothBCenter);

            // ── QuestionnaireController ───────────────────────────────────────
            var controllerGO = new GameObject("QuestionnaireController");
            var controller   = controllerGO.AddComponent<QuestionnaireController>();

            var questionSet = AssetDatabase.LoadAssetAtPath<QuestionSet>(QuestionSetPath);
            if (questionSet == null)
                Debug.LogWarning("WireQuestionnaireScene: TrolleyQuestions.asset not found — assign manually.");

            var so = new SerializedObject(controller);
            so.FindProperty("questionSet").objectReferenceValue = questionSet;

            WireBooth(so, "A", a);
            WireBooth(so, "B", b);
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("TrolleyQuestionnaireSetup: scene wired and saved.");
        }

        static void WireBooth(SerializedObject so, string suffix, BoothRefs r)
        {
            so.FindProperty($"reflectionPanel{suffix}").objectReferenceValue      = r.refPanel;
            so.FindProperty($"reflectionPromptText{suffix}").objectReferenceValue = r.refPrompt;
            so.FindProperty($"reflectionTimerText{suffix}").objectReferenceValue  = r.refTimer;
            so.FindProperty($"recordButton{suffix}").objectReferenceValue         = r.recordButton;
            so.FindProperty($"stopButton{suffix}").objectReferenceValue           = r.stopButton;
            so.FindProperty($"questionPanel{suffix}").objectReferenceValue        = r.qPanel;
            so.FindProperty($"questionBodyText{suffix}").objectReferenceValue     = r.qBody;
            so.FindProperty($"nextButton{suffix}").objectReferenceValue           = r.nextButton;
            so.FindProperty($"scaleMinLabel{suffix}").objectReferenceValue        = r.scaleMinLabel;
            so.FindProperty($"scaleMaxLabel{suffix}").objectReferenceValue        = r.scaleMaxLabel;
            so.FindProperty($"waitingPanel{suffix}").objectReferenceValue         = r.waitPanel;
            so.FindProperty($"waitingText{suffix}").objectReferenceValue          = r.waitText;
            so.FindProperty($"transitionPanel{suffix}").objectReferenceValue      = r.transitionPanel;
            so.FindProperty($"transitionText{suffix}").objectReferenceValue       = r.transitionText;
            so.FindProperty($"startButton{suffix}").objectReferenceValue          = r.startButton;

            var btnProp = so.FindProperty($"likertButtons{suffix}");
            btnProp.arraySize = r.buttons.Length;
            for (int i = 0; i < r.buttons.Length; i++)
                btnProp.GetArrayElementAtIndex(i).objectReferenceValue = r.buttons[i];

            var lblProp = so.FindProperty($"likertLabels{suffix}");
            lblProp.arraySize = r.labels.Length;
            for (int i = 0; i < r.labels.Length; i++)
                lblProp.GetArrayElementAtIndex(i).objectReferenceValue = r.labels[i];
        }

        // ── Booth canvas builder ──────────────────────────────────────────────

        static BoothRefs BuildBoothCanvas(string canvasName, Vector3 boothCenter)
        {
            var canvasGO = new GameObject(canvasName);
            canvasGO.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            canvasGO.GetComponent<RectTransform>().sizeDelta = new Vector2(800f, 600f);
            canvasGO.transform.position    = boothCenter + new Vector3(0f, 1.6f, 0f);
            canvasGO.transform.rotation    = Quaternion.Euler(0f, 180f, 0f);
            canvasGO.transform.localScale  = Vector3.one * 0.003f;

            // ── Reflection panel ──────────────────────────────────────────────
            var refPanel  = CreatePanel("ReflectionPanel", canvasGO, Color.black);
            var refPrompt = CreateTMP("PromptText", refPanel,
                new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.90f), 36,
                "Reflect aloud: why did you make this decision?");
            var refTimer  = CreateTMP("TimerText", refPanel,
                new Vector2(0.35f, 0.05f), new Vector2(0.65f, 0.28f), 72, "15");
            refTimer.alignment = TextAlignmentOptions.Center;

            var recordBtn = CreateButton("RecordButton", refPanel, "● RECORD",
                new Vector2(0.10f, 0.05f), new Vector2(0.45f, 0.27f), new Color(0.7f, 0.1f, 0.1f), 30);
            var stopBtn   = CreateButton("StopButton", refPanel, "■ STOP",
                new Vector2(0.55f, 0.05f), new Vector2(0.90f, 0.27f), new Color(0.3f, 0.3f, 0.3f), 30);
            stopBtn.gameObject.SetActive(false);

            refPanel.SetActive(false);

            // ── Question panel ────────────────────────────────────────────────
            var qPanel = CreatePanel("QuestionPanel", canvasGO, Color.black);

            var qBody = CreateTMP("QuestionBodyText", qPanel,
                new Vector2(0.05f, 0.60f), new Vector2(0.95f, 0.95f), 34, "Question text here.");
            qBody.alignment = TextAlignmentOptions.TopLeft;

            // Likert buttons row
            var rowGO = new GameObject("LikertRow");
            rowGO.transform.SetParent(qPanel.transform, false);
            var rowRect = rowGO.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.02f, 0.38f);
            rowRect.anchorMax = new Vector2(0.98f, 0.60f);
            rowRect.offsetMin = rowRect.offsetMax = Vector2.zero;
            var hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childAlignment      = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth  = true;
            hlg.childForceExpandHeight = true;

            var buttons = new Button[7];
            var labels  = new TextMeshProUGUI[7];
            for (int i = 0; i < 7; i++)
            {
                var btnGO = new GameObject($"LikertButton_{i + 1}");
                btnGO.transform.SetParent(rowGO.transform, false);
                btnGO.AddComponent<RectTransform>();
                btnGO.AddComponent<Image>().color = BtnDefault;
                var btn = btnGO.AddComponent<Button>();

                var labelGO = new GameObject("Label");
                labelGO.transform.SetParent(btnGO.transform, false);
                var lr = labelGO.AddComponent<RectTransform>();
                lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
                lr.offsetMin = lr.offsetMax = Vector2.zero;
                var tmp = labelGO.AddComponent<TextMeshProUGUI>();
                tmp.text = (i + 1).ToString();
                tmp.fontSize = 40;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;

                buttons[i] = btn;
                labels[i]  = tmp;
            }

            // Scale endpoint labels
            var scaleMin = CreateTMP("ScaleMinLabel", qPanel,
                new Vector2(0.02f, 0.28f), new Vector2(0.35f, 0.37f), 22, "Not at all");
            scaleMin.alignment = TextAlignmentOptions.MidlineLeft;
            scaleMin.color = new Color(0.8f, 0.8f, 0.8f);

            var scaleMax = CreateTMP("ScaleMaxLabel", qPanel,
                new Vector2(0.65f, 0.28f), new Vector2(0.98f, 0.37f), 22, "Very much");
            scaleMax.alignment = TextAlignmentOptions.MidlineRight;
            scaleMax.color = new Color(0.8f, 0.8f, 0.8f);

            // Next button
            var nextBtn = CreateButton("NextButton", qPanel, "NEXT →",
                new Vector2(0.30f, 0.05f), new Vector2(0.70f, 0.25f), BtnGreen, 32);
            nextBtn.interactable = false;

            qPanel.SetActive(false);

            // ── Waiting panel ─────────────────────────────────────────────────
            var waitPanel = CreatePanel("WaitingPanel", canvasGO, Color.black);
            var waitText  = CreateTMP("WaitingText", waitPanel,
                new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.7f), 40,
                "Waiting for your partner...");
            waitText.alignment = TextAlignmentOptions.Center;
            waitPanel.SetActive(false);

            // ── Transition panel ──────────────────────────────────────────────
            var transPanel = CreatePanel("TransitionPanel", canvasGO, Color.black);
            var transText  = CreateTMP("TransitionText", transPanel,
                new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.88f), 36, "");
            transText.alignment = TextAlignmentOptions.Center;

            var startBtn = CreateButton("StartButton", transPanel, "START",
                new Vector2(0.25f, 0.05f), new Vector2(0.75f, 0.25f), BtnGreen, 40);

            transPanel.SetActive(false);

            return new BoothRefs
            {
                refPanel       = refPanel,
                refPrompt      = refPrompt,
                refTimer       = refTimer,
                recordButton   = recordBtn,
                stopButton     = stopBtn,
                qPanel         = qPanel,
                qBody          = qBody,
                buttons        = buttons,
                labels         = labels,
                nextButton     = nextBtn,
                scaleMinLabel  = scaleMin,
                scaleMaxLabel  = scaleMax,
                waitPanel      = waitPanel,
                waitText       = waitText,
                transitionPanel = transPanel,
                transitionText  = transText,
                startButton     = startBtn,
            };
        }

        // ── Helpers ───────────────────────────────────────────────────────────

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

        static TextMeshProUGUI CreateTMP(string name, GameObject parent,
            Vector2 min, Vector2 max, float fontSize, string text)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fontSize;
            tmp.color = Color.white; tmp.enableWordWrapping = true;
            return tmp;
        }

        static Button CreateButton(string name, GameObject parent, string label,
            Vector2 min, Vector2 max, Color bg, float fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            go.AddComponent<Image>().color = bg;
            var btn = go.AddComponent<Button>();

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var lr = labelGO.AddComponent<RectTransform>();
            lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
            lr.offsetMin = lr.offsetMax = Vector2.zero;
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btn;
        }

        static GameObject CreateBlackCube(string name, GameObject parent,
            Vector3 position, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent.transform);
            go.transform.position   = position;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            var mat = Object.Instantiate(renderer.sharedMaterial);
            mat.SetColor("_BaseColor", Color.black);
            mat.SetColor("_Color",     Color.black);
            renderer.sharedMaterial = mat;
            return go;
        }

        static void CreateDimLight(string name, GameObject parent, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform);
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type      = LightType.Point;
            light.intensity = 1.2f;
            light.range     = 8f;
            light.color     = new Color(1f, 0.95f, 0.85f);
        }
    }
}
