using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Copies the player spawn markers from the (correct) Driver scene into the Self-harm and Driver-
    /// tutorial scenes, so all three start the player(s) in the same seat.
    ///
    ///   Trolley > Copy Player Locations: Driver → Selfharm + TutorialDriver
    ///
    /// Source path (Driver): Tool_scenesetup_Trolley/Player Initial Locations/Player1 (and Player2).
    /// Those markers live inside the Tool_scenesetup_Trolley prefab instance, so this works on the loaded
    /// hierarchy (prefab children are reachable by name) rather than touching scene/prefab YAML by hand.
    ///
    /// Copies WORLD position + WORLD rotation (lands exactly where Driver has them regardless of any
    /// parent differences) and LOCAL scale. Reads from Driver, writes to both targets, saves both.
    /// Prompts to save the currently open scene first. Re-runnable; undoable per-object.
    /// </summary>
    public static class TrolleyPlayerLocationCopy
    {
        const string DriverScene   = "Assets/Trolley/Scenes/TrolleyDriver.unity";
        const string SelfharmScene = "Assets/Trolley/Scenes/TrolleySelfharm.unity";
        const string TutorialScene = "Assets/Trolley/Scenes/TrolleyTutorialDriver.unity";

        const string LocationsParent = "Player Initial Locations";
        static readonly string[] PlayerNames = { "Player1", "Player2" };

        struct Placement { public Vector3 worldPos; public Quaternion worldRot; public Vector3 localScale; }

        [MenuItem("Trolley/Copy Player Locations: Driver → Selfharm + TutorialDriver")]
        public static void CopyPlayerLocations()
        {
            // Don't lose any unsaved work in the currently open scene.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // ── Read the markers from the Driver scene ──
            var captured = new Dictionary<string, Placement>();
            var src = EditorSceneManager.OpenScene(DriverScene, OpenSceneMode.Single);
            if (!src.IsValid()) { Debug.LogError($"Copy Player Locations: could not open {DriverScene}."); return; }
            foreach (var n in PlayerNames)
            {
                var go = FindPlayerMarker(src, n);
                if (go == null) { Debug.LogWarning($"Copy Player Locations: '{LocationsParent}/{n}' not found in Driver — skipped."); continue; }
                var t = go.transform;
                captured[n] = new Placement { worldPos = t.position, worldRot = t.rotation, localScale = t.localScale };
            }
            if (captured.Count == 0) { Debug.LogError("Copy Player Locations: no player markers found in Driver — nothing to copy."); return; }

            // ── Apply them to each target scene ──
            ApplyTo(SelfharmScene, captured);
            ApplyTo(TutorialScene, captured);
        }

        static void ApplyTo(string scenePath, Dictionary<string, Placement> captured)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) { Debug.LogError($"Copy Player Locations: could not open {scenePath}."); return; }

            int applied = 0;
            var missing = new List<string>();
            foreach (var n in PlayerNames)
            {
                if (!captured.TryGetValue(n, out var p)) continue;
                var go = FindPlayerMarker(scene, n);
                if (go == null) { missing.Add(n); continue; }
                Undo.RecordObject(go.transform, "Copy Player Locations");
                go.transform.position   = p.worldPos;
                go.transform.rotation   = p.worldRot;
                go.transform.localScale = p.localScale;
                applied++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            var msg = $"Copy Player Locations: applied {applied}/{captured.Count} markers to {Path(scenePath)} and saved.";
            if (missing.Count > 0)
                msg += $"\nNot found there: {string.Join(", ", missing)}.";
            Debug.Log(msg);
        }

        // Find the marker named e.g. "Player1" that sits under a "Player Initial Locations" parent, so we
        // don't accidentally grab an unrelated object that happens to share the name.
        static GameObject FindPlayerMarker(Scene scene, string playerName)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == playerName && t.parent != null && t.parent.name == LocationsParent)
                        return t.gameObject;
            return null;
        }

        static string Path(string assetPath) => System.IO.Path.GetFileNameWithoutExtension(assetPath);
    }
}
