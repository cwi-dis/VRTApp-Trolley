using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Replaces the text "A"/"B" labels on the decision buttons with arrow graphics:
    ///   Button_TrackA Variant → straight-up arrow  (stay on the main track / inaction)
    ///   Button_TrackB Variant → elbow-right arrow   (divert / switch track / action)
    ///
    ///   Trolley > Buttons – Swap A/B Labels To Arrow Graphics
    ///
    /// Because both buttons are shared prefab VARIANTS, editing the two prefab assets propagates the
    /// graphic to every scene that instances them (Bystander, Driver, Self-harm, both tutorials).
    ///
    /// For each variant the script:
    ///   • disables the TextMeshPro "A"/"B" label (kept, not deleted, so it's easy to revert),
    ///   • creates/refreshes a child quad "ArrowIcon" anchored on the label's transform (known-good
    ///     position + facing), sized from the label's text box, with an unlit-transparent URP material
    ///     sampling the arrow PNG. White fill reads on both the grey (unselected) and green (selected)
    ///     button states.
    ///
    /// The "Button" face renderer that <see cref="TrolleyToggleDecision"/> recolours by name is left
    /// untouched, so selection recolouring still works behind the icon.
    ///
    /// Re-runnable (idempotent — reuses the existing ArrowIcon / material). I cannot render in-Editor,
    /// so after running, eyeball the icon size/facing on a button and tune ICON_FILL / the forward
    /// offset if needed; the change lives on the prefab and updates all scenes at once.
    /// </summary>
    public static class TrolleyButtonGraphicSetup
    {
        const string TrackAPrefab = "Assets/Trolley/Prefabs/Button_TrackA Variant.prefab";
        const string TrackBPrefab = "Assets/Trolley/Prefabs/Button_TrackB Variant.prefab";
        const string ArrowATex    = "Assets/Trolley/Textures/button_arrow_straight.png";
        const string ArrowBTex    = "Assets/Trolley/Textures/button_arrow_divert.png";
        const string MaterialsDir = "Assets/Trolley/Materials";

        // Fraction of the label's text box the icon quad fills, and how far (local units) it sits in
        // front of the label so it never z-fights the button face.
        const float ICON_FILL    = 1.0f;
        const float FWD_OFFSET   = 0.002f;

        [MenuItem("Trolley/Buttons – Swap A_B Labels To Arrow Graphics")]
        public static void SwapLabels()
        {
            int ok = 0;
            ok += Apply(TrackAPrefab, ArrowATex, "ArrowA") ? 1 : 0;
            ok += Apply(TrackBPrefab, ArrowBTex, "ArrowB") ? 1 : 0;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ButtonGraphic] Done — {ok}/2 buttons updated. Verify icon size/facing on a " +
                      "button in any scene; tune ICON_FILL / FWD_OFFSET in TrolleyButtonGraphicSetup if needed.");
        }

        static bool Apply(string prefabPath, string texPath, string matName)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex == null) { Debug.LogError($"[ButtonGraphic] Texture not found: {texPath}"); return false; }

            var mat  = GetOrCreateIconMaterial(matName, tex);
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var label = root.GetComponentInChildren<TMP_Text>(true);
                if (label == null) { Debug.LogError($"[ButtonGraphic] No TMP label in {prefabPath}"); return false; }

                // Hide (don't destroy) the A/B text so a revert is trivial.
                label.enabled = false;

                var parent = label.transform.parent != null ? label.transform.parent : label.transform;
                var existing = parent.Find("ArrowIcon");
                GameObject icon;
                if (existing != null)
                {
                    icon = existing.gameObject;
                }
                else
                {
                    icon = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    icon.name = "ArrowIcon";
                    var col = icon.GetComponent<Collider>();
                    if (col != null) Object.DestroyImmediate(col);   // never block the XR interactable
                }

                icon.layer = label.gameObject.layer;
                icon.transform.SetParent(parent, false);
                // Anchor on the label transform (already correctly placed + facing the viewer),
                // nudged forward so it sits just in front of the button face.
                icon.transform.localPosition = label.transform.localPosition + label.transform.localRotation * (Vector3.forward * FWD_OFFSET);
                icon.transform.localRotation = label.transform.localRotation;

                // Size from the label's text box (sizeDelta in the label's local units).
                var rt = label.rectTransform;
                Vector2 box = Vector2.Scale(rt.sizeDelta, new Vector2(rt.localScale.x, rt.localScale.y)) * ICON_FILL;
                if (box.x <= 0f || box.y <= 0f) box = new Vector2(0.08f, 0.08f);  // fallback if no rect
                icon.transform.localScale = new Vector3(box.x, box.y, 1f);

                var mr = icon.GetComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log($"[ButtonGraphic] {Path.GetFileName(prefabPath)} → icon '{icon.name}' " +
                          $"scale {icon.transform.localScale} (label box {box}).");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static Material GetOrCreateIconMaterial(string matName, Texture2D tex)
        {
            string path = $"{MaterialsDir}/{matName}_Icon.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            // Sprites/Default is a built-in, URP-compatible, unlit + alpha-blended shader. It's the most
            // reliable choice for a white-on-transparent decal — URP/Unlit needs surface-type keywords
            // that render MAGENTA if they don't resolve, which is the "pink button" symptom.
            var shader = Shader.Find("Sprites/Default")
                      ?? Shader.Find("Universal Render Pipeline/Unlit")
                      ?? Shader.Find("Unlit/Transparent");
            if (shader == null)
            {
                Debug.LogError("[ButtonGraphic] No usable transparent shader found for the icon material.");
                return mat;
            }

            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;                 // force-reset (repairs an earlier magenta material)
            mat.color  = Color.white;            // Sprites/Default tint = _Color
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);   // in case of URP/Unlit
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
