using System.IO;
using UnityEditor;
using UnityEngine;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Creates (or selects) the shared TrolleyTimingConfig asset in a Resources folder so every scene
    /// loads it by name with no per-scene wiring.
    ///
    ///   Trolley > Create or Select Timing Config
    ///
    /// Run once. Then change the decision window on the asset in the Inspector to retime all scenes at
    /// once — train speed and worker-hide delay follow automatically.
    /// </summary>
    public static class TrolleyTimingConfigSetup
    {
        const string Dir   = "Assets/Trolley/Resources";
        const string Path_ = "Assets/Trolley/Resources/TrolleyTimingConfig.asset";

        [MenuItem("Trolley/Create or Select Timing Config")]
        public static void CreateOrSelect()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TrolleyTimingConfig>(Path_);
            if (existing == null)
            {
                if (!Directory.Exists(Dir)) Directory.CreateDirectory(Dir);
                existing = ScriptableObject.CreateInstance<TrolleyTimingConfig>();
                AssetDatabase.CreateAsset(existing, Path_);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"Timing Config: created {Path_}. Set the decision window here to retime all scenes.");
            }
            else
            {
                Debug.Log($"Timing Config: already exists at {Path_} — selecting it.");
            }
            Selection.activeObject = existing;
            EditorGUIUtility.PingObject(existing);
        }
    }
}
