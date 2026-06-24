using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Adds a researcher-only "skip tutorial" button to the CURRENTLY OPEN tutorial scene.
    ///
    ///   Trolley > Add Tutorial Skip Button (open scene)
    ///
    /// Run it once in TrolleyTutorialDriver and once in TrolleyTutorialBystander. It instantiates the
    /// Button_Skip prefab (a copy of the A/B networked button, so it looks/behaves like the real buttons),
    /// adds a TutorialSkipButton, and repoints the button's OnTrigger event from the default PressA to
    /// TutorialSkipButton.Skip() — the same wiring mechanism the A/B buttons use, so the press path is
    /// proven. Pressing it loads the next scene in the session flow.
    ///
    /// Placement is manual: the button spawns behind/left of the world origin and must be nudged in the
    /// Inspector to sit just behind (or left of) the participant's seat, out of their natural reach.
    ///
    /// Re-running is safe and repairs wiring: it adopts an existing OBJ_TutorialSkip OR a Button_Skip prefab
    /// that was dragged into the scene by hand (a dragged prefab has no TutorialSkipButton and its OnTrigger
    /// still points at the default PressA — i.e. it does nothing). Adopting renames + wires it in place.
    /// </summary>
    public static class TrolleySkipButtonSetup
    {
        const string SkipName   = "OBJ_TutorialSkip";
        const string SkipPrefab = "Assets/Trolley/Prefabs/Button_Skip.prefab";

        [MenuItem("Trolley/Add Tutorial Skip Button (open scene)")]
        public static void AddSkipButton()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.path.Contains("Tutorial"))
            {
                Debug.LogError("Add Skip Button: open a TrolleyTutorial* scene first — this menu builds into the open scene.");
                return;
            }

            // Reuse an existing skip button (one we made, or a Button_Skip prefab dragged in by hand) so we
            // repair its wiring in place rather than creating a duplicate. Otherwise instantiate a fresh one.
            var go = GameObject.Find(SkipName) ?? GameObject.Find("Button_Skip");
            if (go == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkipPrefab);
                if (prefab == null) { Debug.LogError($"Add Skip Button: prefab not found at {SkipPrefab}."); return; }
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                // Default: behind + left of the origin, roughly seat height. MUST be repositioned per scene.
                go.transform.position = new Vector3(-0.6f, 1.2f, -0.5f);
            }
            go.name = SkipName;

            var skip = go.GetComponent<TutorialSkipButton>();
            if (skip == null) skip = go.AddComponent<TutorialSkipButton>();

            bool wired = RewireOnTrigger(go, skip);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"Add Skip Button: '{SkipName}' ready in {scene.name} — TutorialSkipButton ✓, " +
                      $"OnTrigger→Skip {(wired ? "✓" : "✗ (selectEntered fallback)")}.\n" +
                      "MANUAL: reposition it behind / to the left of the participant's seat, out of reach.");
        }

        // Points the networked button's OnTrigger UnityEvent at TutorialSkipButton.Skip(), exactly the way
        // the A/B buttons wire OnTrigger → PressA on the toggle (the prefab default we're replacing here).
        // Done via SerializedProperty so we don't need a compile-time reference to the package's button type.
        static bool RewireOnTrigger(GameObject go, TutorialSkipButton skip)
        {
            foreach (var comp in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (comp == null) continue;
                var so = new SerializedObject(comp);
                var calls = so.FindProperty("OnTrigger.m_PersistentCalls.m_Calls");
                if (calls == null) continue;

                calls.arraySize = 1;
                var call = calls.GetArrayElementAtIndex(0);
                call.FindPropertyRelative("m_Target").objectReferenceValue = skip;
                call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = "VRT.Pilots.Trolley.TutorialSkipButton, Assembly-CSharp";
                call.FindPropertyRelative("m_MethodName").stringValue = "Skip";
                call.FindPropertyRelative("m_Mode").enumValueIndex = 1;      // PersistentListenerMode.Void
                call.FindPropertyRelative("m_CallState").enumValueIndex = 2; // UnityEventCallState.RuntimeOnly
                so.ApplyModifiedProperties();
                return true;
            }
            return false;
        }
    }
}
