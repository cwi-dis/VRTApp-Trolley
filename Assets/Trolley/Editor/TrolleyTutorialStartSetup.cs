using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Adds a participant-facing "Start" button to the CURRENTLY OPEN tutorial scene and wires it into
    /// that scene's drill (TutorialTrainDrill or TutorialDriverDrill).
    ///
    ///   Trolley > Add Tutorial Start Button (open scene)
    ///
    /// Run once in each tutorial scene. It instantiates the Button_Skip prefab (recoloured GREEN via
    /// M_Button_Active so it reads as a participant control, unlike the grey skip button), adds a
    /// TutorialGate, repoints the button's OnTrigger event to TutorialGate.Press(), and assigns the gate
    /// to the drill's 'gate' field. With a Start button wired, the drill opens with a free button warm-up
    /// (the A/B buttons go live so the participant can try them) and waits for a Start press before
    /// beginning — the fix for "tutorials are too fast" / cold-start.
    ///
    /// Placement is manual: position it within the participant's reach in front of the seat. Re-running is
    /// safe — if it already exists, placement is preserved and only the wiring is verified.
    /// </summary>
    public static class TrolleyTutorialStartSetup
    {
        const string StartName     = "OBJ_TutorialStart";
        const string SkipPrefab    = "Assets/Trolley/Prefabs/Button_Skip.prefab";
        const string ActiveMatGuid = "048704c7724cb4395bb12436363dd36a"; // M_Button_Active (green)

        [MenuItem("Trolley/Add Tutorial Start Button (open scene)")]
        public static void AddStartButton()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.path.Contains("Tutorial"))
            {
                Debug.LogError("Add Start Button: open a TrolleyTutorial* scene first — this menu builds into the open scene.");
                return;
            }

            var go = GameObject.Find(StartName);
            // Adopt a button placed by the earlier "Continue" version of this menu, if present, so we don't
            // create a duplicate — keeps its placement, green colour, and existing wiring.
            if (go == null)
            {
                var legacy = GameObject.Find("OBJ_TutorialContinue");
                if (legacy != null) { legacy.name = StartName; go = legacy; }
            }
            if (go == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkipPrefab);
                if (prefab == null) { Debug.LogError($"Add Start Button: prefab not found at {SkipPrefab}."); return; }

                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                go.name = StartName;
                go.transform.position = new Vector3(0f, 1.2f, 0.6f); // in front of the seat — reposition to reach
                ApplyActiveMaterial(go);
            }

            var gate = go.GetComponent<TutorialGate>();
            if (gate == null) gate = go.AddComponent<TutorialGate>();

            bool wiredEvent = RewireOnTrigger(go, gate);
            bool wiredDrill = WireDrillGate(gate);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"Add Start Button: '{StartName}' ready in {scene.name} — OnTrigger→Press {(wiredEvent ? "✓" : "✗ (selectEntered fallback)")}, " +
                      $"drill.gate {(wiredDrill ? "✓" : "✗ — wire it manually on the drill")}.\n" +
                      "MANUAL: position it within the participant's reach in front of the seat. Run once per tutorial scene.");
        }

        static void ApplyActiveMaterial(GameObject go)
        {
            var path = AssetDatabase.GUIDToAssetPath(ActiveMatGuid);
            var mat = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { Debug.LogWarning("Add Start Button: M_Button_Active not found — button stays grey."); return; }
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = mat;
        }

        // Point the networked button's OnTrigger event at TutorialGate.Press() (same mechanism as the
        // A/B buttons and the skip button), via SerializedProperty so no compile-time package ref is needed.
        static bool RewireOnTrigger(GameObject go, TutorialGate gate)
        {
            foreach (var comp in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (comp == null) continue;
                var so = new SerializedObject(comp);
                var calls = so.FindProperty("OnTrigger.m_PersistentCalls.m_Calls");
                if (calls == null) continue;

                calls.arraySize = 1;
                var call = calls.GetArrayElementAtIndex(0);
                call.FindPropertyRelative("m_Target").objectReferenceValue = gate;
                call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = "VRT.Pilots.Trolley.TutorialGate, Assembly-CSharp";
                call.FindPropertyRelative("m_MethodName").stringValue = "Press";
                call.FindPropertyRelative("m_Mode").enumValueIndex = 1;      // PersistentListenerMode.Void
                call.FindPropertyRelative("m_CallState").enumValueIndex = 2; // UnityEventCallState.RuntimeOnly
                so.ApplyModifiedProperties();
                return true;
            }
            return false;
        }

        // Assign the gate to whichever drill is in the scene (bystander = TutorialTrainDrill,
        // driver = TutorialDriverDrill). Both expose a serialized 'gate' field.
        static bool WireDrillGate(TutorialGate gate)
        {
            foreach (var drill in new MonoBehaviour[] {
                         Object.FindFirstObjectByType<TutorialTrainDrill>(),
                         Object.FindFirstObjectByType<TutorialDriverDrill>() })
            {
                if (drill == null) continue;
                var so = new SerializedObject(drill);
                var prop = so.FindProperty("gate");
                if (prop == null) continue;
                prop.objectReferenceValue = gate;
                so.ApplyModifiedProperties();
                return true;
            }
            return false;
        }
    }
}
