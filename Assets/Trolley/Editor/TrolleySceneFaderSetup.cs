using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Adds a SceneFader GameObject to the open scene. SceneFader is scene-local (no
    /// DontDestroyOnLoad) — each scene that wants mid-scene or end-of-scene fades needs its
    /// own instance. Safe to run multiple times — skips if one is already present.
    /// Does NOT save; review and save the scene yourself.
    ///
    ///   Trolley > Add Scene Fader (open scene)
    /// </summary>
    public static class TrolleySceneFaderSetup
    {
        [MenuItem("Trolley/Add Scene Fader (open scene)")]
        public static void AddSceneFader()
        {
            var existing = Object.FindFirstObjectByType<SceneFader>();
            if (existing != null)
            {
                Debug.Log($"TrolleySceneFaderSetup: SceneFader already present ({existing.gameObject.name}) — skipped.", existing.gameObject);
                return;
            }

            var go = new GameObject("SceneFader");
            Undo.RegisterCreatedObjectUndo(go, "Add Scene Fader");
            go.AddComponent<SceneFader>();

            EditorSceneManager.MarkSceneDirty(go.scene);
            Debug.Log("TrolleySceneFaderSetup: added SceneFader. Save the scene.", go);
        }
    }
}
