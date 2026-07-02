using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using TMPro;
using VRT.Pilots.Common;

namespace VRT.Pilots.Trolley.Editor
{
    public static class TrolleyQuestionnaireSetup
    {
        const string ScenePath      = "Assets/Trolley/Scenes/TrolleyQuestionnaire.unity";
        const string QuestionSetPath = "Assets/Trolley/TrolleyQuestions.asset";

        static readonly Vector3 BoothACenter  = new Vector3(0f,  0f,   0f);
        static readonly Vector3 BoothBCenter  = new Vector3(0f,  0f, -30f);
        static readonly Vector3 DividerCenter = new Vector3(0f,  2f, -15f);

        static readonly Color BtnGreen   = new Color(0.1f, 0.55f, 0.1f);

        // Reflection-panel palette — dark theme (black card, white text), keeping the clear hierarchy
        // from Resources/SelfReflectionPanel.png so participants still understand what to do.
        static readonly Color CardBg     = new Color(0.05f, 0.05f, 0.06f);
        static readonly Color TextPrimary= new Color(0.96f, 0.96f, 0.97f);
        static readonly Color TextMuted  = new Color(0.60f, 0.62f, 0.66f);
        static readonly Color DividerCol = new Color(0.25f, 0.26f, 0.30f);
        static readonly Color AccentBg   = new Color(0.10f, 0.16f, 0.28f);
        static readonly Color AccentText = new Color(0.68f, 0.82f, 0.99f);
        static readonly Color ButtonRed  = new Color(0.78f, 0.22f, 0.20f);
        static readonly Color DoneText   = new Color(0.97f, 0.98f, 0.97f);

        const int MaxRows = 5; // most questions shown on one page (Decision Evaluation / Partner = 5)

        struct RowRefs
        {
            public GameObject root;
            public TextMeshProUGUI text;
            public Button[] buttons;
            public TextMeshProUGUI[] labels;
        }

        struct BoothRefs
        {
            public GameObject refPanel;
            public TextMeshProUGUI refPrompt, refTimer, refInstruction;
            public Button doneButton;
            public GameObject qPanel;
            public RowRefs[] rows;
            public Button nextButton;
            public RectTransform progressFill;
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
                "BoothA_Canvas", "BoothB_Canvas",
                "TransitionReadyTrigger", "TransitionBarrier", "TransitionProceedTrigger" })
            {
                var existing = GameObject.Find(name);
                if (existing != null) Object.DestroyImmediate(existing);
            }

            const string menuItem = "Trolley/Wire Questionnaire Scene";

            // ── Environment ───────────────────────────────────────────────────
            var envGO = new GameObject("Environment");
            envGO.AddComponent<ManagedBySetupScript>().menuItem = menuItem;

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
            controllerGO.AddComponent<ManagedBySetupScript>().menuItem = menuItem;
            var controller   = controllerGO.AddComponent<QuestionnaireController>();

            var questionSet = AssetDatabase.LoadAssetAtPath<QuestionSet>(QuestionSetPath);
            if (questionSet == null)
                Debug.LogWarning("WireQuestionnaireScene: TrolleyQuestions.asset not found — assign manually.");

            var so = new SerializedObject(controller);
            so.FindProperty("questionSet").objectReferenceValue = questionSet;

            WireBooth(so, "A", a);
            WireBooth(so, "B", b);
            TrolleySetupBarrierUtils.AddTransitionBarrier(so, menuItem);
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
            so.FindProperty($"reflectionInstructionText{suffix}").objectReferenceValue = r.refInstruction;
            so.FindProperty($"reflectionDoneButton{suffix}").objectReferenceValue = r.doneButton;
            so.FindProperty($"questionPanel{suffix}").objectReferenceValue        = r.qPanel;
            so.FindProperty($"nextButton{suffix}").objectReferenceValue           = r.nextButton;
            so.FindProperty($"progressFill{suffix}").objectReferenceValue         = r.progressFill;
            so.FindProperty($"scaleMinLabel{suffix}").objectReferenceValue        = r.scaleMinLabel;
            so.FindProperty($"scaleMaxLabel{suffix}").objectReferenceValue        = r.scaleMaxLabel;
            so.FindProperty($"waitingPanel{suffix}").objectReferenceValue         = r.waitPanel;
            so.FindProperty($"waitingText{suffix}").objectReferenceValue          = r.waitText;
            so.FindProperty($"transitionPanel{suffix}").objectReferenceValue      = r.transitionPanel;
            so.FindProperty($"transitionText{suffix}").objectReferenceValue       = r.transitionText;
            so.FindProperty($"startButton{suffix}").objectReferenceValue          = r.startButton;

