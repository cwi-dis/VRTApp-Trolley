using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using TMPro;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Run once via menu: Trolley > Wire Avatar Setup Scene
    /// Duplicates TrolleyResearcherSetup (preserving VR2Gather / XR rig setup),
    /// strips the researcher UI, and replaces it with the avatar selection canvas.
    /// </summary>
    public static class TrolleyAvatarSetupSceneSetup
    {
        const string SourceScene = "Assets/Trolley/Scenes/TrolleyResearcherSetup.unity";
        const string TargetScene = "Assets/Trolley/Scenes/TrolleyAvatarSetup.unity";

        static readonly Color PanelBg     = new Color(0.08f, 0.08f, 0.08f, 0.97f);
        static readonly Color BtnColor    = new Color(0.20f, 0.20f, 0.50f);
        static readonly Color ConfirmColor = new Color(0.10f, 0.55f, 0.10f);

        static readonly Color[] SkinTones =
        {
            new Color(1.00f, 0.86f, 0.71f),
            new Color(0.91f, 0.73f, 0.60f),
            new Color(0.78f, 0.52f, 0.26f),
            new Color(0.63f, 0.32f, 0.18f),
            new Color(0.42f, 0.23f, 0.16f),
            new Color(0.23f, 0.12f, 0.10f),
        };

        static readonly Color[] HairColors =
        {
            new Color(0.10f, 0.10f, 0.10f),
            new Color(0.23f, 0.17f, 0.10f),
            new Color(0.42f, 0.30f, 0.16f),
            new Color(0.77f, 0.64f, 0.35f),
            new Color(0.55f, 0.23f, 0.17f),
            new Color(0.63f, 0.63f, 0.63f),
        };

        [MenuItem("Trolley/Wire Avatar Setup Scene")]
        public static void WireScene()
        {
            // ── Duplicate Tutorial scene ──────────────────────────────────────
            if (!System.IO.File.Exists(TargetScene))
            {
                bool copied = AssetDatabase.CopyAsset(SourceScene, TargetScene);
                if (!copied)
                {
                    Debug.LogError($"WireAvatarSetupScene: could not copy {SourceScene}. " +
                                   "Make sure TrolleyResearcherSetup.unity exists.");
                    return;
                }
                AssetDatabase.Refresh();
            }

            var scene = EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Single);

            // ── Strip Tutorial-specific objects ───────────────────────────────
            foreach (string n in new[] {
                "ResearcherCanvas", "AvatarCanvas",
                "ResearcherSetupController", "AvatarSelector", "AvatarSetupController",
                "PracticeLever", "PracticeButton" })
            {
                var go = GameObject.Find(n);
                if (go != null) Object.DestroyImmediate(go);
            }

            // ── Avatar canvas ─────────────────────────────────────────────────
            const string menuItem = "Trolley/Wire Avatar Setup Scene";

            var canvasGO = new GameObject("AvatarCanvas");
            canvasGO.AddComponent<ManagedBySetupScript>().menuItem = menuItem;
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            canvasGO.AddComponent<TrackedDeviceGraphicRaycaster>();
            var cRect = canvasGO.GetComponent<RectTransform>();
            cRect.sizeDelta = new Vector2(700f, 620f);
            canvasGO.transform.position   = new Vector3(0f, 1.6f, 2f);
            canvasGO.transform.rotation   = Quaternion.Euler(0f, 180f, 0f);
            canvasGO.transform.localScale = Vector3.one * 0.003f;

            var panel = MakePanel("AvatarPanel", canvasGO, PanelBg);

            MakeLabel("Title", panel, "CHOOSE YOUR AVATAR",
                new Vector2(0f, 0.90f), new Vector2(1f, 1f), 38);

            // Body type
            MakeLabel("BodyLabel", panel, "Body type",
                new Vector2(0.02f, 0.79f), new Vector2(0.36f, 0.88f), 24);
            var mascBtn = MakeButton("MasculineButton", panel, "Masculine",
                new Vector2(0.37f, 0.79f), new Vector2(0.66f, 0.88f));
            var femBtn  = MakeButton("FeminineButton",  panel, "Feminine",
                new Vector2(0.68f, 0.79f), new Vector2(0.97f, 0.88f));

            // Skin tone
            MakeLabel("SkinLabel", panel, "Skin tone",
                new Vector2(0.02f, 0.67f), new Vector2(0.36f, 0.76f), 24);
            var skinBtns = new Button[6];
            for (int i = 0; i < 6; i++)
            {
                float x0 = 0.37f + i * 0.103f;
                skinBtns[i] = MakeSwatch($"SkinTone_{i}", panel, SkinTones[i],
                    new Vector2(x0, 0.67f), new Vector2(x0 + 0.093f, 0.76f));
            }

            // Hair colour
            MakeLabel("HairLabel", panel, "Hair colour",
                new Vector2(0.02f, 0.55f), new Vector2(0.36f, 0.64f), 24);
            var hairBtns = new Button[6];
            for (int i = 0; i < 6; i++)
            {
                float x0 = 0.37f + i * 0.103f;
                hairBtns[i] = MakeSwatch($"HairColor_{i}", panel, HairColors[i],
                    new Vector2(x0, 0.55f), new Vector2(x0 + 0.093f, 0.64f));
            }

            // Status + Confirm
            var statusText = MakeTMPLabel("StatusText", panel,
                "Customise your avatar, then press Confirm.",
                new Vector2(0.02f, 0.20f), new Vector2(0.98f, 0.30f), 24);
            statusText.color     = new Color(0.8f, 0.8f, 0.8f);
            statusText.alignment = TextAlignmentOptions.Center;

            var confirmBtn = MakeButton("ConfirmButton", panel, "CONFIRM",
                new Vector2(0.25f, 0.05f), new Vector2(0.75f, 0.17f),
                fontSize: 34, bgColor: ConfirmColor);

            // ── AvatarSelector ────────────────────────────────────────────────
            var selectorGO = new GameObject("AvatarSelector");
            selectorGO.AddComponent<ManagedBySetupScript>().menuItem = menuItem;
            var selector   = selectorGO.AddComponent<AvatarSelector>();

            var sSO = new SerializedObject(selector);
            sSO.FindProperty("selectionPanel").objectReferenceValue  = panel;
            sSO.FindProperty("masculineButton").objectReferenceValue = mascBtn;
            sSO.FindProperty("feminineButton").objectReferenceValue  = femBtn;
            var skinProp = sSO.FindProperty("skinToneButtons");
            skinProp.arraySize = 6;
            for (int i = 0; i < 6; i++)
                skinProp.GetArrayElementAtIndex(i).objectReferenceValue = skinBtns[i];

            var hairProp = sSO.FindProperty("hairColorButtons");
            hairProp.arraySize = 6;
            for (int i = 0; i < 6; i++)
                hairProp.GetArrayElementAtIndex(i).objectReferenceValue = hairBtns[i];

            sSO.ApplyModifiedProperties();

            // ── AvatarSetupController ─────────────────────────────────────────
            var controllerGO = new GameObject("AvatarSetupController");
            controllerGO.AddComponent<ManagedBySetupScript>().menuItem = menuItem;
            var controller   = controllerGO.AddComponent<AvatarSetupController>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("WireAvatarSetupScene: done. Add TrolleyAvatarSetup to Build Settings.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        static GameObject MakePanel(string name, GameObject parent, Color bg)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            go.AddComponent<Image>().color = bg;
            return go;
        }

        static void MakeLabel(string name, GameObject parent, string text,
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

        static TextMeshProUGUI MakeTMPLabel(string name, GameObject parent, string text,
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

        static Button MakeButton(string name, GameObject parent, string label,
                                 Vector2 min, Vector2 max,
                                 float fontSize = 26, Color? bgColor = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = min; r.anchorMax = max;
            r.offsetMin = r.offsetMax = Vector2.zero;
            go.AddComponent<Image>().color = bgColor ?? BtnColor;
            var btn = go.AddComponent<Button>();
            var lGO = new GameObject("Label");
            lGO.transform.SetParent(go.transform, false);
            var lr = lGO.AddComponent<RectTransform>();
            lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
            lr.offsetMin = lr.offsetMax = Vector2.zero;
            var tmp = lGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = fontSize;
            tmp.color = Color.white; tmp.alignment = TextAlignmentOptions.Center;
            return btn;
        }

        static Button MakeSwatch(string name, GameObject parent, Color color,
                                 Vector2 min, Vector2 max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = min; r.anchorMax = max;
            r.offsetMin = r.offsetMax = Vector2.zero;
            go.AddComponent<Image>().color = color;
            return go.AddComponent<Button>();
        }
    }
}
