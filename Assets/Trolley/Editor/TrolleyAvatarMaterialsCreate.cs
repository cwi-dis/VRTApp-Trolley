using UnityEditor;
using UnityEngine;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Run once via menu: Trolley > Create Avatar Materials
    /// Creates 6 skin tone and 6 hair colour materials in Assets/Trolley/Materials/.
    /// Uses URP Lit shader. If materials turn pink, select them and switch shader manually.
    /// </summary>
    public static class TrolleyAvatarMaterialsCreate
    {
        const string OutputFolder = "Assets/Trolley/Materials";

        static readonly (string name, Color color)[] SkinTones =
        {
            ("SkinTone_0_Light",       new Color(1.00f, 0.86f, 0.71f)),
            ("SkinTone_1_LightMedium", new Color(0.91f, 0.73f, 0.60f)),
            ("SkinTone_2_Medium",      new Color(0.78f, 0.52f, 0.26f)),
            ("SkinTone_3_MediumDark",  new Color(0.63f, 0.32f, 0.18f)),
            ("SkinTone_4_Dark",        new Color(0.42f, 0.23f, 0.16f)),
            ("SkinTone_5_Darkest",     new Color(0.23f, 0.12f, 0.10f)),
        };

        static readonly (string name, Color color)[] HairColors =
        {
            ("Hair_0_Black",      new Color(0.10f, 0.10f, 0.10f)),
            ("Hair_1_DarkBrown",  new Color(0.23f, 0.17f, 0.10f)),
            ("Hair_2_Brown",      new Color(0.42f, 0.30f, 0.16f)),
            ("Hair_3_Blonde",     new Color(0.77f, 0.64f, 0.35f)),
            ("Hair_4_Auburn",     new Color(0.55f, 0.23f, 0.17f)),
            ("Hair_5_Grey",       new Color(0.63f, 0.63f, 0.63f)),
        };

        [MenuItem("Trolley/Create Avatar Materials")]
        public static void CreateMaterials()
        {
            // Clone an existing working material so we inherit the correct shader
            var baseMat = AssetDatabase.LoadAssetAtPath<Material>($"{OutputFolder}/M_Lever.mat");
            if (baseMat == null)
            {
                Debug.LogError("TrolleyAvatarMaterialsCreate: could not find M_Lever.mat as base. " +
                               "Place any working .mat in Assets/Trolley/Materials/ and update the path.");
                return;
            }

            foreach (var (name, color) in SkinTones)
                CreateMaterial(baseMat, name, color);

            foreach (var (name, color) in HairColors)
                CreateMaterial(baseMat, name, color);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("TrolleyAvatarMaterialsCreate: 12 materials created in " + OutputFolder);
        }

        static void CreateMaterial(Material baseMat, string name, Color color)
        {
            string path     = $"{OutputFolder}/{name}.mat";
            string srcPath  = AssetDatabase.GetAssetPath(baseMat);

            if (AssetDatabase.LoadAssetAtPath<Material>(path) == null)
                AssetDatabase.CopyAsset(srcPath, path);

            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { Debug.LogError($"Could not load {path}"); return; }
            mat.SetColor("_Color",     color); // Standard
            mat.SetColor("_BaseColor", color); // URP Lit
            // Clear all textures inherited from the base material
            foreach (var prop in mat.GetTexturePropertyNames())
                mat.SetTexture(prop, null);
            EditorUtility.SetDirty(mat);
        }
    }
}
