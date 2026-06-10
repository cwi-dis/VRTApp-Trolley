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
        //   Trolley > Driver – Wire Rail
        //   Trolley > Driver – Wire Toggle Buttons


        /// <summary>
        /// Non-destructive: creates Rail SplineContainer with 2 default splines and wires it
        /// to TrainController. Also sets modelForwardYaw=180 so TrackEnvironment never rotates.
        /// After running, open the Rail in the Spline editor and adjust knots to match your
        /// track geometry. Spline 0 = straight (inaction), Spline 1 = action branch.
        /// </summary>
        [MenuItem("Trolley/Driver – Wire Rail")]
        public static void WireRail()
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.path != ScenePath)
            {
                Debug.LogWarning("Driver – Wire Rail: open TrolleyDriver scene first.");
                return;
            }

            // Find or create Rail SplineContainer at scene root
            var railGO = GameObject.Find("Rail");
            SplineContainer railContainer;
            if (railGO == null)
            {
                railGO = new GameObject("Rail");
                railGO.AddComponent<ManagedBySetupScript>().menuItem = "Trolley/Driver – Wire Rail";
                railContainer = railGO.AddComponent<SplineContainer>();
                Debug.Log("Driver – Wire Rail: created new Rail SplineContainer.");
            }
            else
            {
                railContainer = railGO.GetComponent<SplineContainer>();
                if (railContainer == null) railContainer = railGO.AddComponent<SplineContainer>();
            }

            // Populate 2 splines with default knots for the environment-movement approach.
            // TrackEnvironment starts at world z=60 and moves toward/past the player at z=0.
            // Spline 0 (inaction): straight ahead — 5 workers on center track approach player.
            // Spline 1 (action): diverges right by x=3 — 1 worker on side track is in the path.
            // ADJUST knots in the Spline editor to match actual track geometry.
            while (railContainer.Splines.Count < 2)
                railContainer.AddSpline();

            // Only seed default knots if the spline has none — never overwrite hand-drawn knots.
            var spline0 = railContainer.Splines[0];
            if (spline0.Count == 0)
            {
                spline0.Add(new BezierKnot(new float3(0f, 0f, 60f)));
                spline0.Add(new BezierKnot(new float3(0f, 0f,  0f)));
                spline0.Add(new BezierKnot(new float3(0f, 0f, -20f)));
                Debug.Log("Driver – Wire Rail: seeded default knots on spline 0 (straight). Adjust in Spline editor.");
            }

            var spline1 = railContainer.Splines[1];
            if (spline1.Count == 0)
            {
                spline1.Add(new BezierKnot(new float3(0f,  0f, 60f)));
                spline1.Add(new BezierKnot(new float3(1.5f, 0f, 20f)));
                spline1.Add(new BezierKnot(new float3(3f,  0f, -20f)));
                Debug.Log("Driver – Wire Rail: seeded default knots on spline 1 (action branch). Adjust in Spline editor.");
            }

            EditorUtility.SetDirty(railContainer);

            // Wire Rail and train to TrainController; set modelForwardYaw=180 so
            // TrackEnvironment (the "train") never rotates as it moves along the spline.
            var trackEnvGO = GameObject.Find("TrackEnvironment");
            if (trackEnvGO == null)
            {
                Debug.LogError("Driver – Wire Rail: TrackEnvironment not found in scene.");
                return;
            }

            var trainController = trackEnvGO.GetComponent<TrainController>();
            if (trainController == null)
            {
                Debug.LogError("Driver – Wire Rail: TrainController not found on TrackEnvironment.");
                return;
            }

            var tcSO = new SerializedObject(trainController);
            var railProp = tcSO.FindProperty("rail");
            if (railProp != null) railProp.objectReferenceValue = railContainer;
            var trainProp = tcSO.FindProperty("train");
            if (trainProp != null && trainProp.objectReferenceValue == null)
                trainProp.objectReferenceValue = trackEnvGO.transform;
            var yawProp = tcSO.FindProperty("modelForwardYaw");
            if (yawProp != null) yawProp.floatValue = 180f;
            var speedProp = tcSO.FindProperty("trainSpeed");
            if (speedProp != null && speedProp.floatValue < 0.1f) speedProp.floatValue = 5f;
            tcSO.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("Driver – Wire Rail: done.\n" +
                      "Rail wired to TrainController. modelForwardYaw=180 (no rotation on straight splines).\n" +
                      "NEXT: select Rail in Hierarchy → open Spline editor → adjust knot positions to match track.\n" +
                      "Spline 0 = straight/inaction, Spline 1 = action branch.");
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
            var controller = Object.FindObjectOfType<TrolleyController>();
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
