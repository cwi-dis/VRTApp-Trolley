using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Reproduces the Driver scene's player↔button geometry in the two Bystander scenes, so the reach
    /// from each seat to the (centred) track buttons is the same across all scenario types.
    ///
    ///   Trolley > Adapt Driver Layout: Driver → Bystander + TutorialBystander
    ///
    /// What it does, per Bystander scene:
    ///   • Keeps the BUTTONS where they are (they're modelled onto the CCTV console — the anchor).
    ///   • Moves Player1 and Player2 so they sit symmetrically around the button midpoint, with the exact
    ///     lateral + depth offsets the Driver scene uses (so the reach geometry matches Driver).
    ///   • PRESERVES Player1's current height (its Y is untouched); Player2 is levelled to the same Y.
    ///   • Leaves both players' ROTATION untouched (each scene keeps its own facing).
    ///
    /// Reads the Driver scene live (transforms are resolved by Unity, so the nested-prefab / fileID-remapping
    /// issues that block static scene-file edits don't apply here). Reads Driver, writes both Bystander
    /// scenes, saves them. Prompts to save the open scene first. Re-runnable; undoable per-object.
    ///
    /// ASSUMPTION: offsets are applied in WORLD axes, i.e. the Bystander console faces the same world
    /// direction as the Driver window. If a Bystander scene is rotated differently, the players will land
    /// on the wrong side — check in the Scene view after running and tell me; switching to a button-local
    /// frame is a small change.
    /// </summary>
    public static class TrolleyBystanderLayoutAdapt
    {
        const string DriverScene            = "Assets/Trolley/Scenes/TrolleyDriver.unity";
        const string BystanderScene         = "Assets/Trolley/Scenes/TrolleyBystander.unity";
        const string TutorialBystanderScene = "Assets/Trolley/Scenes/TrolleyTutorialBystander.unity";

        [MenuItem("Trolley/Adapt Driver Layout: Driver → Bystander + TutorialBystander")]
        public static void Adapt()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // ── Read the reference geometry from the Driver scene ──
            var src = EditorSceneManager.OpenScene(DriverScene, OpenSceneMode.Single);
            if (!src.IsValid()) { Debug.LogError($"Adapt Layout: could not open {DriverScene}."); return; }

            var p1   = FindPlayer(src, "Player1");
            var p2   = FindPlayer(src, "Player2");
            var bA   = FindByName(src, "Button_TrackA");
            var bB   = FindByName(src, "Button_TrackB");
            if (p1 == null || p2 == null || bA == null || bB == null)
            {
                Debug.LogError("Adapt Layout: Driver scene missing Player1/Player2/Button_TrackA/Button_TrackB — aborted.");
                return;
            }

            Vector3 buttonMid = (bA.transform.position + bB.transform.position) * 0.5f;
            Vector3 offP1 = p1.transform.position - buttonMid;   // world offset, button-midpoint → seat
            Vector3 offP2 = p2.transform.position - buttonMid;

            Debug.Log($"Adapt Layout: Driver reference — buttonMid={buttonMid}, " +
                      $"offset→P1={offP1}, offset→P2={offP2} (players {Vector3.Distance(p1.transform.position, p2.transform.position):F3} m apart).");

            // ── Apply to each Bystander scene ──
            ApplyTo(BystanderScene, offP1, offP2);
            ApplyTo(TutorialBystanderScene, offP1, offP2);

            EditorUtility.DisplayDialog("Adapt Driver Layout",
                "Players in Bystander + TutorialBystander repositioned to match Driver's seat↔button geometry.\n\n" +
                "• Buttons left in place (console anchor)\n" +
                "• Player1 height preserved; Player2 levelled to it\n" +
                "• Rotations untouched\n\n" +
                "Check the Scene view: if a scene faces a different world direction, the players will be on the " +
                "wrong side — say so and I'll switch to a button-local frame.", "OK");
        }

        static void ApplyTo(string scenePath, Vector3 offP1, Vector3 offP2)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) { Debug.LogError($"Adapt Layout: could not open {scenePath}."); return; }

            var p1 = FindPlayer(scene, "Player1");
            var p2 = FindPlayer(scene, "Player2");
            var bA = FindByName(scene, "Button_TrackA");
            var bB = FindByName(scene, "Button_TrackB");
            if (p1 == null || p2 == null || bA == null || bB == null)
            {
                Debug.LogWarning($"Adapt Layout: {Name(scenePath)} missing a player or button — skipped.");
                return;
            }

            Vector3 buttonMid = (bA.transform.position + bB.transform.position) * 0.5f;
            float keepY = p1.transform.position.y;            // preserve Player1's height (room-specific)

            Vector3 newP1 = buttonMid + offP1; newP1.y = keepY;
            Vector3 newP2 = buttonMid + offP2; newP2.y = keepY;   // level Player2 with Player1

            Undo.RecordObject(p1.transform, "Adapt Driver Layout");
            Undo.RecordObject(p2.transform, "Adapt Driver Layout");
            p1.transform.position = newP1;
            p2.transform.position = newP2;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Adapt Layout: {Name(scenePath)} — buttonMid={buttonMid}, " +
                      $"Player1→{newP1} (Y kept {keepY:F3}), Player2→{newP2}; saved.");
        }

        // Player1/Player2 sit under a "Player Initial Locations" parent — require it to avoid grabbing
        // an unrelated same-named object.
        static GameObject FindPlayer(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name && t.parent != null && t.parent.name == "Player Initial Locations")
                        return t.gameObject;
            return null;
        }

        static GameObject FindByName(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name) return t.gameObject;
            return null;
        }

        static string Name(string assetPath) => System.IO.Path.GetFileNameWithoutExtension(assetPath);
    }
}
