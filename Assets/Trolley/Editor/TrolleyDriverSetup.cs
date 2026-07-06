using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.Splines;
using TMPro;
using VRT.Pilots.Common;


namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Targeted, non-destructive menu items for the Driver scene.
    /// Full WireDriverScene was removed to prevent accidental scene destruction.
    /// </summary>
    public static class TrolleyDriverSetup
    {
        const string ScenePath = "Assets/Trolley/Scenes/TrolleyDriver.unity";

        // WireDriverScene removed — use targeted menu items only:
        //   Trolley > Driver – Wire Movement
        //   Trolley > Driver – Wire Toggle Buttons


        /// <summary>
        /// Non-destructive: puts a DriverTrainController on TrackEnvironment and wires it.
        /// The Driver scene uses the environment-movement approach — the whole environment
        /// slides toward the stationary player, then yaws about the player's seat
        /// (the DivertMarker) on the action outcome. No spline is needed.
        /// </summary>
        [MenuItem("Trolley/Driver – Wire Movement")]
        public static void WireDriverMovement()
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.path != ScenePath)
            {
                Debug.LogWarning("Driver – Wire Movement: open TrolleyDriver scene first.");
                return;
            }

            var trackEnvGO = GameObject.Find("TrackEnvironment");
            if (trackEnvGO == null)
            {
                Debug.LogError("Driver – Wire Movement: TrackEnvironment not found in scene.");
                return;
            }

            // Swap the Bystander-style TrainController (if present) for the Driver one.
            var oldTC = trackEnvGO.GetComponent<TrainController>();
            if (oldTC != null) Object.DestroyImmediate(oldTC);

            var driver = trackEnvGO.GetComponent<DriverTrainController>();
            if (driver == null) driver = trackEnvGO.AddComponent<DriverTrainController>();

            var dSO = new SerializedObject(driver);
            // Divert point is ~95 units ahead; at speed 11 it reaches the player ~8.6s in —
            // just after the 8s decision window, so the swing lands as you roll onto the switch.
            // Switch is ~81.5 units ahead; speed 9.5 → reaches the player ~8.6s in, just after the 8s window.
            dSO.FindProperty("approachSpeed").floatValue    = 9.5f;
            // Branch is a 90° quarter-circle: tangents at (0,0,81.52) and (79.5,0,160.54), chord ≈ 112 → radius ≈ 79.3.
            // Turn rate = speed / radius, so the tilt matches travelling the arc. Flip sign to change direction.
            dSO.FindProperty("branchTurnAngle").floatValue  = -95f;  // tuned by hand
            dSO.FindProperty("branchRadius").floatValue     = 79.3f;

            var marker = GameObject.Find("DivertMarker");
            if (marker != null)
                dSO.FindProperty("divertPivot").objectReferenceValue = marker.transform;
            else
                Debug.LogWarning("Driver – Wire Movement: DivertMarker not found — " +
                                 "divert will pivot about world origin. Create/assign it for an accurate turn.");

            // Hit-worker groups — hidden on impact so they don't clip into the cab.
            var actionWorkers   = GameObject.Find("ActionTrackWorkers");
            var inactionWorkers = GameObject.Find("InactionTrackWorkers");
            if (actionWorkers != null)
                dSO.FindProperty("actionHitWorkers").objectReferenceValue = actionWorkers;
            if (inactionWorkers != null)
                dSO.FindProperty("inactionHitWorkers").objectReferenceValue = inactionWorkers;
            dSO.FindProperty("hitDelay").floatValue = 4f;    // fade-to-black / impact-effect timing (workers hide by distance, not this)
            dSO.FindProperty("hideRadius").floatValue = 5f;  // hide each worker group when its meshes reach ~5 units from the seat (tuned)
            dSO.ApplyModifiedProperties();

            // Wire to TrolleyController.
            var controller = Object.FindFirstObjectByType<TrolleyController>();
            if (controller != null)
            {
                var cSO = new SerializedObject(controller);
                cSO.FindProperty("trainController").objectReferenceValue = driver;
                cSO.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarning("Driver – Wire Movement: TrolleyController not found.");
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("Driver – Wire Movement: done.\n" +
                      "DriverTrainController on TrackEnvironment, wired to TrolleyController.\n" +
                      "approachSpeed=9.5, branchTurnAngle=-95, branchRadius=79.3, divertPivot=" +
                      (marker != null ? "DivertMarker" : "world origin") + ".\n" +
                      "If the divert turns the wrong way, flip the sign of Branch Turn Angle in the Inspector.");
        }

        [MenuItem("Trolley/Driver – Wire Toggle Buttons")]
        public static void WireToggleButtons()
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.path != ScenePath)
            {
                Debug.LogWarning("Driver – Wire Toggle Buttons: open TrolleyDriver scene first.");
                return;
            }

            var buttonA = GameObject.Find("Button_TrackA");
            var buttonB = GameObject.Find("Button_TrackB");
            if (buttonA == null || buttonB == null)
            {
                Debug.LogError("Button_TrackA or Button_TrackB not found in scene.");
                return;
            }

            // Create or replace ToggleDecision
            var existing = GameObject.Find("ToggleDecision");
            if (existing != null) Object.DestroyImmediate(existing);
            var toggleGO = new GameObject("ToggleDecision");
            var toggle = toggleGO.AddComponent<TrolleyToggleDecision>();

            // Wire buttonA and buttonB — Awake() will auto-find renderer and XRSimpleInteractable
            var tSO = new SerializedObject(toggle);
            tSO.FindProperty("buttonA").objectReferenceValue = buttonA;
            tSO.FindProperty("buttonB").objectReferenceValue = buttonB;
            tSO.ApplyModifiedProperties();

            // Wire to TrolleyController
            var controller = Object.FindFirstObjectByType<TrolleyController>();
            if (controller == null) { Debug.LogWarning("Driver – Wire Toggle Buttons: TrolleyController not found."); return; }
            var cSO = new SerializedObject(controller);
            cSO.FindProperty("toggleDecision").objectReferenceValue = toggle;
            cSO.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("Driver – Wire Toggle Buttons: done. A=inaction (green default), B=action (grey).\nNow wire Button_TrackA OnTrigger → PressA() and Button_TrackB OnTrigger → PressB() on the ToggleDecision object.");
        }


    }
}
