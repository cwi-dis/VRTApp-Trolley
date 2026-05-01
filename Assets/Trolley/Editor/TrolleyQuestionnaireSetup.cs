using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Run once via menu: Trolley > Wire Questionnaire Scene
    /// Creates a black two-booth room. Booth A = master, Booth B = non-master.
    /// An opaque wall divides them so participants cannot see each other.
    /// </summary>
    public static class TrolleyQuestionnaireSetup
    {
        const string ScenePath = "Assets/Trolley/Scenes/TrolleyQuestionnaire.unity";
        const string QuestionSetPath = "Assets/Trolley/TrolleyQuestions.asset";

        // Booth positions along the Z axis, far enough apart for voice separation.
        static readonly Vector3 BoothACenter = new Vector3(0f, 0f, 0f);
        static readonly Vector3 BoothBCenter = new Vector3(0f, 0f, -30f);
        static readonly Vector3 DividerCenter = new Vector3(0f, 2f, -15f);

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
            // Black floor spanning both booths + an opaque dividing wall.
            var envGO = new GameObject("Environment");

            var floor = CreateBlackCube("Floor", envGO,
                position: new Vector3(0f, -0.05f, -15f),
                scale: new Vector3(12f, 0.1f, 40f));

            // Ceiling to block looking over the wall.
            var ceiling = CreateBlackCube("Ceiling", envGO,
                position: new Vector3(0f, 3.1f, -15f),
                scale: new Vector3(12f, 0.1f, 40f));

            // Side walls.
            CreateBlackCube("WallLeft",  envGO, new Vector3(-6f, 1.5f, -15f), new Vector3(0.1f, 3f, 40f));
            CreateBlackCube("WallRight", envGO, new Vector3( 6f, 1.5f, -15f), new Vector3(0.1f, 3f, 40f));

            // End walls behind each booth.
            CreateBlackCube("WallEndA", envGO, new Vector3(0f, 1.5f,  5f), new Vector3(12f, 3f, 0.1f));
            CreateBlackCube("WallEndB", envGO, new Vector3(0f, 1.5f, -35f), new Vector3(12f, 3f, 0.1f));

            // Opaque divider between booths (full height + width to block sightlines).
            CreateBlackCube("Divider", envGO, DividerCenter, new Vector3(12f, 4f, 0.3f));

            // Dim overhead lights — one per booth.
            CreateDimLight("LightBoothA", envGO, BoothACenter + new Vector3(0f, 2.8f, 0f));
            CreateDimLight("LightBoothB", envGO, BoothBCenter + new Vector3(0f, 2.8f, 0f));

            // ── Booth canvases ─────────────────────────────────────────────────
            var (refPanelA, refPromptA, refTimerA,
                 qPanelA, qBodyA, buttonsA, labelsA,
                 waitPanelA, waitTextA) = BuildBoothCanvas("BoothA_Canvas", BoothACenter);

            var (refPanelB, refPromptB, refTimerB,
                 qPanelB, qBodyB, buttonsB, labelsB,
                 waitPanelB, waitTextB) = BuildBoothCanvas("BoothB_Canvas", BoothBCenter);

            // ── QuestionnaireController ───────────────────────────────────────
            var controllerGO = new GameObject("QuestionnaireController");
            var controller = controllerGO.AddComponent<QuestionnaireController>();

            var questionSet = AssetDatabase.LoadAssetAtPath<QuestionSet>(QuestionSetPath);
            if (questionSet == null)
                Debug.LogWarning("WireQuestionnaireScene: TrolleyQuestions.asset not found — assign manually.");

            var so = new SerializedObject(controller);
            so.FindProperty("questionSet").objectReferenceValue = questionSet;

            // Booth A
            so.FindProperty("reflectionPanelA").objectReferenceValue      = refPanelA;
            so.FindProperty("reflectionPromptTextA").objectReferenceValue  = refPromptA;
            so.FindProperty("reflectionTimerTextA").objectReferenceValue   = refTimerA;
            so.FindProperty("questionPanelA").objectReferenceValue         = qPanelA;
            so.FindProperty("questionBodyTextA").objectReferenceValue      = qBodyA;
            so.FindProperty("waitingPanelA").objectReferenceValue          = waitPanelA;
            so.FindProperty("waitingTextA").objectReferenceValue           = waitTextA;
            SetButtonArray(so, "likertButtonsA", buttonsA);
            SetTMPArray(so, "likertLabelsA", labelsA);

            // Booth B
            so.FindProperty("reflectionPanelB").objectReferenceValue      = refPanelB;
            so.FindProperty("reflectionPromptTextB").objectReferenceValue  = refPromptB;
            so.FindProperty("reflectionTimerTextB").objectReferenceValue   = refTimerB;
            so.FindProperty("questionPanelB").objectReferenceValue         = qPanelB;
            so.FindProperty("questionBodyTextB").objectReferenceValue      = qBodyB;
            so.FindProperty("waitingPanelB").objectReferenceValue          = waitPanelB;
            so.FindProperty("waitingTextB").objectReferenceValue           = waitTextB;
            SetButtonArray(so, "likertButtonsB", buttonsB);
            SetTMPArray(so, "likertLabelsB", labelsB);

            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("TrolleyQuestionnaireSetup: scene wired and saved.");
        }

        // ── Booth canvas builder ──────────────────────────────────────────────
        // Returns all UI component refs for wiring into QuestionnaireController.

        static (GameObject refPanel, TextMeshProUGUI refPrompt, TextMeshProUGUI refTimer,
                GameObject qPanel, TextMeshProUGUI qBody,
                Button[] buttons, TextMeshProUGUI[] labels,
                GameObject waitPanel, TextMeshProUGUI waitText)
            BuildBoothCanvas(string canvasName, Vector3 boothCenter)
        {
            // Canvas faces the player (facing +Z from the booth center).
            var canvasGO = new GameObject(canvasName);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            var rootRect = canvasGO.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(800f, 600f);
            canvasGO.transform.position = boothCenter + new Vector3(0f, 1.6f, 2f);
            canvasGO.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // face the player
            canvasGO.transform.localScale = Vector3.one * 0.003f;

            // ── Reflection panel ──────────────────────────────────────────────
            var refPanel = CreatePanel("ReflectionPanel", canvasGO, Color.black);

            var refPrompt = CreateTMP("PromptText", refPanel,
                anchorMin: new Vector2(0.05f, 0.3f), anchorMax: new Vector2(0.95f, 0.9f),
                fontSize: 36, text: "Reflect aloud: why did you make this decision?");

            var refTimer = CreateTMP("TimerText", refPanel,
                anchorMin: new Vector2(0.35f, 0.05f), anchorMax: new Vector2(0.65f, 0.28f),
                fontSize: 72, text: "15");
            refTimer.alignment = TextAlignmentOptions.Center;
            refPanel.SetActive(false);

            // ── Question panel ────────────────────────────────────────────────
            var qPanel = CreatePanel("QuestionPanel", canvasGO, Color.black);

            var qBody = CreateTMP("QuestionBodyText", qPanel,
                anchorMin: new Vector2(0.05f, 0.55f), anchorMax: new Vector2(0.95f, 0.95f),
                fontSize: 34, text: "Question text here.");
            qBody.alignment = TextAlignmentOptions.TopLeft;

            // Likert row: 7 buttons + labels
            var likertRowGO = new GameObject("LikertRow");
            likertRowGO.transform.SetParent(qPanel.transform, false);
            var rowRect = likertRowGO.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.02f, 0.05f);
            rowRect.anchorMax = new Vector2(0.98f, 0.52f);
            rowRect.offsetMin = rowRect.offsetMax = Vector2.zero;
            var hlg = likertRowGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            var buttons = new Button[7];
            var labels  = new TextMeshProUGUI[7];
            for (int i = 0; i < 7; i++)
            {
                var btnGO = new GameObject($"LikertButton_{i + 1}");
                btnGO.transform.SetParent(likertRowGO.transform, false);
                btnGO.AddComponent<RectTransform>();

                var img = btnGO.AddComponent<Image>();
                img.color = new Color(0.2f, 0.2f, 0.8f);

                var btn = btnGO.AddComponent<Button>();
                var colors = btn.colors;
                colors.highlightedColor = new Color(0.4f, 0.4f, 1f);
                colors.pressedColor     = new Color(0.1f, 0.1f, 0.5f);
                btn.colors = colors;

                var labelGO = new GameObject("Label");
                labelGO.transform.SetParent(btnGO.transform, false);
                var labelRect = labelGO.AddComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
                var tmp = labelGO.AddComponent<TextMeshProUGUI>();
                tmp.text = (i + 1).ToString();
                tmp.fontSize = 40;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;

                buttons[i] = btn;
                labels[i]  = tmp;
            }

            qPanel.SetActive(false);

            // ── Waiting panel ─────────────────────────────────────────────────
            var waitPanel = CreatePanel("WaitingPanel", canvasGO, Color.black);
            var waitText = CreateTMP("WaitingText", waitPanel,
                anchorMin: new Vector2(0.1f, 0.3f), anchorMax: new Vector2(0.9f, 0.7f),
                fontSize: 40, text: "Waiting for your partner...");
            waitText.alignment = TextAlignmentOptions.Center;
            waitPanel.SetActive(false);

            return (refPanel, refPrompt, refTimer,
                    qPanel, qBody, buttons, labels,
                    waitPanel, waitText);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

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

        static TextMeshProUGUI CreateTMP(string name, GameObject parent,
            Vector2 anchorMin, Vector2 anchorMax, float fontSize, string text)
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
            tmp.enableWordWrapping = true;
            return tmp;
        }

        static GameObject CreateBlackCube(string name, GameObject parent,
            Vector3 position, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent.transform);
            go.transform.position = position;
            go.transform.localScale = scale;

            var renderer = go.GetComponent<Renderer>();
            var mat = Object.Instantiate(renderer.sharedMaterial); // copy the existing URP-ready default
            mat.SetColor("_BaseColor", Color.black);
            mat.SetColor("_Color",     Color.black);
            renderer.sharedMaterial = mat;
            return go;
        }

        static void SetButtonArray(SerializedObject so, string fieldName, Button[] buttons)
        {
            var prop = so.FindProperty(fieldName);
            prop.arraySize = buttons.Length;
            for (int i = 0; i < buttons.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
        }

        static void CreateDimLight(string name, GameObject parent, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform);
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.intensity = 0.4f;
            light.range = 8f;
            light.color = new Color(1f, 0.95f, 0.85f); // warm white
        }

        static void SetTMPArray(SerializedObject so, string fieldName, TextMeshProUGUI[] tmps)
        {
            var prop = so.FindProperty(fieldName);
            prop.arraySize = tmps.Length;
            for (int i = 0; i < tmps.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = tmps[i];
        }
    }
}
