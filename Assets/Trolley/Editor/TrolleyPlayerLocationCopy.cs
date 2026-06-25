using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Copies the seat markers AND the two track buttons from the (correct) Driver scene into the
    /// Self-harm and Driver-tutorial scenes, so all three start the player(s) in the same seat and put
    /// the A/B buttons in the same reachable spot.
    ///
    ///   Trolley > Copy Seat + Buttons: Driver → Selfharm + TutorialDriver
    ///
    /// Objects copied (by name, on the loaded hierarchy so prefab-instance children work):
    ///   • Player Initial Locations/Player1, Player2  (seat markers, matched under their parent)
    ///   • Button_TrackA, Button_TrackB               (matched by name anywhere)
    ///
    /// Copies WORLD position + WORLD rotation (lands exactly where Driver has them regardless of parent
    /// differences) and LOCAL scale. Transform only — button wiring (OnTrigger / ToggleDecision) is
    /// untouched. Reads from Driver, writes to both targets, saves both. Prompts to save the open scene
    /// first. Re-runnable; undoable per-object.
    /// </summary>
    public static class TrolleyPlayerLocationCopy
    {
        const string DriverScene   = "Assets/Trolley/Scenes/TrolleyDriver.unity";
        const string SelfharmScene = "Assets/Trolley/Scenes/TrolleySelfharm.unity";
        const string TutorialScene = "Assets/Trolley/Scenes/TrolleyTutorialDriver.unity";

        // name = object to copy; parent = required parent name to disambiguate (null = match by name anywhere).
        struct Target { public string name; public string parent; }
        static readonly Target[] Targets =
        {
            new Target { name = "Player1",       parent = "Player Initial Locations" },
            new Target { name = "Player2",       parent = "Player Initial Locations" },
            new Target { name = "Button_TrackA", parent = null },
            new Target { name = "Button_TrackB", parent = null },
        };

        struct Placement { public Vector3 worldPos; public Quaternion worldRot; public Vector3 localScale; }

        [MenuItem("Trolley/Copy Seat + Buttons: Driver → Selfharm + TutorialDriver")]
        public static void CopySeatAndButtons()
        {
            // Don't lose any unsaved work in the currently open scene.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // ── Read the markers/buttons from the Driver scene ──
            var captured = new Dictionary<string, Placement>();
            var src = EditorSceneManager.OpenScene(DriverScene, OpenSceneMode.Single);
            if (!src.IsValid()) { Debug.LogError($"Copy Seat + Buttons: could not open {DriverScene}."); return; }
            foreach (var tgt in Targets)
            {
                var go = Find(src, tgt);
                if (go == null) { Debug.LogWarning($"Copy Seat + Buttons: '{Label(tgt)}' not found in Driver — skipped."); continue; }
                var t = go.transform;
                captured[tgt.name] = new Placement { worldPos = t.position, worldRot = t.rotation, localScale = t.localScale };
            }
            if (captured.Count == 0) { Debug.LogError("Copy Seat + Buttons: nothing found in Driver — nothing to copy."); return; }

            // ── Apply them to each target scene ──
            ApplyTo(SelfharmScene, captured);
            ApplyTo(TutorialScene, captured);
        }

        static void ApplyTo(string scenePath, Dictionary<string, Placement> captured)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) { Debug.LogError($"Copy Seat + Buttons: could not open {scenePath}."); return; }

            int applied = 0;
            var missing = new List<string>();
            foreach (var tgt in Targets)
            {
                if (!captured.TryGetValue(tgt.name, out var p)) continue;
                var go = Find(scene, tgt);
                if (go == null) { missing.Add(tgt.name); continue; }
                Undo.RecordObject(go.transform, "Copy Seat + Buttons");
                go.transform.position   = p.worldPos;
                go.transform.rotation   = p.worldRot;
                go.transform.localScale = p.localScale;
                applied++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            var msg = $"Copy Seat + Buttons: applied {applied}/{captured.Count} to {Path(scenePath)} and saved.";
            if (missing.Count > 0)
                msg += $"\nNot found there: {string.Join(", ", missing)}.";
            Debug.Log(msg);
        }

        // Find by name. When a parent is given, require it (so we don't grab an unrelated same-named object);
        // otherwise take the first match anywhere in the scene.
        static GameObject Find(Scene scene, Target tgt)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == tgt.name &&
                        (tgt.parent == null || (t.parent != null && t.parent.name == tgt.parent)))
                        return t.gameObject;
            return null;
        }

        static string Label(Target t) => t.parent == null ? t.name : $"{t.parent}/{t.name}";
        static string Path(string assetPath) => System.IO.Path.GetFileNameWithoutExtension(assetPath);
    }
}
