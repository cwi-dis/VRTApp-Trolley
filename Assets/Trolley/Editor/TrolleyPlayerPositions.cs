using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Run once via menu: Trolley > Set Player Positions
    /// Sets Player 1 and Player 2 spawn positions in Tutorial, Bystander, and Questionnaire scenes.
    /// Finds them at Tool_SceneSetup/Player Initial Locations/Player 1 (Player 2).
    /// </summary>
    public static class TrolleyPlayerPositions
    {
        [MenuItem("Trolley/Set Player Positions")]
        public static void SetAllPlayerPositions()
        {
            SetPositionsInScene(
                "Assets/Trolley/Scenes/TrolleyTutorial.unity",
                // In front of the researcher canvas (canvas is at z=2 facing toward z=0)
                p1Pos: new Vector3( 0.0f, 0f, -0.5f), p1Rot: Quaternion.Euler(0, 0, 0),
                p2Pos: new Vector3( 0.6f, 0f, -0.5f), p2Rot: Quaternion.Euler(0, 0, 0));

            SetPositionsInScene(
                "Assets/Trolley/Scenes/TrolleyBystander.unity",
                // Standing beside the track fork near the lever (-1.5, 0.9, -0.5)
                // Both see the fork and the approaching train (train starts at z=-15)
                p1Pos: new Vector3(-1.2f, 0f, -2.0f), p1Rot: Quaternion.Euler(0, 0, 0),
                p2Pos: new Vector3( 0.2f, 0f, -2.0f), p2Rot: Quaternion.Euler(0, 0, 0));

            SetPositionsInScene(
                "Assets/Trolley/Scenes/TrolleyQuestionnaire.unity",
                // Booth A (master) — canvas at (0, 1.6, 2) facing toward z=0
                p1Pos: new Vector3(0f, 0f, -0.5f),  p1Rot: Quaternion.Euler(0, 0, 0),
                // Booth B (non-master) — canvas at (0, 1.6, -28) facing toward z=-30
                p2Pos: new Vector3(0f, 0f, -30.5f), p2Rot: Quaternion.Euler(0, 0, 0));

            Debug.Log("TrolleyPlayerPositions: positions set in Tutorial, Bystander, Questionnaire.");
        }

        static void SetPositionsInScene(string scenePath,
            Vector3 p1Pos, Quaternion p1Rot,
            Vector3 p2Pos, Quaternion p2Rot)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            SetPlayer("Tool_SceneSetup/Player Initial Locations/Player 1", p1Pos, p1Rot);
            SetPlayer("Tool_SceneSetup/Player Initial Locations/Player 2", p2Pos, p2Rot);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"TrolleyPlayerPositions: saved {scenePath}");
        }

        static void SetPlayer(string path, Vector3 pos, Quaternion rot)
        {
            var go = GameObject.Find(path);
            if (go == null)
            {
                Debug.LogWarning($"TrolleyPlayerPositions: '{path}' not found in scene.");
                return;
            }
            Undo.RecordObject(go.transform, "Set Player Position");
            go.transform.position = pos;
            go.transform.rotation = rot;
        }
    }
}