            // questionRows is an array of the controller's [Serializable] QuestionRow — wire each row's
            // sub-fields (root / text / buttons[] / labels[]) via SerializedProperty navigation.
            var rowsProp = so.FindProperty($"questionRows{suffix}");
            rowsProp.arraySize = r.rows.Length;
            for (int i = 0; i < r.rows.Length; i++)
            {
                var rp = rowsProp.GetArrayElementAtIndex(i);
                rp.FindPropertyRelative("root").objectReferenceValue = r.rows[i].root;
                rp.FindPropertyRelative("text").objectReferenceValue = r.rows[i].text;

                var bp = rp.FindPropertyRelative("buttons");
                bp.arraySize = r.rows[i].buttons.Length;
                for (int b = 0; b < r.rows[i].buttons.Length; b++)
                    bp.GetArrayElementAtIndex(b).objectReferenceValue = r.rows[i].buttons[b];

                var lp = rp.FindPropertyRelative("labels");
                lp.arraySize = r.rows[i].labels.Length;
                for (int b = 0; b < r.rows[i].labels.Length; b++)
                    lp.GetArrayElementAtIndex(b).objectReferenceValue = r.rows[i].labels[b];
            }
        }

        // ── Booth canvas builder ──────────────────────────────────────────────

        static BoothRefs BuildBoothCanvas(string canvasName, Vector3 boothCenter)
        {
            var canvasGO = new GameObject(canvasName);
            canvasGO.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            canvasGO.AddComponent<TrackedDeviceGraphicRaycaster>();
            canvasGO.AddComponent<ManagedBySetupScript>().menuItem = "Trolley/Wire Questionnaire Scene";
            canvasGO.GetComponent<RectTransform>().sizeDelta = new Vector2(800f, 600f);
            canvasGO.transform.position    = boothCenter + new Vector3(0f, 1.6f, 0f);
            canvasGO.transform.rotation    = Quaternion.Euler(0f, 180f, 0f);
            canvasGO.transform.localScale  = Vector3.one * 0.003f;

            // ── Reflection panel (redesigned — see Resources/SelfReflectionPanel.png) ──────────
            // A light card with a clear hierarchy so participants understand what to do:
            //   centred title · OUTCOME (the consequence, set at runtime) · a highlighted box
            //   telling them to speak aloud · a button that names the action.
            var refPanel = CreateCard("ReflectionPanel", canvasGO,
                Vector2.zero, Vector2.one, CardBg);

            // Header: centred title.
            var refTitle = CreateTMP("Title", refPanel,
                new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.97f), 30, "Self-reflection");
            refTitle.alignment = TextAlignmentOptions.Center;
            refTitle.color = TextPrimary;
            refTitle.fontStyle = FontStyles.Bold;

            // Divider under the header.
            CreateCard("Divider", refPanel, new Vector2(0.05f, 0.868f), new Vector2(0.95f, 0.872f), DividerCol);

            // OUTCOME — the consequence of their choice (controller sets ConsequenceText at runtime).
            var whatLbl = CreateTMP("OutcomeLabel", refPanel,
                new Vector2(0.05f, 0.79f), new Vector2(0.95f, 0.845f), 16, "OUTCOME");
            whatLbl.alignment = TextAlignmentOptions.MidlineLeft;
            whatLbl.color = TextMuted;
            whatLbl.characterSpacing = 6f;

            var refPrompt = CreateTMP("ConsequenceText", refPanel,
                new Vector2(0.05f, 0.62f), new Vector2(0.95f, 0.79f), 28,
                "You diverted the train to the side track. The five workers are safe, but one worker was hit.");
            refPrompt.alignment = TextAlignmentOptions.TopLeft;
            refPrompt.color = TextPrimary;

            // Highlighted instruction box — the imperative + the question to answer aloud (controller
            // sets InstructionText at runtime, appending the paired-session line when relevant).
            var accent = CreateCard("InstructionBox", refPanel,
                new Vector2(0.05f, 0.35f), new Vector2(0.95f, 0.605f), AccentBg);
            var accentHead = CreateTMP("InstructionHead", accent,
                new Vector2(0.05f, 0.54f), new Vector2(0.95f, 0.90f), 26, "Answer the questions out loud");
            accentHead.alignment = TextAlignmentOptions.MidlineLeft;
            accentHead.color = AccentText;
            accentHead.fontStyle = FontStyles.Bold;
            var refInstruction = CreateTMP("InstructionText", accent,
                new Vector2(0.05f, 0.10f), new Vector2(0.95f, 0.54f), 24,
                "Why did you make that choice? What were you thinking in the moment?");
            refInstruction.alignment = TextAlignmentOptions.TopLeft;
            refInstruction.color = AccentText;

            // Done button — narrower and centred so it reads as a button; the label names the action.
            var doneBtn = CreateButton("DoneButton", refPanel, "I've answered",
                new Vector2(0.28f, 0.06f), new Vector2(0.72f, 0.185f), ButtonRed, 26, DoneText);

            // Timer kept (empty, invisible) only for the no-Done-button fallback path in the controller.
            var refTimer = CreateTMP("TimerText", refPanel,
                Vector2.zero, new Vector2(0.001f, 0.001f), 1, "");

            refPanel.SetActive(false);

            // ── Question panel: a Likert matrix — the 7 point-words as a shared column header, then up to
            //    MaxRows question rows, each a line of 7 circles aligned under the header words ──
            var qPanel = CreatePanel("QuestionPanel", canvasGO, Color.black);

            // 7 evenly-spaced circle columns on the right; the question text occupies the (wider) left margin.
            const float colsLeft = 0.47f, colsRight = 0.985f;
            float colW = (colsRight - colsLeft) / 7f;
            var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"); // built-in circle

            // Column header — the 7 anchor words. col 0 / col 6 are returned to the controller as
            // scaleMin/scaleMax (kept for compatibility; the header is otherwise static).
            string[] anchors = { "Strongly\ndisagree", "Disagree", "Somewhat\ndisagree", "Neutral",
                                 "Somewhat\nagree", "Agree", "Strongly\nagree" };
            TextMeshProUGUI scaleMin = null, scaleMax = null;
            for (int i = 0; i < 7; i++)
            {
                float cx = colsLeft + i * colW;
                var lbl = CreateTMP($"Anchor_{i + 1}", qPanel,
                    new Vector2(cx, 0.88f), new Vector2(cx + colW, 0.985f), 14, anchors[i]);
                lbl.alignment = TextAlignmentOptions.Center;
                lbl.color = new Color(0.85f, 0.85f, 0.85f);
                if (i == 0) scaleMin = lbl;
                if (i == 6) scaleMax = lbl;
            }

            // Up to MaxRows question rows, stacked top → bottom, with a gap between them. The controller
            // hides unused rows. rowGap is the vertical space between questions; increase for more air.
            var rows = new RowRefs[MaxRows];
            const float top = 0.86f, bottom = 0.17f;
            const float rowGap = 0.05f;
            float slotH = (top - bottom) / MaxRows;
            for (int i = 0; i < MaxRows; i++)
            {
                float slotMax = top - i * slotH;
                float slotMin = slotMax - slotH;
                rows[i] = BuildMatrixRow(qPanel, slotMin + rowGap * 0.5f, slotMax - rowGap * 0.5f, colsLeft, colW, knob);
            }

            // Next button.
            var nextBtn = CreateButton("NextButton", qPanel, "NEXT →",
                new Vector2(0.34f, 0.05f), new Vector2(0.66f, 0.15f), BtnGreen, 30);
            nextBtn.interactable = false;

            // Thin progress bar along the very bottom — the controller fills it green to page/total.
            var barBg = CreatePanel("ProgressBarBg", qPanel, new Color(0.25f, 0.25f, 0.25f));
            var barBgRect = barBg.GetComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0f, 0f);
            barBgRect.anchorMax = new Vector2(1f, 0.012f);
            barBgRect.offsetMin = barBgRect.offsetMax = Vector2.zero;

            var fillGO = new GameObject("ProgressFill");
            fillGO.transform.SetParent(barBg.transform, false);
            var progressFill = fillGO.AddComponent<RectTransform>();
            progressFill.anchorMin = new Vector2(0f, 0f);
            progressFill.anchorMax = new Vector2(0f, 1f);   // empty; controller sets anchorMax.x to page/total
            progressFill.offsetMin = progressFill.offsetMax = Vector2.zero;
            fillGO.AddComponent<Image>().color = new Color(0.1f, 0.6f, 0.1f); // green

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

            var startBtn = CreateButton("StartButton", transPanel, "CONTINUE",
                new Vector2(0.28f, 0.06f), new Vector2(0.72f, 0.185f), BtnGreen, 26);

            transPanel.SetActive(false);

            return new BoothRefs
            {
                refPanel       = refPanel,
                refPrompt      = refPrompt,
                refTimer       = refTimer,
                refInstruction = refInstruction,
                doneButton     = doneBtn,
                qPanel         = qPanel,
                rows           = rows,
                nextButton     = nextBtn,
                progressFill   = progressFill,
                scaleMinLabel  = scaleMin,
                scaleMaxLabel  = scaleMax,
                waitPanel      = waitPanel,
                waitText       = waitText,
                transitionPanel = transPanel,
                transitionText  = transText,
                startButton     = startBtn,
            };
        }

        // One matrix row: the question text in the left margin, then 7 Likert circles centred in the 7
        // columns (colsLeft + i*colW) so they line up under the header words. Built as a full-width
        // container so the controller can hide the whole row when a page uses fewer than MaxRows questions.
        static RowRefs BuildMatrixRow(GameObject panel, float yMin, float yMax,
                                      float colsLeft, float colW, Sprite knob)
        {
            var rowGO = new GameObject("QuestionRow");
            rowGO.transform.SetParent(panel.transform, false);
            var rect = rowGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, yMin);
            rect.anchorMax = new Vector2(1f, yMax);
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            // Question text — left margin, vertically centred.
            var text = CreateTMP("RowText", rowGO,
                new Vector2(0.03f, 0f), new Vector2(colsLeft - 0.01f, 1f), 22, "Question");
            text.alignment = TextAlignmentOptions.MidlineLeft;

            // 7 circles, centred in their columns. A fixed square sizeDelta keeps them truly circular
            // (the Knob sprite) regardless of panel aspect; the controller colours empty vs selected.
            var buttons = new Button[7];
            for (int i = 0; i < 7; i++)
            {
                float cx = colsLeft + (i + 0.5f) * colW;
                var cGO = new GameObject($"Circle_{i + 1}");
                cGO.transform.SetParent(rowGO.transform, false);
                var cr = cGO.AddComponent<RectTransform>();
                cr.anchorMin = cr.anchorMax = new Vector2(cx, 0.5f);
                cr.sizeDelta = new Vector2(40f, 40f);
                var img = cGO.AddComponent<Image>();
                if (knob != null) img.sprite = knob;
                img.color = new Color(0.78f, 0.78f, 0.78f);   // empty; controller manages empty/selected
                buttons[i] = cGO.AddComponent<Button>();
            }

            return new RowRefs { root = rowGO, text = text, buttons = buttons, labels = new TextMeshProUGUI[0] };
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
            tmp.color = Color.white; tmp.textWrappingMode = TextWrappingModes.Normal;
            return tmp;
        }

        static Button CreateButton(string name, GameObject parent, string label,
            Vector2 min, Vector2 max, Color bg, float fontSize, Color? textColor = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            ApplyRoundedSprite(go.AddComponent<Image>(), bg);
            var btn = go.AddComponent<Button>();

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var lr = labelGO.AddComponent<RectTransform>();
            lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
            lr.offsetMin = lr.offsetMax = Vector2.zero;
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = textColor ?? Color.white;

            return btn;
        }

        // A rounded-corner panel (Unity's built-in sliced UISprite). Used for the reflection card,
        // the Recording pill, the divider, the instruction box, and buttons.
        static GameObject CreateCard(string name, GameObject parent, Vector2 min, Vector2 max, Color bg)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            ApplyRoundedSprite(go.AddComponent<Image>(), bg);
            return go;
        }

        static void ApplyRoundedSprite(Image img, Color color)
        {
            var spr = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (spr != null) { img.sprite = spr; img.type = Image.Type.Sliced; }
            img.color = color;
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
