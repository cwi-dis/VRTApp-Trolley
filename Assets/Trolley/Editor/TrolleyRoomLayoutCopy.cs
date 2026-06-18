using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Copies the placement of the control-room layout objects from the Tutorial scene into the
    /// Bystander scene, so the two rooms match after the Bystander room was rescaled.
    ///
    ///   Trolley > Copy Room Layout: Tutorial → Bystander
    ///
    /// Copies WORLD position + WORLD rotation (so each object lands exactly where the tutorial has it,
    /// regardless of any parent-scale differences) and LOCAL scale. Objects handled:
    ///   MonitorGroup, MonitorLabelGroup, ControlRoomShell, Button_TrackA, Button_TrackB, GazeTarget_Buttons.
    ///
    /// Reads from Tutorial, writes to Bystander, and saves Bystander. It prompts to save the currently
    /// open scene first (so the Bystander room-scale change isn't lost). Re-runnable; undoable per-object.
    /// </summary>
    public static class TrolleyRoomLayoutCopy
    {
        const string TutorialScene  = "Assets/Trolley/Scenes/TrolleyTutorialBystander.unity";
        const string BystanderScene = "Assets/Trolley/Scenes/TrolleyBystander.unity";

        static readonly string[] Names =
        {
            "MonitorGroup", "MonitorLabelGroup", "ControlRoomShell",
            "Button_TrackA", "Button_TrackB", "GazeTarget_Buttons",
        };

        struct Placement { public Vector3 worldPos; public Quaternion worldRot; public Vector3 localScale; }

        [MenuItem("Trolley/Copy Room Layout: Tutorial → Bystander")]
        public static void CopyRoomLayout()
        {
            // Don't lose the just-made Bystander room-scale edit (or any other open scene).
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // ── Read placements from the Tutorial scene ──
            var captured = new Dictionary<string, Placement>();
            var tut = EditorSceneManager.OpenScene(TutorialScene, OpenSceneMode.Single);
            if (!tut.IsValid()) { Debug.LogError($"Copy Room Layout: could not open {TutorialScene}."); return; }
            foreach (var n in Names)
            {
                var go = FindInScene(tut, n);
                if (go == null) { Debug.LogWarning($"Copy Room Layout: '{n}' not found in Tutorial — skipped."); continue; }
                var t = go.transform;
                captured[n] = new Placement { worldPos = t.position, worldRot = t.rotation, localScale = t.localScale };
            }

            // ── Apply them in the Bystander scene ──
            var bys = EditorSceneManager.OpenScene(BystanderScene, OpenSceneMode.Single);
            if (!bys.IsValid()) { Debug.LogError($"Copy Room Layout: could not open {BystanderScene}."); return; }
            int applied = 0;
            var missing = new List<string>();
            foreach (var n in Names)
            {
                if (!captured.TryGetValue(n, out var p)) continue;
                var go = FindInScene(bys, n);
                if (go == null) { missing.Add(n); continue; }
                Undo.RecordObject(go.transform, "Copy Room Layout");
                go.transform.position   = p.worldPos;
                go.transform.rotation   = p.worldRot;
                go.transform.localScale = p.localScale;
                applied++;
            }

            EditorSceneManager.MarkSceneDirty(bys);
            EditorSceneManager.SaveScene(bys);

            var msg = $"Copy Room Layout: applied {applied}/{Names.Length} transforms from Tutorial to Bystander and saved.";
            if (missing.Count > 0)
                msg += $"\nNot found in Bystander (build/create them there first, then re-run): {string.Join(", ", missing)}.";
            Debug.Log(msg);
        }

        static GameObject FindInScene(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name) return t.gameObject;
            return null;
        }
    }
}
