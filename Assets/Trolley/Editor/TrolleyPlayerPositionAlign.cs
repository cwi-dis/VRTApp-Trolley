using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Aligns the two player spawn points (Tool_scenesetup_Trolley/Player Initial Locations/
    /// Player1+Player2) consistently across all scenario scenes:
    ///
    ///   Trolley > Align Player Positions (reference: TrolleyDriver)
    ///
    /// Reference: the CURRENT Player1-to-button-midpoint offset (Y and Z) in TrolleyDriver.
    /// In every scene, players are placed relative to that scene's own Button_TrackA/B midpoint:
    /// 1 m apart along X centered on the midpoint, and at the reference Y/Z offset from it.
    /// Scenes with identical button placement (all driver-type scenes) therefore end up with
    /// identical player positions; bystander scenes get the same relative geometry.
    /// Player rotations are left untouched. Undoable per scene before save; re-runnable.
    /// </summary>
    public static class TrolleyPlayerPositionAlign
    {
        const string Dir = "Assets/Trolley/Scenes/";
        const string ReferenceScene = "TrolleyDriver";
        const string FloorName = "Invisible floor for users";
        const float Separation = 1.0f;

        static readonly string[] Scenes =
        {
            "TrolleyDriver", "TrolleyTutorialDriver", "TrolleySelfharm",
            "TrolleyBystander", "TrolleyTutorialBystander",
        };

        [MenuItem("Trolley/Align Player Positions (reference: TrolleyDriver)")]
        public static void Align()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // ── measure the reference offset in TrolleyDriver ──
            var refScene = EditorSceneManager.OpenScene(Dir + ReferenceScene + ".unity", OpenSceneMode.Single);
            Vector3 refMid = ButtonMidpoint(refScene);
            var refP1 = FindPlayer(refScene, 1);
            if (float.IsNaN(refMid.x) || refP1 == null)
            {
                Debug.LogError($"AlignPlayers: Button_TrackA/B or Player1 missing in {ReferenceScene} — aborted.");
                return;
            }
            float offY = refP1.transform.position.y - refMid.y;
            float offZ = refP1.transform.position.z - refMid.z;
            // Floor height relative to the players, taken from the reference scene too.
            var refFloor = Find(refScene, FloorName);
            float floorOffY = refFloor != null ? refFloor.transform.position.y - refP1.transform.position.y : 0f;
            Debug.Log($"AlignPlayers: reference offsets from button midpoint (from {ReferenceScene}): dY={offY:F3}, dZ={offZ:F3}, floor dY={floorOffY:F3}");

            // ── apply per scene, relative to that scene's own buttons ──
            foreach (var name in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(Dir + name + ".unity", OpenSceneMode.Single);
                Vector3 mid = ButtonMidpoint(scene);
                var s1 = FindPlayer(scene, 1);
                var s2 = FindPlayer(scene, 2);
                if (float.IsNaN(mid.x) || s1 == null || s2 == null)
                {
                    Debug.LogError($"AlignPlayers: Button_TrackA/B or Player1/Player2 missing in {name} — skipped.");
                    continue;
                }
                var tgt1 = new Vector3(mid.x - Separation / 2f, mid.y + offY, mid.z + offZ);
                var tgt2 = new Vector3(mid.x + Separation / 2f, mid.y + offY, mid.z + offZ);
                Undo.RecordObjects(new Object[] { s1.transform, s2.transform }, "Align Player Positions");
                Debug.Log($"AlignPlayers {name}: P1 {s1.transform.position} -> {tgt1}, P2 {s2.transform.position} -> {tgt2}");
                // Move the invisible floor under the players FIRST: the players are children of
                // the floor object in Tool_scenesetup_Trolley, so moving it shifts them — setting
                // the players' world positions afterwards makes the final result correct.
                var floor = Find(scene, FloorName);
                if (floor != null)
                {
                    Undo.RecordObject(floor.transform, "Align Player Positions");
                    var fTgt = new Vector3(mid.x, tgt1.y + floorOffY, tgt1.z);
                    Debug.Log($"AlignPlayers {name}: floor {floor.transform.position} -> {fTgt}");
                    floor.transform.position = fTgt;
                }
                else Debug.LogWarning($"AlignPlayers {name}: '{FloorName}' not found — floor not adjusted.");
                s1.transform.position = tgt1;
                s2.transform.position = tgt2;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log("AlignPlayers: done.");
        }

        static Vector3 ButtonMidpoint(Scene scene)
        {
            var a = Find(scene, "Button_TrackA");
            var b = Find(scene, "Button_TrackB");
            if (a == null || b == null) return new Vector3(float.NaN, 0, 0);
            return (a.transform.position + b.transform.position) / 2f;
        }

        static GameObject FindPlayer(Scene scene, int n) =>
            Find(scene, $"Player{n}") ?? Find(scene, $"Player {n}");

        static GameObject Find(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name) return t.gameObject;
            return null;
        }
    }
}
