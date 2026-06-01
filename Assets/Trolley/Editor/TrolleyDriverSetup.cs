using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.UI;
using TMPro;
using VRT.Pilots.Common;


namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Run once via menu: Trolley > Wire Driver Scene
    /// Environment-movement approach: TrackEnvironment moves toward the stationary player,
    /// simulating the view from inside a moving train cab.
    /// Workers are children of TrackEnvironment so they ride with it.
    /// StartPoint/EndPoint are root-level world-space objects — TrainController moves
    /// TrackEnvironment from start to end at auto-calculated speed.
    /// </summary>
    public static class TrolleyDriverSetup
    {
        const string ScenePath = "Assets/Trolley/Scenes/TrolleyDriver.unity";
        const string WorkerFbxPath = "Assets/Trolley/Animations/Ch17_nonPBR.fbx";
        const string WorkerControllerPath = "Assets/Trolley/Animations/WorkerController.controller";

        [MenuItem("Trolley/Wire Driver Scene")]
        public static void WireDriverScene()
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var scene = (activeScene.IsValid() && activeScene.path == ScenePath)
                ? activeScene
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            foreach (string name in new[] {
                "TrolleyController", "NarrationPlayer", "TimerCanvas",
                "TrackEndpoints", "Button", "SceneDirectionalLight",
                // legacy names from old setup runs
                "TrackPaths", "TrainPaths", "InactionTrackWorkers", "ActionTrackWorkers",
                "TransitionReadyTrigger", "TransitionBarrier", "TransitionProceedTrigger" })
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
            var narrationAudioSrc = narrationGO.AddComponent<AudioSource>();
            narrationAudioSrc.playOnAwake = false;
            narrationAudioSrc.loop = false;
            var narrationPlayer = narrationGO.AddComponent<NarrationPlayer>();
            SetField(narrationPlayer, "audioSource", narrationAudioSrc);

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

            // ── TrackEnvironment ──────────────────────────────────────────────
            // Reuse existing TrackEnvironment (manually placed track geometry preserved).
            var trackEnvGO = GameObject.Find("TrackEnvironment");
            if (trackEnvGO == null)
            {
                trackEnvGO = new GameObject("TrackEnvironment");
                trackEnvGO.transform.position = new Vector3(0f, 0f, 60f);
                Debug.LogWarning("WireDriverScene: no TrackEnvironment found — created empty one at (0,0,60). Add track geometry manually.");
            }
            if (trackEnvGO.GetComponent<ManagedBySetupScript>() == null)
                trackEnvGO.AddComponent<ManagedBySetupScript>().menuItem = menuItem;

            // Ambient AudioSource for train sound — separate from narration
            var ambientAudioSrc = trackEnvGO.GetComponent<AudioSource>();
            if (ambientAudioSrc == null) ambientAudioSrc = trackEnvGO.AddComponent<AudioSource>();
            ambientAudioSrc.playOnAwake = false;
            ambientAudioSrc.loop = true;

            var trainController = trackEnvGO.GetComponent<TrainController>();
            if (trainController == null) trainController = trackEnvGO.AddComponent<TrainController>();

            // Workers as local children of TrackEnvironment — ride with the environment
            foreach (string wg in new[] { "InactionTrackWorkers", "ActionTrackWorkers" })
            {
                var t = trackEnvGO.transform.Find(wg);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }

            var workerPrefab     = AssetDatabase.LoadAssetAtPath<GameObject>(WorkerFbxPath);
            var workerController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(WorkerControllerPath);

            SpawnWorkers("InactionTrackWorkers", workerPrefab, workerController,
                parent: trackEnvGO.transform,
                localCenter: new Vector3(0f, 0f, 15f), count: 5, spacing: 1.2f);

            SpawnWorkers("ActionTrackWorkers", workerPrefab, workerController,
                parent: trackEnvGO.transform,
                localCenter: new Vector3(3f, 0f, 10f), count: 1, spacing: 1.2f);

            // ── Start / End points (root-level, world-space) ──────────────────
            // TrackEnvironment starts at StartPoint, moves toward EndPoint.
            // EndPoint is far enough that the scene transitions before arrival.
            var endpointsGO = new GameObject("TrackEndpoints");
            endpointsGO.AddComponent<ManagedBySetupScript>().menuItem = menuItem;

            var startPointGO = new GameObject("StartPoint");
            startPointGO.transform.SetParent(endpointsGO.transform, false);
            startPointGO.transform.position = new Vector3(0f, 0f, 60f);

            var endPointGO = new GameObject("EndPoint");
            endPointGO.transform.SetParent(endpointsGO.transform, false);
            endPointGO.transform.position = new Vector3(0f, 0f, -60f);

            // Action end point: side track direction (x matches ActionTrackWorkers x=12.5)
            var actionEndPointGO = new GameObject("ActionEndPoint");
            actionEndPointGO.transform.SetParent(endpointsGO.transform, false);
            actionEndPointGO.transform.position = new Vector3(12.5f, 0f, -60f);

            // ── Lighting (created early so it survives any later exception) ────
            var lightGO = new GameObject("SceneDirectionalLight");
            lightGO.AddComponent<ManagedBySetupScript>().menuItem = menuItem;
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.95f, 0.85f);
            lightGO.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.3f, 0.3f, 0.35f);

            // ── TrainController wiring ────────────────────────────────────────
            var tcSO = new SerializedObject(trainController);
            SetSerializedProp(tcSO, "train",               trackEnvGO.transform);
            SetSerializedProp(tcSO, "startPoint",          startPointGO.transform);
            SetSerializedProp(tcSO, "endPoint",            endPointGO.transform);
            SetSerializedProp(tcSO, "actionEndPoint",      actionEndPointGO.transform);
            SetSerializedProp(tcSO, "ambientAudioSource",  ambientAudioSrc);
            tcSO.ApplyModifiedProperties();

            // ── Button ────────────────────────────────────────────────────────
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

            // ── Wire TrolleyController ─────────────────────────────────────────
            var cSO = new SerializedObject(controller);
            cSO.FindProperty("narrationPlayer").objectReferenceValue  = narrationPlayer;
            cSO.FindProperty("decisionTimer").objectReferenceValue    = decisionTimer;
            cSO.FindProperty("trainController").objectReferenceValue  = trainController;
            cSO.FindProperty("interactable").objectReferenceValue     = trolleyButton;
            TrolleySetupBarrierUtils.AddTransitionBarrier(cSO, menuItem);
            cSO.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("TrolleyDriverSetup: TrolleyDriver scene wired and saved.");
        }

        static void SpawnWorkers(string groupName, GameObject prefab,
            RuntimeAnimatorController animController, Transform parent,
            Vector3 localCenter, int count, float spacing)
        {
            var group = new GameObject(groupName);
            group.transform.SetParent(parent, false);
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
            }
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

            var buttonA = GameObject.Find("Button_TrackA Variant");
            var buttonB = GameObject.Find("Button_TrackB Variant");
            if (buttonA == null || buttonB == null)
            {
                Debug.LogError("Button_TrackA Variant or Button_TrackB Variant not found in scene.");
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

            // Wire to TrolleyController — clear single-trigger interactable
            var controller = Object.FindObjectOfType<TrolleyController>();
            if (controller == null) { Debug.LogWarning("Driver – Wire Toggle Buttons: TrolleyController not found."); return; }
            var cSO = new SerializedObject(controller);
            cSO.FindProperty("toggleDecision").objectReferenceValue = toggle;
            cSO.FindProperty("interactable").objectReferenceValue   = null;
            cSO.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("Driver – Wire Toggle Buttons: done. A=inaction (green default), B=action (grey).\nNow wire Button_TrackA OnTrigger → PressA() and Button_TrackB OnTrigger → PressB() on the ToggleDecision object.");
        }

        static void SetField(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(fieldName).objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }

        static void SetSerializedProp(SerializedObject so, string fieldName, Object value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop == null) { Debug.LogWarning($"WireDriverScene: field '{fieldName}' not found on {so.targetObject.GetType().Name}"); return; }
            prop.objectReferenceValue = value;
        }
    }
}
