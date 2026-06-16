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
    /// Run once via menu: Trolley > Wire Avatar Setup Scene
    /// Builds two side-by-side avatar stations on one world-space canvas.
    /// Station A = P1 / master.  Station B = P2 / non-master (paired only).
    /// After running, assign masculinePreview / femininePreview / bodyRenderers / hairRenderers
    /// on each AvatarSelector in the Inspector once FBX models are added under the preview parents.
    /// </summary>
    public static class TrolleyAvatarSetupSceneSetup
    {
        const string SourceScene = "Assets/Trolley/Scenes/TrolleyResearcherSetup.unity";
        const string TargetScene = "Assets/Trolley/Scenes/TrolleyAvatarSetup.unity";
        const string MenuItem    = "Trolley/Wire Avatar Setup Scene";

        static readonly Color PanelBg     = new Color(0.08f, 0.08f, 0.08f, 0.97f);
        static readonly Color BtnColor    = new Color(0.20f, 0.20f, 0.50f);
        static readonly Color ConfirmColor = new Color(0.10f, 0.55f, 0.10f);
        static readonly Color P1Color     = new Color(0.10f, 0.30f, 0.60f);
        static readonly Color P2Color     = new Color(0.50f, 0.20f, 0.10f);

        // Swatch button colors — shown on the UI, saturated for clarity
        static readonly Color[] SkinToneSwatches =
        {
            new Color(1.00f, 0.86f, 0.71f),
            new Color(0.91f, 0.73f, 0.60f),
            new Color(0.78f, 0.52f, 0.26f),
            new Color(0.63f, 0.32f, 0.18f),
            new Color(0.42f, 0.23f, 0.16f),
            new Color(0.23f, 0.12f, 0.10f),
        };

        // Tint colors — applied to avatar via MaterialPropertyBlock, lighter to preserve texture
        static readonly Color[] SkinTones =
        {
            new Color(1.00f, 1.00f, 1.00f),
            new Color(1.00f, 0.93f, 0.85f),
            new Color(1.00f, 0.84f, 0.68f),
            new Color(0.95f, 0.72f, 0.50f),
            new Color(0.78f, 0.52f, 0.32f),
            new Color(0.52f, 0.30f, 0.18f),
        };

        // Swatch button colors — shown on the UI
        static readonly Color[] HairColorSwatches =
        {
            new Color(0.10f, 0.10f, 0.10f),
            new Color(0.23f, 0.17f, 0.10f),
            new Color(0.42f, 0.30f, 0.16f),
            new Color(0.77f, 0.64f, 0.35f),
            new Color(0.55f, 0.23f, 0.17f),
            new Color(0.63f, 0.63f, 0.63f),
        };

        // Tint colors — applied to avatar via MaterialPropertyBlock
        static readonly Color[] HairColors =
        {
            new Color(0.20f, 0.20f, 0.20f),
            new Color(0.42f, 0.30f, 0.18f),
            new Color(0.65f, 0.48f, 0.28f),
            new Color(0.92f, 0.78f, 0.48f),
            new Color(0.75f, 0.35f, 0.25f),
            new Color(0.75f, 0.75f, 0.75f),
        };

        [MenuItem(MenuItem)]
        public static void WireScene()
        {
            if (!System.IO.File.Exists(TargetScene))
            {
                if (!AssetDatabase.CopyAsset(SourceScene, TargetScene))
                {
                    Debug.LogError($"TrolleyAvatarSetupSceneSetup: could not copy {SourceScene}.");
                    return;
                }
                AssetDatabase.Refresh();
            }

            var scene = EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Single);

            foreach (string n in new[] {
                "ResearcherCanvas", "AvatarCanvas", "AvatarPreviews",
                "ResearcherSetupController", "AvatarSetupController",
                "AvatarSelector_A", "AvatarSelector_B",
                "PracticeButton",
                "AvatarSetupLight", "AvatarSetupFillLight",
                "TransitionReadyTrigger", "TransitionBarrier", "TransitionProceedTrigger" })
            {
                var existing = GameObject.Find(n);
                if (existing != null) Object.DestroyImmediate(existing);
            }

            // ── Directional lights ────────────────────────────────────────────
            var lightGO = new GameObject("AvatarSetupLight");
            lightGO.AddComponent<ManagedBySetupScript>().menuItem = MenuItem;
            var light = lightGO.AddComponent<Light>();
            light.type       = LightType.Directional;
            light.intensity  = 1.2f;
            light.color      = new Color(1f, 0.97f, 0.92f);
            light.lightmapBakeType = LightmapBakeType.Realtime;
            lightGO.transform.rotation = Quaternion.Euler(50f, 150f, 0f);

            var fillLightGO = new GameObject("AvatarSetupFillLight");
            fillLightGO.AddComponent<ManagedBySetupScript>().menuItem = MenuItem;
            var fillLight = fillLightGO.AddComponent<Light>();
            fillLight.type       = LightType.Directional;
            fillLight.intensity  = 0.4f;
            fillLight.color      = new Color(0.85f, 0.90f, 1f);
            fillLight.lightmapBakeType = LightmapBakeType.Realtime;
            fillLightGO.transform.rotation = Quaternion.Euler(30f, -30f, 0f);

            // ── Wide canvas holding both stations ────────────────────────────
            var canvasGO = new GameObject("AvatarCanvas");
            canvasGO.AddComponent<ManagedBySetupScript>().menuItem = MenuItem;
            canvasGO.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            canvasGO.AddComponent<TrackedDeviceGraphicRaycaster>();
            var cRect = canvasGO.GetComponent<RectTransform>();
            cRect.sizeDelta = new Vector2(1440f, 680f);
            canvasGO.transform.position   = new Vector3(0f, 1.6f, 2f);
            canvasGO.transform.rotation   = Quaternion.Euler(0f, 180f, 0f);
            canvasGO.transform.localScale = Vector3.one * 0.003f;

            // ── Build Station A (left half) ──────────────────────────────────
            var stationARoot = MakeSubPanel("StationA", canvasGO,
                new Vector2(0.01f, 0f), new Vector2(0.49f, 1f), PanelBg);

            Button mascBtnA, femBtnA, confirmBtnA;
            Button[] skinBtnsA, hairBtnsA;
            TextMeshProUGUI statusA;
            BuildStationPanel(stationARoot, "P1", P1Color,
                out mascBtnA, out femBtnA, out skinBtnsA, out hairBtnsA,
                out statusA, out confirmBtnA);

            // ── Build Station B (right half) ─────────────────────────────────
            var stationBRoot = MakeSubPanel("StationB", canvasGO,
                new Vector2(0.51f, 0f), new Vector2(0.99f, 1f), PanelBg);

            Button mascBtnB, femBtnB, confirmBtnB;
            Button[] skinBtnsB, hairBtnsB;
            TextMeshProUGUI statusB;
            BuildStationPanel(stationBRoot, "P2", P2Color,
                out mascBtnB, out femBtnB, out skinBtnsB, out hairBtnsB,
                out statusB, out confirmBtnB);

            // ── Avatar preview placeholder parents ────────────────────────────
            // Place them in front of each station (facing toward participants).
            // Add your FBX models as children after running this script.
            var previewsRoot = new GameObject("AvatarPreviews");
            previewsRoot.AddComponent<ManagedBySetupScript>().menuItem = MenuItem;

            var previewA = new GameObject("AvatarPreview_A");
            previewA.transform.SetParent(previewsRoot.transform, false);
            previewA.transform.position = new Vector3(-1.1f, 0f, 3.2f);
            previewA.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // facing participant

            var previewB = new GameObject("AvatarPreview_B");
            previewB.transform.SetParent(previewsRoot.transform, false);
            previewB.transform.position = new Vector3(1.1f, 0f, 3.2f);
            previewB.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            // ── AvatarSelector A ──────────────────────────────────────────────
            var selAGO = new GameObject("AvatarSelector_A");
            selAGO.AddComponent<ManagedBySetupScript>().menuItem = MenuItem;
            var selA = selAGO.AddComponent<AvatarSelector>();
            WireSelector(selA, mascBtnA, femBtnA, skinBtnsA, hairBtnsA, 0);

            // ── AvatarSelector B ──────────────────────────────────────────────
            var selBGO = new GameObject("AvatarSelector_B");
            selBGO.AddComponent<ManagedBySetupScript>().menuItem = MenuItem;
            var selB = selBGO.AddComponent<AvatarSelector>();
            WireSelector(selB, mascBtnB, femBtnB, skinBtnsB, hairBtnsB, 1);

            // ── AvatarSetupController ─────────────────────────────────────────
            var controllerGO = new GameObject("AvatarSetupController");
            controllerGO.AddComponent<ManagedBySetupScript>().menuItem = MenuItem;
            var controller = controllerGO.AddComponent<AvatarSetupController>();

            var cSO = new SerializedObject(controller);
            cSO.FindProperty("stationARoot").objectReferenceValue = stationARoot;
            cSO.FindProperty("selectorA").objectReferenceValue    = selA;
            cSO.FindProperty("confirmButtonA").objectReferenceValue = confirmBtnA;
            cSO.FindProperty("statusTextA").objectReferenceValue  = statusA;
            cSO.FindProperty("stationBRoot").objectReferenceValue = stationBRoot;
            cSO.FindProperty("selectorB").objectReferenceValue    = selB;
            cSO.FindProperty("confirmButtonB").objectReferenceValue = confirmBtnB;
            cSO.FindProperty("statusTextB").objectReferenceValue  = statusB;
            TrolleySetupBarrierUtils.AddTransitionBarrier(cSO, MenuItem);
            cSO.ApplyModifiedProperties();

            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("TrolleyAvatarSetupSceneSetup: done.\n" +
                      "TODO: add FBX children under AvatarPreview_A / AvatarPreview_B, " +
                      "then assign masculinePreview, femininePreview, bodyRenderers, hairRenderers " +
                      "on AvatarSelector_A and AvatarSelector_B in the Inspector.");
        }

        // ── Station builder ───────────────────────────────────────────────────

        static void BuildStationPanel(GameObject parent, string playerLabel, Color headerColor,
            out Button mascBtn, out Button femBtn,
            out Button[] skinBtns, out Button[] hairBtns,
            out TextMeshProUGUI statusText, out Button confirmBtn)
        {
            // Header bar
            var header = new GameObject("Header");
            header.transform.SetParent(parent.transform, false);
            var hr = header.AddComponent<RectTransform>();
            hr.anchorMin = new Vector2(0f, 0.90f); hr.anchorMax = Vector2.one;
            hr.offsetMin = hr.offsetMax = Vector2.zero;
            header.AddComponent<Image>().color = headerColor;
            MakeLabel("PlayerLabel", header, playerLabel,
                Vector2.zero, Vector2.one, 36);

            MakeLabel("Title", parent, "CHOOSE YOUR AVATAR",
                new Vector2(0.02f, 0.79f), new Vector2(0.98f, 0.89f), 26);

            // Body type
            MakeLabel("BodyLabel", parent, "Body type",
                new Vector2(0.02f, 0.68f), new Vector2(0.38f, 0.77f), 22);
            mascBtn = MakeButton($"MasculineButton_{playerLabel}", parent, "Masculine",
                new Vector2(0.40f, 0.68f), new Vector2(0.69f, 0.77f));
            femBtn = MakeButton($"FeminineButton_{playerLabel}", parent, "Feminine",
                new Vector2(0.71f, 0.68f), new Vector2(0.98f, 0.77f));

            // Skin tone
            MakeLabel("SkinLabel", parent, "Skin tone",
                new Vector2(0.02f, 0.56f), new Vector2(0.38f, 0.65f), 22);
            skinBtns = new Button[6];
            for (int i = 0; i < 6; i++)
            {
                float x0 = 0.40f + i * 0.098f;
                skinBtns[i] = MakeSwatch($"SkinTone_{playerLabel}_{i}", parent, SkinToneSwatches[i],
                    new Vector2(x0, 0.56f), new Vector2(x0 + 0.088f, 0.65f));
            }

            // Hair colour
            MakeLabel("HairLabel", parent, "Hair colour",
                new Vector2(0.02f, 0.44f), new Vector2(0.38f, 0.53f), 22);
            hairBtns = new Button[6];
            for (int i = 0; i < 6; i++)
            {
                float x0 = 0.40f + i * 0.098f;
                hairBtns[i] = MakeSwatch($"HairColor_{playerLabel}_{i}", parent, HairColorSwatches[i],
                    new Vector2(x0, 0.44f), new Vector2(x0 + 0.088f, 0.53f));
            }

            // Status
            statusText = MakeTMPLabel("StatusText_" + playerLabel, parent,
                "Customise your avatar, then press Confirm.",
                new Vector2(0.02f, 0.20f), new Vector2(0.98f, 0.30f), 22);
            statusText.color     = new Color(0.75f, 0.75f, 0.75f);
            statusText.alignment = TextAlignmentOptions.Center;

            // Confirm
            confirmBtn = MakeButton($"ConfirmButton_{playerLabel}", parent, "CONFIRM",
                new Vector2(0.15f, 0.05f), new Vector2(0.85f, 0.17f),
                fontSize: 30, bgColor: ConfirmColor);
        }

        // ── Selector wiring ───────────────────────────────────────────────────

        static void WireSelector(AvatarSelector selector,
            Button mascBtn, Button femBtn, Button[] skinBtns, Button[] hairBtns, int playerIndex)
        {
            var so = new SerializedObject(selector);
            so.FindProperty("playerIndex").intValue = playerIndex;
            so.FindProperty("masculineButton").objectReferenceValue = mascBtn;
            so.FindProperty("feminineButton").objectReferenceValue  = femBtn;

            var skinBtnProp = so.FindProperty("skinToneButtons");
            skinBtnProp.arraySize = 6;
            for (int i = 0; i < 6; i++)
                skinBtnProp.GetArrayElementAtIndex(i).objectReferenceValue = skinBtns[i];

            var hairBtnProp = so.FindProperty("hairColorButtons");
            hairBtnProp.arraySize = 6;
            for (int i = 0; i < 6; i++)
                hairBtnProp.GetArrayElementAtIndex(i).objectReferenceValue = hairBtns[i];

            var skinColorProp = so.FindProperty("skinToneColors");
            skinColorProp.arraySize = SkinTones.Length;
            for (int i = 0; i < SkinTones.Length; i++)
                skinColorProp.GetArrayElementAtIndex(i).colorValue = SkinTones[i];

            var hairColorProp = so.FindProperty("hairColors");
            hairColorProp.arraySize = HairColors.Length;
            for (int i = 0; i < HairColors.Length; i++)
                hairColorProp.GetArrayElementAtIndex(i).colorValue = HairColors[i];

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(selector);
        }

        // ── UI helpers ────────────────────────────────────────────────────────

        static GameObject MakeSubPanel(string name, GameObject parent,
            Vector2 anchorMin, Vector2 anchorMax, Color bg)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = anchorMin; r.anchorMax = anchorMax;
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
            tmp.alignment = TextAlignmentOptions.Center;
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
            Vector2 min, Vector2 max, float fontSize = 24, Color? bgColor = null)
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
