using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Run once via menu: Trolley > Set Player Positions
    /// Copies Player 1 spawn position from the Tutorial scene to all scenario
    /// and setup scenes. Questionnaire keeps its own booth positions.
    /// Path: Tool_SceneSetup/Player Initial Locations/Player 1 (Player 2)
    /// </summary>
    public static class TrolleyPlayerPositions
    {
        // Tutorial Player 1 position — copied to all scenario scenes.
        static readonly Vector3    P1Pos = new Vector3(0.0f, 0f, -0.5f);
        static readonly Quaternion P1Rot = Quaternion.Euler(0, 0, 0);
        static readonly Vector3    P2Pos = new Vector3(0.6f, 0f, -0.5f);
        static readonly Quaternion P2Rot = Quaternion.Euler(0, 0, 0);

        static readonly string[] ScenarioScenes =
        {
            "Assets/Trolley/Scenes/TrolleyTutorial.unity",
            "Assets/Trolley/Scenes/TrolleyAvatarSetup.unity",
            "Assets/Trolley/Scenes/TrolleyBystander.unity",
            "Assets/Trolley/Scenes/TrolleyDriver.unity",
            "Assets/Trolley/Scenes/TrolleyOptional.unity",
        };

        [MenuItem("Trolley/Set Player Positions")]
        public static void SetAllPlayerPositions()
        {
            foreach (string scenePath in ScenarioScenes)
            {
                if (!System.IO.File.Exists(scenePath))
                {
                    Debug.LogWarning($"TrolleyPlayerPositions: skipping {scenePath} — file not found.");
                    continue;
                }
                SetPositionsInScene(scenePath, P1Pos, P1Rot, P2Pos, P2Rot);
            }

            // Questionnaire keeps its own booth positions.
            SetPositionsInScene(
                "Assets/Trolley/Scenes/TrolleyQuestionnaire.unity",
                p1Pos: new Vector3(0f, 0f, -0.5f),  p1Rot: Quaternion.Euler(0, 0, 0),
                p2Pos: new Vector3(0f, 0f, -30.5f), p2Rot: Quaternion.Euler(0, 0, 0));

            Debug.Log("TrolleyPlayerPositions: all scenes updated.");
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
                Debug.LogWarning($"TrolleyPlayerPositions: '{path}' not found — skipping.");
                return;
            }
            Undo.RecordObject(go.transform, "Set Player Position");
            go.transform.position = pos;
            go.transform.rotation = rot;
        }
    }
}
