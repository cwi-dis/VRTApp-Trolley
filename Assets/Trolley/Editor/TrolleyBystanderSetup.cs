using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using TMPro;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Run once via menu: Trolley > Wire Bystander Scene
    /// Control room design: participant stands at a console with a lever, watches four CCTV monitors
    /// showing the track from different angles (west view, switch point, track-1 east, track-2 east).
    /// Train + workers live at x=1000 (off-camera from player).
    /// Four RenderTexture cameras at the track feed the four monitor quads in the control room.
    /// </summary>
    public static class TrolleyBystanderSetup
    {
        const string ScenePath             = "Assets/Trolley/Scenes/TrolleyBystander.unity";
        const string WorkerFbxPath         = "Assets/Trolley/Animations/Ch17_nonPBR.fbx";
        const string WorkerControllerPath  = "Assets/Trolley/Animations/WorkerController.controller";
        const string TrainPrefabPath       = "Assets/Polyeler/Simple Train Pack/Prefabs/Train/Train_Type B.prefab";

        static readonly string[] RTPaths = {
            "Assets/Trolley/Textures/CCTV_RT_WestView.renderTexture",
            "Assets/Trolley/Textures/CCTV_RT_SwitchPoint.renderTexture",
            "Assets/Trolley/Textures/CCTV_RT_Track1East.renderTexture",
            "Assets/Trolley/Textures/CCTV_RT_Track2East.renderTexture",
        };
        static readonly string[] MatPaths = {
            "Assets/Trolley/Materials/M_Monitor_WestView.mat",
            "Assets/Trolley/Materials/M_Monitor_SwitchPoint.mat",
            "Assets/Trolley/Materials/M_Monitor_Track1East.mat",
            "Assets/Trolley/Materials/M_Monitor_Track2East.mat",
        };

        // All track objects are offset by this so the player can't see them directly
        static readonly Vector3 TrackOffset = new Vector3(1000f, 0f, 0f);

        // ── Monitor grid layout (2×2) ──────────────────────────────────────
        // Matches the monitor the user manually placed: 3m × 1.6875m at (0.023, 2.16, 4.15).
        // Split into 4 equal sub-monitors with a 0.04m gap. Rotation stays identity.
        const float FullW = 3.0f;
        const float FullH = 1.6875f;
        const float Gap   = 0.04f;
        const float SubW  = (FullW - Gap) * 0.5f;   // 1.48 m
        const float SubH  = (FullH - Gap) * 0.5f;   // 0.824 m

        // Local offsets from MonitorGroup centre
        static readonly Vector2[] GridOffsets = {
            new Vector2(-(SubW + Gap) * 0.5f,  (SubH + Gap) * 0.5f),  // TL  West View
            new Vector2( (SubW + Gap) * 0.5f,  (SubH + Gap) * 0.5f),  // TR  Switch Point
            new Vector2(-(SubW + Gap) * 0.5f, -(SubH + Gap) * 0.5f),  // BL  Track 1 East
            new Vector2( (SubW + Gap) * 0.5f, -(SubH + Gap) * 0.5f),  // BR  Track 2 East
        };
        static readonly string[] MonitorLabels = {
            "Monitor_WestView",
            "Monitor_SwitchPoint",
            "Monitor_Track1East",
            "Monitor_Track2East",
        };

        // Matches the user's manually placed single Monitor position
        static readonly Vector3 MonitorGroupCenter = new Vector3(0.023f, 2.16f, 4.15f);

        // ── CCTV camera local positions relative to TrackEnvironment ──────
        // Track layout: approach z=-15→0, fork at z≈0, inaction track z=0→40 (x=0),
        // action track x=1→4, z=5→35. Inaction workers at (0,0,22). Action worker at (4,0,17).
        static readonly Vector3[] CamLocalPos = {
            new Vector3(-5f,  3f, 20f),   // West View  — left of inaction track, watching workers
            new Vector3( 2f, 12f,  1f),   // Switch Pt  — overhead above fork
            new Vector3( 5f,  4f, 30f),   // Track1East — east of workers, looking back
            new Vector3(10f,  4f, 20f),   // Track2East — east of action worker
        };
        static readonly Quaternion[] CamRot = {
            Quaternion.Euler(25f,  70f,   0f),   // West View
            Quaternion.Euler(75f, -15f,   0f),   // Switch Point
            Quaternion.Euler(25f, -145f,  0f),   // Track 1 East
            Quaternion.Euler(30f, -120f,  0f),   // Track 2 East
        };
        static readonly string[] CamNames = {
            "CCTVCam_WestView",
            "CCTVCam_SwitchPoint",
            "CCTVCam_Track1East",
            "CCTVCam_Track2East",
        };

        [MenuItem("Trolley/Wire Bystander Scene")]
        public static void WireBystanderScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            foreach (string name in new[] {
                "TrolleyController", "NarrationPlayer", "TimerCanvas",
                "Train_TypeB", "Train_TypeB [PLACEHOLDER — assign real prefab]",
                "TrainPaths", "InactionTrackWorkers", "ActionTrackWorkers",
                "Lever", "Button", "TrackEnvironment",
                "CCTVCamera", "Monitor",       // old single-camera names
                "MonitorGroup",                // new group name
                "SceneDirectionalLight" })
            {
                var existing = GameObject.Find(name);
                if (existing != null) Object.DestroyImmediate(existing);
            }

            const string menuItem = "Trolley/Wire Bystander Scene";

            // ── TrolleyController ─────────────────────────────────────────────
            var controllerGO = new GameObject("TrolleyController");
            controllerGO.AddComponent<ManagedBySetupScript>().menuItem = menuItem;
            var controller = controllerGO.AddComponent<TrolleyController>();
            controller.scenarioID = "bystander";

            // ── NarrationPlayer ───────────────────────────────────────────────
            var narrationGO = new GameObject("NarrationPlayer");
            narrationGO.AddComponent<ManagedBySetupScript>().menuItem = menuItem;
            var audioSrc = narrationGO.AddComponent<AudioSource>();
            audioSrc.playOnAwake = false;
            var narrationPlayer = narrationGO.AddComponent<NarrationPlayer>();
            SetField(narrationPlayer, "audioSource", audioSrc);

            // ── Timer Canvas (World Space, above monitors) ────────────────────
            var canvasGO = new GameObject("TimerCanvas");
            canvasGO.AddComponent<ManagedBySetupScript>().menuItem = menuItem;
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            canvasGO.AddComponent<TrackedDeviceGraphicRaycaster>();
            var canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(400f, 150f);
            canvasGO.transform.position = new Vector3(0f, 2.5f, 1.0f);
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
            timerTMP.text = "8.0";
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

            // ── Track Environment (offset to x=1000, invisible to player) ─────
            var trackRoot = new GameObject("TrackEnvironment");
            trackRoot.AddComponent<ManagedBySetupScript>().menuItem = menuItem;
            trackRoot.transform.position = TrackOffset;

            var trainPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TrainPrefabPath);
            GameObject trainGO;
            if (trainPrefab != null)
            {
                trainGO = (GameObject)PrefabUtility.InstantiatePrefab(trainPrefab);
                trainGO.name = "Train_TypeB";
            }
            else
            {
                trainGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
                trainGO.name = "Train_TypeB [PLACEHOLDER — assign real prefab]";
                trainGO.transform.localScale = new Vector3(2f, 1.5f, 5f);
                Debug.LogWarning("WireBystanderScene: Train_Type B prefab not found — placeholder cube created.");
            }
            trainGO.transform.SetParent(trackRoot.transform);
            trainGO.transform.localPosition = new Vector3(0f, 0f, -15f);
            var trainController = trainGO.AddComponent<TrainController>();

            var pathsGO = new GameObject("TrainPaths");
            pathsGO.transform.SetParent(trackRoot.transform, false);

            var approachPathGO = new GameObject("ApproachPath");
            approachPathGO.transform.SetParent(pathsGO.transform, false);
            var approachWPs = CreateLocalWaypoints(approachPathGO,
                new Vector3(0f, 0f, -8f),
                new Vector3(0f, 0f, -4f),
                new Vector3(0f, 0f,  0f));

            var inactionPathGO = new GameObject("InactionPath");
            inactionPathGO.transform.SetParent(pathsGO.transform, false);
            var inactionWPs = CreateLocalWaypoints(inactionPathGO,
                new Vector3(0f, 0f,  5f),
                new Vector3(0f, 0f, 20f),
                new Vector3(0f, 0f, 40f));

            var actionPathGO = new GameObject("ActionPath");
            actionPathGO.transform.SetParent(pathsGO.transform, false);
            var actionWPs = CreateLocalWaypoints(actionPathGO,
                new Vector3(1f, 0f,  5f),
                new Vector3(4f, 0f, 15f),
                new Vector3(4f, 0f, 35f));

            var workerPrefab     = AssetDatabase.LoadAssetAtPath<GameObject>(WorkerFbxPath);
            var workerController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(WorkerControllerPath);

            var inactionWorkers = SpawnWorkers("InactionTrackWorkers", workerPrefab, workerController,
                parent: trackRoot.transform, localCenter: new Vector3(0f, 0f, 22f), count: 5, spacing: 1.2f);
            var actionWorkers = SpawnWorkers("ActionTrackWorkers", workerPrefab, workerController,
                parent: trackRoot.transform, localCenter: new Vector3(4f, 0f, 17f), count: 1, spacing: 1.2f);

            var tcSO = new SerializedObject(trainController);
            tcSO.FindProperty("train").objectReferenceValue = trainGO.transform;
            tcSO.FindProperty("approachDuration").floatValue = 38f;
            SetTransformArray(tcSO, "approachPath",    approachWPs);
            SetTransformArray(tcSO, "inactionPath",    inactionWPs);
            SetTransformArray(tcSO, "actionPath",      actionWPs);
            SetAnimatorArray(tcSO,  "inactionTrackWorkers", inactionWorkers);
            SetAnimatorArray(tcSO,  "actionTrackWorkers",   actionWorkers);
            tcSO.ApplyModifiedProperties();

            // ── Ensure Textures + Materials folders exist ──────────────────────
            if (!AssetDatabase.IsValidFolder("Assets/Trolley/Textures"))
                AssetDatabase.CreateFolder("Assets/Trolley", "Textures");
            if (!AssetDatabase.IsValidFolder("Assets/Trolley/Materials"))
                AssetDatabase.CreateFolder("Assets/Trolley", "Materials");

            // ── Four CCTV Cameras + RenderTextures ─────────────────────────────
            var cctvRoot = new GameObject("CCTVCameras");
            cctvRoot.transform.SetParent(trackRoot.transform, false);

            var rts = new RenderTexture[4];
            for (int i = 0; i < 4; i++)
            {
                rts[i] = AssetDatabase.LoadAssetAtPath<RenderTexture>(RTPaths[i]);
                if (rts[i] == null)
                {
                    rts[i] = new RenderTexture(1280, 720, 16, RenderTextureFormat.ARGB32);
                    rts[i].name = System.IO.Path.GetFileNameWithoutExtension(RTPaths[i]);
                    AssetDatabase.CreateAsset(rts[i], RTPaths[i]);
                }

                var camGO = new GameObject(CamNames[i]);
                camGO.transform.SetParent(cctvRoot.transform);
                camGO.transform.localPosition = CamLocalPos[i];
                camGO.transform.localRotation = CamRot[i];
                var cam = camGO.AddComponent<Camera>();
                cam.targetTexture = rts[i];
                cam.fieldOfView   = 60f;
                cam.farClipPlane  = 200f;
                cam.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
            }

            // ── Four Monitor Quads (2×2 grid in control room) ─────────────────
            var monitorGroup = new GameObject("MonitorGroup");
            monitorGroup.AddComponent<ManagedBySetupScript>().menuItem = menuItem;

            for (int i = 0; i < 4; i++)
            {
                var monMat = AssetDatabase.LoadAssetAtPath<Material>(MatPaths[i]);
                if (monMat == null)
                {
                    monMat = new Material(Shader.Find("Unlit/Texture"));
                    monMat.name = System.IO.Path.GetFileNameWithoutExtension(MatPaths[i]);
                    AssetDatabase.CreateAsset(monMat, MatPaths[i]);
                }
                monMat.mainTexture = rts[i];
                EditorUtility.SetDirty(monMat);

                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = MonitorLabels[i];
                quad.transform.SetParent(monitorGroup.transform);
                quad.transform.position = MonitorGroupCenter
                    + new Vector3(GridOffsets[i].x, GridOffsets[i].y, 0f);
                quad.transform.rotation = Quaternion.identity; // matches user's monitor placement
                quad.transform.localScale = new Vector3(SubW, SubH, 1f);
                quad.GetComponent<Renderer>().sharedMaterial = monMat;
                Object.DestroyImmediate(quad.GetComponent<MeshCollider>());
            }

            // ── Lever (on the control console, to the right of the monitors) ──
            var leverGO = new GameObject("Lever");
            leverGO.AddComponent<ManagedBySetupScript>().menuItem = menuItem;
            leverGO.transform.position = new Vector3(0.5f, 0.95f, 0.7f);

            var leverPivotGO = new GameObject("LeverPivot");
            leverPivotGO.transform.SetParent(leverGO.transform, false);

            var leverHandleGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leverHandleGO.name = "LeverHandle";
            leverHandleGO.transform.SetParent(leverPivotGO.transform, false);
            leverHandleGO.transform.localScale    = new Vector3(0.05f, 0.2f, 0.05f);
            leverHandleGO.transform.localPosition = new Vector3(0f, 0.2f, 0f);

            leverGO.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            var trolleyLever = leverGO.AddComponent<TrolleyLever>();
            var leverSO = new SerializedObject(trolleyLever);
            leverSO.FindProperty("leverPivot").objectReferenceValue = leverPivotGO.transform;
            leverSO.ApplyModifiedProperties();

            // ── Lighting ──────────────────────────────────────────────────────
            var lightGO = new GameObject("SceneDirectionalLight");
            lightGO.AddComponent<ManagedBySetupScript>().menuItem = menuItem;
            var light = lightGO.AddComponent<Light>();
            light.type      = LightType.Directional;
            light.intensity = 1.2f;
            light.color     = new Color(1f, 0.95f, 0.85f);
            lightGO.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.3f, 0.3f, 0.35f);

            // ── Wire TrolleyController ─────────────────────────────────────────
            var cSO = new SerializedObject(controller);
            cSO.FindProperty("narrationPlayer").objectReferenceValue  = narrationPlayer;
            cSO.FindProperty("decisionTimer").objectReferenceValue    = decisionTimer;
            cSO.FindProperty("trainController").objectReferenceValue  = trainController;
            cSO.FindProperty("interactable").objectReferenceValue     = trolleyLever;
            cSO.ApplyModifiedProperties();

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log(
                "TrolleyBystanderSetup: scene wired.\n" +
                "  4-monitor grid centred at (0, 1.55, 1.1) — place against your control room back wall.\n" +
                "  Lever at (0.5, 0.95, 0.7) — adjust to match console model.\n" +
                "  TrackEnvironment at x=1000 with 4 CCTV cameras."
            );
        }

        static Transform[] CreateLocalWaypoints(GameObject parent, params Vector3[] localPositions)
        {
            var wps = new Transform[localPositions.Length];
            for (int i = 0; i < localPositions.Length; i++)
            {
                var wp = new GameObject($"Waypoint{i + 1}");
                wp.transform.SetParent(parent.transform);
                wp.transform.localPosition = localPositions[i];
                wps[i] = wp.transform;
            }
            return wps;
        }

        static Animator[] SpawnWorkers(string groupName, GameObject prefab,
            RuntimeAnimatorController animController, Transform parent,
            Vector3 localCenter, int count, float spacing)
        {
            var group = new GameObject(groupName);
            group.transform.SetParent(parent, false);
            var animators = new Animator[count];
            for (int i = 0; i < count; i++)
            {
                GameObject w = prefab != null
                    ? (GameObject)PrefabUtility.InstantiatePrefab(prefab)
                    : GameObject.CreatePrimitive(PrimitiveType.Capsule);
                w.name = $"Worker_{i + 1}";
                w.transform.SetParent(group.transform);
                float offset = (i - (count - 1) * 0.5f) * spacing;
                w.transform.localPosition = localCenter + new Vector3(offset, 0f, 0f);
                var anim = w.GetComponentInChildren<Animator>(true);
                if (anim == null) anim = w.AddComponent<Animator>();
                if (animController != null) anim.runtimeAnimatorController = animController;
                animators[i] = anim;
            }
            return animators;
        }

        // ── Targeted fix: wire toggle decision buttons (A=inaction default, B=action) ──
        [MenuItem("Trolley/Bystander – Wire Toggle Buttons")]
        public static void WireToggleButtons()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            var scene = activeScene.path == ScenePath
                ? activeScene
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var buttonA = GameObject.Find("OBJ_NetworkButton_A");
            var buttonB = GameObject.Find("OBJ_NetworkButton_B");
            if (buttonA == null || buttonB == null)
            {
                Debug.LogError("OBJ_NetworkButton_A or OBJ_NetworkButton_B not found in scene.");
                return;
            }

            // Add XRSimpleInteractable to each button if missing
            foreach (var btn in new[] { buttonA, buttonB })
            {
                if (btn.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>(true) == null)
                    btn.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            }

            // Create or find TrolleyToggleDecision GameObject
            var existing = GameObject.Find("ToggleDecision");
            if (existing != null) Object.DestroyImmediate(existing);
            var toggleGO = new GameObject("ToggleDecision");
            var toggle = toggleGO.AddComponent<TrolleyToggleDecision>();

            var tSO = new SerializedObject(toggle);
            tSO.FindProperty("buttonA").objectReferenceValue = buttonA;
            tSO.FindProperty("buttonB").objectReferenceValue = buttonB;
            tSO.ApplyModifiedProperties();

            // Wire to TrolleyController — clear the lever interactable, set toggleDecision
            var controller = Object.FindObjectOfType<TrolleyController>();
            if (controller != null)
            {
                var cSO = new SerializedObject(controller);
                cSO.FindProperty("interactable").objectReferenceValue    = null;
                cSO.FindProperty("toggleDecision").objectReferenceValue  = toggle;
                cSO.ApplyModifiedProperties();
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Bystander: toggle buttons wired. A=inaction (green), B=action (grey). Swap materials in Inspector if preferred.");
        }

        // ── Targeted fix: add green rim quads around Track A and Track B monitors ──
        // Track A = Monitor_WestView (index 0, TL) — main track, 5 workers
        // Track B = Monitor_Track2East (index 3, BR) — side track, 1 worker
        [MenuItem("Trolley/Bystander – Add Monitor Rims")]
        public static void AddMonitorRims()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            var scene = activeScene.path == ScenePath
                ? activeScene
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var monitorGroup = GameObject.Find("MonitorGroup");
            if (monitorGroup == null) { Debug.LogError("MonitorGroup not found."); return; }

            const string rimMatPath = "Assets/Trolley/Materials/M_MonitorRim.mat";
            var rimMat = AssetDatabase.LoadAssetAtPath<Material>(rimMatPath);
            if (rimMat == null)
            {
                rimMat = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.2f, 0.75f, 0.2f) };
                rimMat.name = "M_MonitorRim";
                AssetDatabase.CreateAsset(rimMat, rimMatPath);
            }

            // Indices: 2 = Track1East (BL, Track A), 3 = Track2East (BR, Track B)
            int[] rimIndices = { 2, 3 };
            string[] rimNames = { "RimA", "RimB" };
            var rims = new GameObject[2];

            for (int r = 0; r < 2; r++)
            {
                int idx = rimIndices[r];
                if (idx >= monitorGroup.transform.childCount) continue;
                var monitor = monitorGroup.transform.GetChild(idx);

                var existing = monitor.Find(rimNames[r]);
                if (existing != null) Object.DestroyImmediate(existing.gameObject);

                var rim = GameObject.CreatePrimitive(PrimitiveType.Quad);
                rim.name = rimNames[r];
                rim.transform.SetParent(monitor, false);
                // Slightly larger than monitor, slightly behind (z+ = further from player)
                rim.transform.localPosition = new Vector3(0f, 0f, 0.01f);
                rim.transform.localScale    = new Vector3(1.06f, 1.06f, 1f);
                rim.GetComponent<Renderer>().sharedMaterial = rimMat;
                Object.DestroyImmediate(rim.GetComponent<MeshCollider>());
                rim.SetActive(false); // enabled at runtime by TrolleyToggleDecision
                rims[r] = rim;
            }

            // Wire to TrolleyToggleDecision if present
            var toggle = Object.FindObjectOfType<TrolleyToggleDecision>();
            if (toggle != null)
            {
                var tSO = new SerializedObject(toggle);
                if (rims[0] != null) tSO.FindProperty("rimA").objectReferenceValue = rims[0];
                if (rims[1] != null) tSO.FindProperty("rimB").objectReferenceValue = rims[1];
                tSO.ApplyModifiedProperties();
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Bystander: monitor rims added. RimA on Monitor_WestView, RimB on Monitor_Track2East.");
        }

        // ── Targeted fix: update TrainController to new startPoint/endPoint API ──
        // Run this instead of the full wire script — preserves manually placed geometry.
        [MenuItem("Trolley/Bystander – Fix Train Controller")]
        public static void FixTrainController()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            var scene = activeScene.path == ScenePath
                ? activeScene
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var trainController = Object.FindObjectOfType<TrainController>();
            if (trainController == null) { Debug.LogError("TrainController not found — run Wire Bystander Scene first."); return; }

            var trackEnv = GameObject.Find("TrackEnvironment");
            if (trackEnv == null) { Debug.LogError("TrackEnvironment not found."); return; }

            // Remove old waypoint path objects if present
            foreach (string n in new[] { "TrainPaths", "ApproachPath", "InactionPath", "ActionPath" })
            {
                var old = trackEnv.transform.Find(n)?.gameObject ?? GameObject.Find(n);
                if (old != null) Object.DestroyImmediate(old);
            }

            // Start: far back so train isn't visible at scene open
            // End: well past workers (workers are at local z=22)
            // Speed is auto-calculated: distance / (narrationDuration + 8s)
            // At ~21s narration + 8s window, train reaches workers (~z=22) ~3s into the window
            var startPt = trackEnv.transform.Find("StartPoint")?.gameObject ?? new GameObject("StartPoint");
            startPt.transform.SetParent(trackEnv.transform);
            startPt.transform.localPosition = new Vector3(0f, 0f, -302f);

            var endPt = trackEnv.transform.Find("EndPoint")?.gameObject ?? new GameObject("EndPoint");
            endPt.transform.SetParent(trackEnv.transform);
            endPt.transform.localPosition = new Vector3(0f, 0f, 80f);

            var tcSO = new SerializedObject(trainController);
            tcSO.FindProperty("train").objectReferenceValue = trainController.transform;
            tcSO.FindProperty("startPoint").objectReferenceValue = startPt.transform;
            tcSO.FindProperty("endPoint").objectReferenceValue = endPt.transform;
            tcSO.FindProperty("decisionWindowSeconds").floatValue = 8f;
            tcSO.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Bystander: TrainController wired — startPoint z=-302, endPoint z=80. Adjust endPoint z if timing feels off.");
        }

        // ── Targeted fix: add monitor labels matching the reference image ──────
        [MenuItem("Trolley/Bystander – Add Monitor Labels")]
        public static void AddMonitorLabels()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            var scene = activeScene.path == ScenePath
                ? activeScene
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var monitorGroup = GameObject.Find("MonitorGroup");
            if (monitorGroup == null) { Debug.LogError("MonitorGroup not found — run Wire Bystander Scene first."); return; }

            // Remove existing label group
            var existing = GameObject.Find("MonitorLabelGroup");
            if (existing != null) Object.DestroyImmediate(existing);

            string[] labelTexts = {
                "Track 1 – West View",
                "Tracks 1 & 2 – Switch Point",
                "Track 1 – East View",
                "Track 2 – East View",
            };

            // Place label canvases at same grid positions as monitors, slightly in front (z-)
            var labelGroup = new GameObject("MonitorLabelGroup");
            labelGroup.transform.position = MonitorGroupCenter + new Vector3(0f, 0f, -0.05f);

            for (int i = 0; i < 4; i++)
            {
                var canvasGO = new GameObject($"Label_{i}");
                canvasGO.transform.SetParent(labelGroup.transform);
                canvasGO.transform.localPosition = new Vector3(GridOffsets[i].x, GridOffsets[i].y, 0f);
                canvasGO.transform.localRotation = Quaternion.identity;

                var canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                // sizeDelta (200,200) × localScale 0.005 × parentScale (SubW,SubH) = (SubW, SubH) world
                var canvasRect = canvasGO.GetComponent<RectTransform>();
                canvasRect.sizeDelta = new Vector2(200f, 200f);
                canvasGO.transform.localScale = new Vector3(0.005f / SubW, 0.005f / SubH, 0.005f);

                // Red background panel — bottom-left anchor
                var panelGO = new GameObject("Panel");
                panelGO.transform.SetParent(canvasGO.transform, false);
                var img = panelGO.AddComponent<Image>();
                img.color = new Color(0.78f, 0.08f, 0.08f, 0.92f);
                var panelRect = panelGO.GetComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0f, 0f);
                panelRect.anchorMax = new Vector2(0f, 0f);
                panelRect.pivot     = new Vector2(0f, 0f);
                panelRect.anchoredPosition = new Vector2(6f, 5f);
                panelRect.sizeDelta = new Vector2(115f, 20f);

                // White bold TMP text
                var textGO = new GameObject("Text");
                textGO.transform.SetParent(panelGO.transform, false);
                var tmp = textGO.AddComponent<TextMeshProUGUI>();
                tmp.text      = labelTexts[i];
                tmp.fontSize  = 7f;
                tmp.color     = Color.white;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 4f;
                tmp.fontSizeMax = 7f;
                tmp.enableWordWrapping = false;
                var textRect = textGO.GetComponent<RectTransform>();
                textRect.anchorMin  = Vector2.zero;
                textRect.anchorMax  = Vector2.one;
                textRect.offsetMin  = new Vector2(4f, 2f);
                textRect.offsetMax  = new Vector2(-3f, -2f);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Bystander: monitor labels added. Adjust panel sizeDelta and font size in Inspector if needed.");
        }

        // ── Targeted fix: add CCTV blackout overlays and wire to TrolleyController ──
        [MenuItem("Trolley/Bystander – Add CCTV Blackout")]
        public static void AddCCTVBlackout()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            var scene = activeScene.path == ScenePath
                ? activeScene
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var monitorGroup = GameObject.Find("MonitorGroup");
            if (monitorGroup == null) { Debug.LogError("MonitorGroup not found."); return; }

            var controller = Object.FindObjectOfType<TrolleyController>();
            if (controller == null) { Debug.LogError("TrolleyController not found."); return; }

            // Black material
            const string blackMatPath = "Assets/Trolley/Materials/M_MonitorBlackout.mat";
            var blackMat = AssetDatabase.LoadAssetAtPath<Material>(blackMatPath);
            if (blackMat == null)
            {
                blackMat = new Material(Shader.Find("Unlit/Color")) { color = Color.black };
                blackMat.name = "M_MonitorBlackout";
                AssetDatabase.CreateAsset(blackMat, blackMatPath);
            }

            var overlays = new GameObject[4];
            int count = Mathf.Min(4, monitorGroup.transform.childCount);

            for (int i = 0; i < count; i++)
            {
                var monitor = monitorGroup.transform.GetChild(i);
                var existingOverlay = monitor.Find("BlackoutOverlay");
                if (existingOverlay != null) Object.DestroyImmediate(existingOverlay.gameObject);

                var overlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
                overlay.name = "BlackoutOverlay";
                overlay.transform.SetParent(monitor, false);
                overlay.transform.localPosition = new Vector3(0f, 0f, -0.01f);
                overlay.transform.localScale    = Vector3.one;
                overlay.GetComponent<Renderer>().sharedMaterial = blackMat;
                Object.DestroyImmediate(overlay.GetComponent<MeshCollider>());
                overlay.SetActive(false);
                overlays[i] = overlay;
            }

            // Add CCTVBlackout component on TrolleyController GameObject
            var existing = controller.GetComponent<CCTVBlackout>();
            if (existing != null) Object.DestroyImmediate(existing);
            var cctvBlackout = controller.gameObject.AddComponent<CCTVBlackout>();

            var bSO = new SerializedObject(cctvBlackout);
            var overlaysProp = bSO.FindProperty("monitorOverlays");
            overlaysProp.arraySize = count;
            for (int i = 0; i < count; i++)
                overlaysProp.GetArrayElementAtIndex(i).objectReferenceValue = overlays[i];
            bSO.ApplyModifiedProperties();

            // Wire CCTVBlackout to TrolleyController
            var cSO = new SerializedObject(controller);
            cSO.FindProperty("cctvBlackout").objectReferenceValue = cctvBlackout;
            cSO.ApplyModifiedProperties();

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Bystander: CCTV blackout wired — monitors go black on decision.");
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
