using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Run once via menu: Trolley > Setup Scenes.
    /// Duplicates VRTEmpty into all five trolley scenes and adds them to Build Settings.
    /// </summary>
    public static class TrolleySceneSetup
    {
        const string SourceScene =
            "Assets/Samples/VR2Gather/1.3.992/VRT Essential Assets/Scenes/Empty/VRTEmpty.unity";

        static readonly string[] SceneNames =
        {
            "TrolleyTutorial",
            "TrolleyBystander",
            "TrolleyDriver",
            "TrolleySelfharm",
            "TrolleyQuestionnaire",
        };

        const string DestinationFolder = "Assets/Trolley/Scenes";

        [MenuItem("Trolley/Setup Scenes")]
        public static void CreateScenes()
        {
            if (!File.Exists(SourceScene))
            {
                Debug.LogError($"TrolleySceneSetup: source scene not found at {SourceScene}");
                return;
            }

            if (!AssetDatabase.IsValidFolder(DestinationFolder))
                AssetDatabase.CreateFolder("Assets/Trolley", "Scenes");

            var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);

            foreach (string name in SceneNames)
            {
                string dest = $"{DestinationFolder}/{name}.unity";
                if (File.Exists(dest))
                {
                    Debug.Log($"TrolleySceneSetup: {name} already exists, skipping.");
                }
                else
                {
                    AssetDatabase.CopyAsset(SourceScene, dest);
                    Debug.Log($"TrolleySceneSetup: created {dest}");
                }

                // Add to Build Settings if not already there.
                bool alreadyAdded = false;
                foreach (var s in buildScenes)
                    if (s.path == dest) { alreadyAdded = true; break; }

                if (!alreadyAdded)
                    buildScenes.Add(new EditorBuildSettingsScene(dest, true));
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();
            AssetDatabase.Refresh();
            Debug.Log("TrolleySceneSetup: all scenes created and added to Build Settings.");
        }
    }
}
