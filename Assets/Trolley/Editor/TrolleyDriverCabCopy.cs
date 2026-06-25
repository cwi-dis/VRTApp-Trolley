using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Copies the DriverCab prefab instance from the Driver scene into the Self-harm and Driver-tutorial
    /// scenes at the SAME world transform, so all three share the identical cab.
    ///
    ///   Trolley > Copy DriverCab: Driver → Selfharm + TutorialDriver
    ///
    /// Reads the cab's world position/rotation/scale from Driver (so re-running picks up any later tweak)
    /// and applies it to each target. Re-runnable: if a DriverCab already exists in a target it just
    /// re-positions it instead of adding a duplicate.
    ///
    /// NON-DESTRUCTIVE: it deliberately does NOT remove the old DriverCabShell in the target scenes —
    /// that shell still holds the wired ControlPanel / decision objects there. After running this, remove
    /// the old DriverCabShell by hand per scene once you've confirmed nothing wired is lost.
    /// </summary>
    public static class TrolleyDriverCabCopy
    {
        const string DriverScene   = "Assets/Trolley/Scenes/TrolleyDriver.unity";
        const string SelfharmScene = "Assets/Trolley/Scenes/TrolleySelfharm.unity";
        const string TutorialScene = "Assets/Trolley/Scenes/TrolleyTutorialDriver.unity";
        const string CabPrefab     = "Assets/Trolley/Prefabs/DriverCab.prefab";
        const string CabName       = "DriverCab";

        [MenuItem("Trolley/Copy DriverCab: Driver → Selfharm + TutorialDriver")]
        public static void CopyDriverCab()
        {
            // Don't lose unsaved work in the currently open scene.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // ── Read the cab's world transform from the Driver scene ──
            var src = EditorSceneManager.OpenScene(DriverScene, OpenSceneMode.Single);
            if (!src.IsValid()) { Debug.LogError($"Copy DriverCab: could not open {DriverScene}."); return; }
            var srcCab = FindCab(src);
            if (srcCab == null)
            {
                Debug.LogError($"Copy DriverCab: no root object named '{CabName}' found in Driver — nothing to copy.");
                return;
            }
            var t = srcCab.transform;
            Vector3 pos = t.position; Quaternion rot = t.rotation; Vector3 scale = t.localScale;

            // ── Place it in each target scene ──
            ApplyTo(SelfharmScene, pos, rot, scale);
            ApplyTo(TutorialScene, pos, rot, scale);
        }

        static void ApplyTo(string scenePath, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) { Debug.LogError($"Copy DriverCab: could not open {scenePath}."); return; }

            var cab = FindCab(scene);
            bool created = false;
            if (cab == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CabPrefab);
                if (prefab == null) { Debug.LogError($"Copy DriverCab: prefab not found at {CabPrefab}."); return; }
                cab = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                cab.name = CabName;
                created = true;
            }

            Undo.RecordObject(cab.transform, "Copy DriverCab");
            cab.transform.position   = pos;
            cab.transform.rotation   = rot;
            cab.transform.localScale = scale;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"Copy DriverCab: {(created ? "added" : "re-positioned")} '{CabName}' in {Path(scenePath)} " +
                      $"at {pos}, scale {scale} — saved.\n" +
                      "MANUAL: remove the old DriverCabShell in this scene once you've confirmed the wired " +
                      "ControlPanel / decision objects are preserved.");
        }

        // The cab is a root object named "DriverCab" (the prefab instance, or a plain copy).
        static GameObject FindCab(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == CabName) return root;
            return null;
        }

        static string Path(string assetPath) => System.IO.Path.GetFileNameWithoutExtension(assetPath);
    }
}
