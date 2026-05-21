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
