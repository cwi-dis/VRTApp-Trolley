using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Adds PFB_GazeTarget children to the named objects in the open scene.
    /// Safe to run multiple times — skips objects that already have the child.
    /// Does NOT save; review the result and adjust BoxCollider sizes, then save.
    /// Fully undoable (Ctrl+Z).
    ///
    ///   Trolley > Wire GazeTargets
    ///
    /// Covers all three main scenes from one menu item:
    ///   Bystander  — ActionTrackWorkers, InactionTrackWorkers
    ///   Driver     — ActionTrackWorkers, InactionTrackWorkers
    ///   Selfharm   — InactionTrackWorkers, Rock
    /// Objects not present in the open scene are silently skipped.
    /// </summary>
    public static class TrolleyGazeTargetSetup
    {
        const string PrefabPath = "Assets/Trolley/Prefabs/PFB_GazeTarget.prefab";

        static readonly (string parentName, string childName)[] Targets =
        {
            ("ActionTrackWorkers",   "GazeTarget_Workers_Single"),
            ("InactionTrackWorkers", "GazeTarget_Workers_Five"),
            ("Rock",                 "GazeTarget_Rock"),
        };

        [MenuItem("Trolley/Wire GazeTargets")]
        public static void WireGazeTargets()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"TrolleyGazeTargetSetup: prefab not found at {PrefabPath}");
                return;
            }

            int added = 0, skipped = 0;

            foreach (var (parentName, childName) in Targets)
            {
                var parent = GameObject.Find(parentName);
                if (parent == null) continue;

                var existingChild = parent.transform.Find(childName);
                if (existingChild != null)
                {
                    Debug.Log($"TrolleyGazeTargetSetup: {childName} already present under {GetPath(parent.transform)} — skipped.", existingChild.gameObject);
                    skipped++;
                    continue;
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
                Undo.RegisterCreatedObjectUndo(instance, "Wire GazeTargets");
                instance.name = childName;
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;

                EditorSceneManager.MarkSceneDirty(parent.scene);
                Debug.Log($"TrolleyGazeTargetSetup: added {childName} under {GetPath(parent.transform)}.", instance);
                added++;
            }

            Debug.Log($"TrolleyGazeTargetSetup: done — {added} added, {skipped} already present. " +
                      "Resize each BoxCollider to fit its object, then save the scene.");
        }
        static string GetPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }
}
