using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using TMPro;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Run once via menu: Trolley > Add Avatar UI
    /// Adds a self-contained avatar selection canvas to the Tutorial scene.
    /// Safe to run without affecting existing researcher panel, audio, or other scene state.
    /// </summary>
    public static class TrolleyAvatarUISetup
    {
        const string ScenePath = "Assets/Trolley/Scenes/TrolleyTutorial.unity";

        // Skin tone swatches (light → dark)
        static readonly Color[] SkinTones =
        {
            new Color(1.00f, 0.86f, 0.71f),
            new Color(0.91f, 0.73f, 0.60f),
            new Color(0.78f, 0.52f, 0.26f),
            new Color(0.63f, 0.32f, 0.18f),
            new Color(0.42f, 0.23f, 0.16f),
            new Color(0.23f, 0.12f, 0.10f),
        };

        // Hair colour swatches
        static readonly Color[] HairColors =
        {
            new Color(0.10f, 0.10f, 0.10f),   // Black
            new Color(0.23f, 0.17f, 0.10f),   // Dark brown
            new Color(0.42f, 0.30f, 0.16f),   // Brown
            new Color(0.77f, 0.64f, 0.35f),   // Blonde
            new Color(0.55f, 0.23f, 0.17f),   // Red
            new Color(0.63f, 0.63f, 0.63f),   // Grey
        };

        static readonly Color PanelBg      = new Color(0.08f, 0.08f, 0.08f, 0.97f);
        static readonly Color UnselectedBtn = new Color(0.20f, 0.20f, 0.50f);

        [MenuItem("Trolley/Add Avatar UI")]
        public static void AddAvatarUI()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // Remove old AvatarCanvas if present so we can recreate cleanly.
            var existing = GameObject.Find("AvatarCanvas");
            if (existing != null) Object.DestroyImmediate(existing);

            // ── Canvas ────────────────────────────────────────────────────────
            var canvasGO = new GameObject("AvatarCanvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<TrackedDeviceGraphicRaycaster>();
            var canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.sizeDelta   = new Vector2(700f, 560f);
            canvasGO.transform.position   = new Vector3(0f, 1.6f, -1f);
            canvasGO.transform.rotation   = Quaternion.identity;
            canvasGO.transform.localScale = Vector3.one * 0.003f;

            // ── Panel ─────────────────────────────────────────────────────────
            var panel = MakePanel("AvatarPanel", canvasGO, PanelBg);

            // Title
            MakeLabel("Title", panel, "AVATAR",
                new Vector2(0f, 0.88f), new Vector2(1f, 1f), 40);

            // ── Body type ─────────────────────────────────────────────────────
            MakeLabel("BodyTypeLabel", panel, "Body type",
                new Vector2(0.02f, 0.76f), new Vector2(0.35f, 0.86f), 24);
            var mascBtn = MakeButton("MasculineButton", panel, "Masculine",
                new Vector2(0.36f, 0.76f), new Vector2(0.65f, 0.86f));
            var femBtn  = MakeButton("FeminineButton",  panel, "Feminine",
                new Vector2(0.67f, 0.76f), new Vector2(0.96f, 0.86f));

            // ── Skin tone ─────────────────────────────────────────────────────
            MakeLabel("SkinLabel", panel, "Skin tone",
                new Vector2(0.02f, 0.63f), new Vector2(0.35f, 0.73f), 24);
            var skinBtns = new Button[6];
            for (int i = 0; i < 6; i++)
            {
                float x0 = 0.36f + i * 0.103f;
                skinBtns[i] = MakeSwatch($"SkinTone_{i}", panel, SkinTones[i],
                    new Vector2(x0, 0.63f), new Vector2(x0 + 0.093f, 0.73f));
            }

            // ── Hair colour ───────────────────────────────────────────────────
            MakeLabel("HairLabel", panel, "Hair colour",
                new Vector2(0.02f, 0.50f), new Vector2(0.35f, 0.60f), 24);
            var hairBtns = new Button[6];
            for (int i = 0; i < 6; i++)
            {
                float x0 = 0.36f + i * 0.103f;
                hairBtns[i] = MakeSwatch($"HairColor_{i}", panel, HairColors[i],
                    new Vector2(x0, 0.50f), new Vector2(x0 + 0.093f, 0.60f));
            }

            // ── Height ────────────────────────────────────────────────────────
            MakeLabel("HeightLabel", panel, "Height",
                new Vector2(0.02f, 0.37f), new Vector2(0.35f, 0.47f), 24);
            var shortBtn  = MakeButton("ShortButton",  panel, "Short",
                new Vector2(0.36f, 0.37f), new Vector2(0.55f, 0.47f));
            var mediumBtn = MakeButton("MediumButton", panel, "Medium",
                new Vector2(0.57f, 0.37f), new Vector2(0.76f, 0.47f));
            var tallBtn   = MakeButton("TallButton",   panel, "Tall",
                new Vector2(0.78f, 0.37f), new Vector2(0.96f, 0.47f));

            // ── Wire AvatarSelector ───────────────────────────────────────────
            var avatarSelectorGO = GameObject.Find("AvatarSelector");
            if (avatarSelectorGO == null)
            {
                Debug.LogError("AddAvatarUI: AvatarSelector GameObject not found in scene. " +
                               "Run Trolley > Wire Tutorial Scene first.");
            }
            else
            {
                var selector = avatarSelectorGO.GetComponent<AvatarSelector>();
                if (selector == null)
                {
                    Debug.LogError("AddAvatarUI: AvatarSelector component not found.");
                }
                else
                {
                    var so = new SerializedObject(selector);
                    so.FindProperty("selectionPanel").objectReferenceValue    = panel;
                    so.FindProperty("masculineButton").objectReferenceValue   = mascBtn;
                    so.FindProperty("feminineButton").objectReferenceValue    = femBtn;
                    so.FindProperty("shortButton").objectReferenceValue       = shortBtn;
                    so.FindProperty("mediumButton").objectReferenceValue      = mediumBtn;
                    so.FindProperty("tallButton").objectReferenceValue        = tallBtn;

                    var skinProp = so.FindProperty("skinToneButtons");
                    skinProp.arraySize = 6;
                    for (int i = 0; i < 6; i++)
                        skinProp.GetArrayElementAtIndex(i).objectReferenceValue = skinBtns[i];

                    var hairProp = so.FindProperty("hairColorButtons");
                    hairProp.arraySize = 6;
                    for (int i = 0; i < 6; i++)
                        hairProp.GetArrayElementAtIndex(i).objectReferenceValue = hairBtns[i];

                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(selector);
                    Debug.Log("AddAvatarUI: AvatarSelector wired successfully.");
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("AddAvatarUI: AvatarCanvas added and saved.");
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

        static Button MakeButton(string name, GameObject parent, string label,
                                 Vector2 min, Vector2 max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = min; r.anchorMax = max;
            r.offsetMin = r.offsetMax = Vector2.zero;
            go.AddComponent<Image>().color = UnselectedBtn;
            var btn = go.AddComponent<Button>();
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var lr = labelGO.AddComponent<RectTransform>();
            lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
            lr.offsetMin = lr.offsetMax = Vector2.zero;
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = 26;
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
