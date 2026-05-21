using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.UI;
using TMPro;


namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Run once via menu: Trolley > Wire Driver Scene
    /// Environment-movement approach: TrackEnvironment moves toward the stationary player,
    /// simulating the view from inside a moving train cab.
    /// Workers are children of TrackEnvironment so they ride along until the fork.
    /// Waypoints are root-level (world-space fixed) so TrainController can steer
    /// the environment root toward them.
    /// </summary>
    public static class TrolleyDriverSetup
    {
        const string ScenePath = "Assets/Trolley/Scenes/TrolleyDriver.unity";
        const string WorkerFbxPath = "Assets/Trolley/Animations/Ch17_nonPBR.fbx";
        const string WorkerControllerPath = "Assets/Trolley/Animations/WorkerController.controller";

        [MenuItem("Trolley/Wire Driver Scene")]
        public static void WireDriverScene()
        {
            // If the Driver scene is already open and active, use it as-is so that
            // manually placed objects (e.g. StraightRail) are not lost by a reload.
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var scene = (activeScene.IsValid() && activeScene.path == ScenePath)
                ? activeScene
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            foreach (string name in new[] {
                "TrolleyController", "NarrationPlayer", "TimerCanvas",
                "TrackPaths", "Button", "SceneDirectionalLight",
                // legacy names from old setup runs
                "TrainPaths", "InactionTrackWorkers", "ActionTrackWorkers" })
            {
                var existing = GameObject.Find(name);
                if (existing != null) Object.DestroyImmediate(existing);
            }

            const string menuItem = "Trolley/Wire Driver Scene";

            // ── TrolleyController ─────────────────────────────────────────────
            var controllerGO = new GameObject("TrolleyController");
            controllerGO.AddComponent<ManagedBySetupScript>().menuItem = menuItem;
            var controller = controllerGO.AddComponent<TrolleyController>();
            controller.scenarioID = "driver";

            // ── NarrationPlayer ───────────────────────────────────────────────
            var narrationGO = new GameObject("NarrationPlayer");
            narrationGO.AddComponent<ManagedBySetupScript>().menuItem = menuItem;
            var audioSrc = narrationGO.AddComponent<AudioSource>();
            audioSrc.playOnAwake = false;
            var narrationPlayer = narrationGO.AddComponent<NarrationPlayer>();
            SetField(narrationPlayer, "audioSource", audioSrc);

            // ── Timer Canvas (World Space) ─────────────────────────────────────
            var canvasGO = new GameObject("TimerCanvas");
            canvasGO.AddComponent<ManagedBySetupScript>().menuItem = menuItem;
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            canvasGO.AddComponent<TrackedDeviceGraphicRaycaster>();
            var canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(400f, 150f);
            canvasGO.transform.position = new Vector3(0f, 2.8f, 1.5f);
            canvasGO.transform.localScale = Vector3.one * 0.005f;

            var statusTextGO = new GameObject("StatusText");
            statusTextGO.transform.SetParent(canvasGO.transform, false);
            var statusTMP = statusTextGO.AddComponent<TextMeshProUGUI>();
            statusTMP.text = "Narration playing…";
            statusTMP.fontSize = 40;
            statusTMP.alignment = TextAlignmentOptions.Center;
            statusTMP.color = Color.white;
            var statusRect = statusTextGO.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0f, 0.5f);
            statusRect.anchorMax = Vector2.one;
            statusRect.offsetMin = statusRect.offsetMax = Vector2.zero;

            var timerTextGO = new GameObject("TimerText");
            timerTextGO.transform.SetParent(canvasGO.transform, false);
            var timerTMP = timerTextGO.AddComponent<TextMeshProUGUI>();
            timerTMP.text = "5.0";
            timerTMP.fontSize = 120;
            timerTMP.alignment = TextAlignmentOptions.Center;
            timerTMP.color = Color.white;
            var timerRect = timerTextGO.GetComponent<RectTransform>();
            timerRect.anchorMin = Vector2.zero;
            timerRect.anchorMax = new Vector2(1f, 0.5f);
            timerRect.offsetMin = timerRect.offsetMax = Vector2.zero;

            var decisionTimer = canvasGO.AddComponent<DecisionTimer>();
            var dtSO = new SerializedObject(decisionTimer);
            dtSO.FindProperty("timerText").objectReferenceValue  = timerTMP;
            dtSO.FindProperty("statusText").objectReferenceValue = statusTMP;
            dtSO.ApplyModifiedProperties();

            // ── TrackEnvironment ──────────────────────────────────────────────
            // Reuse the existing TrackEnvironment (with manually placed track/rail
            // children) rather than destroying it. Just add TrainController on top.
            var trackEnvGO = GameObject.Find("TrackEnvironment");
            if (trackEnvGO == null)
            {
                trackEnvGO = new GameObject("TrackEnvironment");
                trackEnvGO.transform.position = new Vector3(0f, 0f, 60f);
                Debug.LogWarning("WireDriverScene: no TrackEnvironment found — created empty one at (0,0,60). Add track geometry manually.");
            }
            if (trackEnvGO.GetComponent<ManagedBySetupScript>() == null)
                trackEnvGO.AddComponent<ManagedBySetupScript>().menuItem = menuItem;
            var trainController = trackEnvGO.GetComponent<TrainController>()
                               ?? trackEnvGO.AddComponent<TrainController>();

            // Workers as local children — destroy old groups first so re-runs are idempotent
            foreach (string wg in new[] { "InactionTrackWorkers", "ActionTrackWorkers" })
            {
                var t = trackEnvGO.transform.Find(wg);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }

            var workerPrefab     = AssetDatabase.LoadAssetAtPath<GameObject>(WorkerFbxPath);
            var workerController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(WorkerControllerPath);

            var inactionWorkers = SpawnWorkers("InactionTrackWorkers", workerPrefab, workerController,
                parent: trackEnvGO.transform,
                localCenter: new Vector3(0f, 0f, 15f), count: 5, spacing: 1.2f);

            var actionWorkers = SpawnWorkers("ActionTrackWorkers", workerPrefab, workerController,
                parent: trackEnvGO.transform,
                localCenter: new Vector3(3f, 0f, 10f), count: 1, spacing: 1.2f);

            // ── Track Paths (root-level, world-space) ─────────────────────────
            // Waypoints are NOT children of TrackEnvironment — they stay fixed in
            // world space so TrainController can use them as absolute targets.
            var pathsGO = new GameObject("TrackPaths");
            pathsGO.AddComponent<ManagedBySetupScript>().menuItem = menuItem;

            var approachPathGO = new GameObject("ApproachPath");
            approachPathGO.transform.SetParent(pathsGO.transform, false);
            var approachWPs = CreateWaypoints(approachPathGO,
                new Vector3(0f, 0f, 60f),
                new Vector3(0f, 0f, 30f),
                new Vector3(0f, 0f,  0f));

            var inactionPathGO = new GameObject("InactionPath");
            inactionPathGO.transform.SetParent(pathsGO.transform, false);
            var inactionWPs = CreateWaypoints(inactionPathGO,
                new Vector3(0f, 0f, -20f),
                new Vector3(0f, 0f, -50f));

            var actionPathGO = new GameObject("ActionPath");
            actionPathGO.transform.SetParent(pathsGO.transform, false);
            var actionWPs = CreateWaypoints(actionPathGO,
                new Vector3(3f, 0f, -15f),
                new Vector3(6f, 0f, -40f));

            var tcSO = new SerializedObject(trainController);
            tcSO.FindProperty("train").objectReferenceValue = trackEnvGO.transform;
            tcSO.FindProperty("approachDuration").floatValue = 76f;
            SetTransformArray(tcSO, "approachPath",         approachWPs);
            SetTransformArray(tcSO, "inactionPath",         inactionWPs);
            SetTransformArray(tcSO, "actionPath",           actionWPs);
            SetAnimatorArray(tcSO,  "inactionTrackWorkers", inactionWorkers);
            SetAnimatorArray(tcSO,  "actionTrackWorkers",   actionWorkers);
            tcSO.ApplyModifiedProperties();

            // ── Button ────────────────────────────────────────────────────────
            // Placed in front of the player, at arm height, as if on a dashboard.
            var buttonGO = new GameObject("Button");
            buttonGO.AddComponent<ManagedBySetupScript>().menuItem = menuItem;
            buttonGO.transform.position = new Vector3(0f, 1.0f, 0.6f);

            var buttonMeshGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            buttonMeshGO.name = "ButtonMesh";
            buttonMeshGO.transform.SetParent(buttonGO.transform, false);
            buttonMeshGO.transform.localScale = new Vector3(0.12f, 0.04f, 0.12f);
            buttonMeshGO.transform.localPosition = new Vector3(0f, 0.04f, 0f);

            buttonGO.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            var trolleyButton = buttonGO.AddComponent<TrolleyButton>();
            SetField(trolleyButton, "buttonMesh", buttonMeshGO.transform);

            // ── Lighting ──────────────────────────────────────────────────────
            // Remove any existing directional light added by this script
            var existingLight = GameObject.Find("SceneDirectionalLight");
            if (existingLight != null) Object.DestroyImmediate(existingLight);

            var lightGO = new GameObject("SceneDirectionalLight");
            lightGO.AddComponent<ManagedBySetupScript>().menuItem = menuItem;
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.95f, 0.85f); // warm daylight
            lightGO.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            // Ambient light so shadows aren't pitch black
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.3f, 0.3f, 0.35f);

            // ── Wire TrolleyController ─────────────────────────────────────────
            var cSO = new SerializedObject(controller);
            cSO.FindProperty("narrationPlayer").objectReferenceValue  = narrationPlayer;
            cSO.FindProperty("decisionTimer").objectReferenceValue    = decisionTimer;
            cSO.FindProperty("trainController").objectReferenceValue  = trainController;
            cSO.FindProperty("interactable").objectReferenceValue     = trolleyButton;
            cSO.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("TrolleyDriverSetup: TrolleyDriver scene wired and saved.");
        }

        static Transform[] CreateWaypoints(GameObject parent, params Vector3[] positions)
        {
            var wps = new Transform[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                var wp = new GameObject($"Waypoint{i + 1}");
                wp.transform.SetParent(parent.transform);
                wp.transform.position = positions[i];
                wps[i] = wp.transform;
            }
            return wps;
        }

        // parentTransform: workers are direct children, positioned with localPosition
        static Animator[] SpawnWorkers(string groupName, GameObject prefab,
            RuntimeAnimatorController animController, Transform parent,
            Vector3 localCenter, int count, float spacing)
        {
            var group = new GameObject(groupName);
            group.transform.SetParent(parent, false);
            var animators = new Animator[count];
            for (int i = 0; i < count; i++)
            {
                GameObject w;
                if (prefab != null)
                    w = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                else
                {
                    w = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    Debug.LogWarning($"WireDriverScene: worker prefab not found — placeholder for {groupName}.");
                }
                w.name = $"Worker_{i + 1}";
                w.transform.SetParent(group.transform, false);
                float offset = (i - (count - 1) * 0.5f) * spacing;
                w.transform.localPosition = localCenter + new Vector3(offset, 0f, 0f);
                var anim = w.GetComponentInChildren<Animator>(true);
                if (anim == null) anim = w.AddComponent<Animator>();
                if (animController != null) anim.runtimeAnimatorController = animController;
                animators[i] = anim;
            }
            return animators;
        }

        static void SetField(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(fieldName).objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }

        static void SetTransformArray(SerializedObject so, string fieldName, Transform[] transforms)
        {
            var prop = so.FindProperty(fieldName);
            prop.arraySize = transforms.Length;
            for (int i = 0; i < transforms.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = transforms[i];
        }

        static void SetAnimatorArray(SerializedObject so, string fieldName, Animator[] animators)
        {
            var prop = so.FindProperty(fieldName);
            prop.arraySize = animators.Length;
            for (int i = 0; i < animators.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = animators[i];
        }
    }
}
