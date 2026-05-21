using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class DriverRoomMaterialSetup : Editor
{
    [MenuItem("Trolley/Diagnose Material Matching")]
    static void DiagnoseMaterials()
    {
        GameObject root = Selection.activeGameObject;
        if (root == null) { Debug.LogError("[Trolley] Select the root model GameObject first."); return; }

        var matMap = LoadAllMaterials();
        Debug.Log($"=== All materials found ({matMap.Count}) ===");
        foreach (var k in matMap.Keys) Debug.Log("  MAT: " + k);

        Debug.Log($"=== MeshRenderers under '{root.name}' ===");
        var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        if (renderers.Length == 0) Debug.LogWarning("  No MeshRenderers found — model may not be in scene or uses SkinnedMeshRenderer");
        foreach (MeshRenderer mr in renderers)
        {
            Material best = FindBestMatch(matMap, mr.gameObject.name);
            Debug.Log($"  CHILD: '{mr.gameObject.name}' → best match: '{best?.name ?? "NONE"}'");
        }
    }

    // Remap FBX materials via ModelImporter — select the FBX file in Project panel, not the scene object
    [MenuItem("Trolley/Apply Materials to Selected FBX")]
    static void ApplyMaterials()
    {
        Object selected = Selection.activeObject;
        if (selected == null) { Debug.LogError("[Trolley] Select the FBX file in the Project panel."); return; }

        string fbxPath = AssetDatabase.GetAssetPath(selected);
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null) { Debug.LogError($"[Trolley] Not an FBX: {fbxPath}"); return; }

        var matMap = LoadAllMaterials();

        // Get all embedded material names from the FBX
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        int assigned = 0;
        foreach (Object asset in subAssets)
        {
            if (!(asset is Material embeddedMat)) continue;
            string embeddedName = embeddedMat.name;

            Material found = FindBestMatch(matMap, embeddedName);
            if (found != null)
            {
                var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), embeddedName);
                importer.AddRemap(id, found);
                assigned++;
                Debug.Log($"  FBX mat '{embeddedName}' → {found.name}");
            }
            else
                Debug.LogWarning($"  FBX mat '{embeddedName}' → no match found");
        }

        importer.SaveAndReimport();
        Debug.Log($"[Trolley] Remapped {assigned} materials on {Path.GetFileName(fbxPath)}");
    }

    static Dictionary<string, Material> LoadAllMaterials()
    {
        var matMap = new Dictionary<string, Material>();
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Trolley/Models" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            string key = Path.GetFileNameWithoutExtension(path).ToLower();
            matMap[key] = mat;
        }
        return matMap;
    }

    static Material FindBestMatch(Dictionary<string, Material> matMap, string childName)
    {
        string lower = childName.ToLower(); // e.g. "m_controls"

        // Strip leading "m_" if present to get the base name
        string baseName = lower.StartsWith("m_") ? lower.Substring(2) : lower; // e.g. "controls"

        string[] candidates = {
            lower,                  // exact: m_controls
            $"m_t_{baseName}",      // m_t_controls
            $"m_{baseName}",        // m_controls
            $"m_t_{lower}",         // m_t_m_controls (unlikely but safe)
        };

        foreach (string c in candidates)
            if (matMap.TryGetValue(c, out var m)) return m;

        // Fallback: any material whose name contains the base name
        foreach (var kvp in matMap)
            if (kvp.Key.Contains(baseName)) return kvp.Value;

        // Manual fallbacks for missing textures
        var fallbacks = new Dictionary<string, string> {
            { "plasticgloss_black",  "m_t_plasticgrainy_black" },
            { "plasticrough_grey",   "m_t_metalgrainy_black" },
        };
        if (fallbacks.TryGetValue(baseName, out var fallback) && matMap.TryGetValue(fallback, out var fm))
            return fm;

        return null;
    }

    // 1930s train cabin: partName_basecolour, partName_normal, etc.
    [MenuItem("Trolley/Setup DriverRoom Materials")]
    static void SetupDriverRoom()
    {
        SetupMaterials_Sketchfab(
            "Assets/Trolley/Models/1930s-train-cabin/textures",
            "Assets/Trolley/Models/1930s-train-cabin/Materials"
        );
    }

    // Desiro console: T_PartName_BC, T_PartName_N, T_PartName_E, etc.
    [MenuItem("Trolley/Setup ControlRoom Materials")]
    static void SetupControlRoom()
    {
        SetupMaterials_Desiro(
            "Assets/Trolley/Models/desiro-45-lastochka-train-driver-console/textures",
            "Assets/Trolley/Models/desiro-45-lastochka-train-driver-console/Materials"
        );
    }

    // --- DriverRoom (1930s cabin): suffix = basecolour / normal / roughness / metal / shadows / opacity ---
    static void SetupMaterials_Sketchfab(string texturesPath, string materialsPath)
    {
        EnsureFolder(materialsPath);

        var parts = new Dictionary<string, Dictionary<string, Texture2D>>();
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { texturesPath }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string filename = Path.GetFileNameWithoutExtension(path);
            int sep = filename.LastIndexOf('_');
            if (sep < 0) continue;
            string part = filename.Substring(0, sep);
            string map  = filename.Substring(sep + 1).ToLower();
            if (!parts.ContainsKey(part)) parts[part] = new Dictionary<string, Texture2D>();
            parts[part][map] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        int count = 0;
        foreach (var kvp in parts)
        {
            string part = kvp.Key;
            var maps = kvp.Value;
            Material mat = GetOrCreateMaterial($"{materialsPath}/M_{part}.mat");

            if (maps.TryGetValue("basecolour", out var bc))  { mat.SetTexture("_MainTex", bc); mat.SetColor("_Color", Color.white); }
            if (maps.TryGetValue("normal",     out var n))   { SetNormalMap(n); mat.SetTexture("_BumpMap", n); mat.EnableKeyword("_NORMALMAP"); }
            if (maps.TryGetValue("metal",      out var m))   mat.SetTexture("_MetallicGlossMap", m);
            if (maps.TryGetValue("shadows",    out var ao))  mat.SetTexture("_OcclusionMap", ao);

            // Windows: transparent
            if (part == "windows" && maps.TryGetValue("opacity", out var op))
            {
                mat.SetFloat("_Mode", 2); // Fade
                mat.SetTexture("_MainTex", op);
                mat.renderQueue = 3000;
                mat.EnableKeyword("_ALPHABLEND_ON");
            }

            EditorUtility.SetDirty(mat);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[DriverRoom] {count} materials created/updated in {materialsPath}");
    }

    // --- ControlRoom (Desiro console): prefix T_, suffix BC / N / R / M / AO / E / O ---
    static void SetupMaterials_Desiro(string texturesPath, string materialsPath)
    {
        EnsureFolder(materialsPath);

        var parts = new Dictionary<string, Dictionary<string, Texture2D>>();
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { texturesPath }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string filename = Path.GetFileNameWithoutExtension(path); // e.g. T_Controls_BC
            int sep = filename.LastIndexOf('_');
            if (sep < 0) continue;
            string part = filename.Substring(0, sep); // T_Controls
            string map  = filename.Substring(sep + 1).ToUpper(); // BC
            if (!parts.ContainsKey(part)) parts[part] = new Dictionary<string, Texture2D>();
            parts[part][map] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        int count = 0;
        foreach (var kvp in parts)
        {
            string part = kvp.Key;
            var maps = kvp.Value;
            Material mat = GetOrCreateMaterial($"{materialsPath}/M_{part}.mat");

            if (maps.TryGetValue("BC", out var bc))  { mat.SetTexture("_MainTex", bc); mat.SetColor("_Color", Color.white); }
            if (maps.TryGetValue("N",  out var n))   { SetNormalMap(n); mat.SetTexture("_BumpMap", n); mat.EnableKeyword("_NORMALMAP"); }
            if (maps.TryGetValue("M",  out var m))   mat.SetTexture("_MetallicGlossMap", m);
            if (maps.TryGetValue("AO", out var ao))  mat.SetTexture("_OcclusionMap", ao);

            // Emission (display screens)
            if (maps.TryGetValue("E", out var e))
            {
                mat.SetTexture("_EmissionMap", e);
                mat.SetColor("_EmissionColor", Color.white);
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            // Transparent parts
            if (maps.TryGetValue("O", out var op))
            {
                mat.SetFloat("_Mode", 2); // Fade
                mat.SetTexture("_MainTex", op);
                mat.renderQueue = 3000;
                mat.EnableKeyword("_ALPHABLEND_ON");
            }

            EditorUtility.SetDirty(mat);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ControlRoom] {count} materials created/updated in {materialsPath}");
    }

    static Material GetOrCreateMaterial(string path)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    static void SetNormalMap(Texture2D tex)
    {
        string path = AssetDatabase.GetAssetPath(tex);
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null && imp.textureType != TextureImporterType.NormalMap)
        {
            imp.textureType = TextureImporterType.NormalMap;
            imp.SaveAndReimport();
        }
    }

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string folder = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
