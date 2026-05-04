using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Run once via menu: Trolley > Wire Optional Scene
    /// Same as Driver but the action path leads to a wall (hasWallCollision = true).
    /// The train hits the wall if the participant presses the button.
    /// </summary>
    public static class TrolleyOptionalSetup
    {
        const string ScenePath = "Assets/Trolley/Scenes/TrolleyOptional.unity";
        const string WorkerFbxPath = "Assets/Trolley/Animations/Ch17_nonPBR.fbx";
        const string WorkerControllerPath = "Assets/Trolley/Animations/WorkerController.controller";
        const string TrainPrefabPath = "Assets/Polyeler/Simple Train Pack/Prefabs/Train/Train_Type B.prefab";

        [MenuItem("Trolley/Wire Optional Scene")]
        public static void WireOptionalScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            foreach (string name in new[] {
                "TrolleyController", "NarrationPlayer", "TimerCanvas",
                "Train_TypeB", "Train_TypeB [PLACEHOLDER — assign real prefab]",
                "TrainPaths", "InactionTrackWorkers", "ActionTrackWorkers",
                "Button", "Wall", "WallCollisionEffect" })
            {
                var existing = GameObject.Find(name);
                if (existing != null) Object.DestroyImmediate(existing);
            }

            // ── TrolleyController ─────────────────────────────────────────────
            var controllerGO = new GameObject("TrolleyController");
            var controller = controllerGO.AddComponent<TrolleyController>();
            controller.scenarioID = "optional";

            // ── NarrationPlayer ───────────────────────────────────────────────
            var narrationGO = new GameObject("NarrationPlayer");
            var audioSrc = narrationGO.AddComponent<AudioSource>();
            audioSrc.playOnAwake = false;
            var narrationPlayer = narrationGO.AddComponent<NarrationPlayer>();
            SetField(narrationPlayer, "audioSource", audioSrc);

            // ── Timer Canvas (World Space) ─────────────────────────────────────
            var canvasGO = new GameObject("TimerCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<TrackedDeviceGraphicRaycaster>();
            canvasGO.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 150f);
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

            // ── Train ─────────────────────────────────────────────────────────
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
                Debug.LogWarning("WireOptionalScene: Train_Type B prefab not found — created placeholder cube.");
            }
            trainGO.transform.position = new Vector3(0f, 0f, -15f);
            var trainController = trainGO.AddComponent<TrainController>();

            // ── Train waypoints ────────────────────────────────────────────────
            // Action path ends at z=30 where the wall sits.
            var pathsGO = new GameObject("TrainPaths");

            var approachPathGO = new GameObject("ApproachPath");
            approachPathGO.transform.SetParent(pathsGO.transform);
            var approachWPs = CreateWaypoints(approachPathGO,
                new Vector3(0f, 0f, -8f),
                new Vector3(0f, 0f, -4f),
                new Vector3(0f, 0f,  0f));

            var inactionPathGO = new GameObject("InactionPath");
            inactionPathGO.transform.SetParent(pathsGO.transform);
            var inactionWPs = CreateWaypoints(inactionPathGO,
                new Vector3(0f, 0f, 5f),
                new Vector3(0f, 0f, 20f),
                new Vector3(0f, 0f, 40f));

            var actionPathGO = new GameObject("ActionPath");
            actionPathGO.transform.SetParent(pathsGO.transform);
            var actionWPs = CreateWaypoints(actionPathGO,
                new Vector3(1f, 0f, 5f),
                new Vector3(4f, 0f, 15f),
                new Vector3(4f, 0f, 30f));  // stops at wall

            // ── Workers (only on inaction track — action track has a wall) ────
            var workerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorkerFbxPath);
            var workerController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(WorkerControllerPath);

            var inactionWorkers = SpawnWorkers("InactionTrackWorkers", workerPrefab, workerController,
                center: new Vector3(0f, 0f, 22f), count: 2, spacing: 1.2f);

            // Action track has no workers (train hits wall instead).
            var actionWorkers = new Animator[0];

            // ── Wall at end of action path ────────────────────────────────────
            var wallGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallGO.name = "Wall";
            wallGO.transform.position = new Vector3(4f, 1f, 31f);
            wallGO.transform.localScale = new Vector3(4f, 3f, 0.3f);

            // ── Collision effect (starts inactive, activated on impact) ────────
            var effectGO = new GameObject("WallCollisionEffect");
            effectGO.transform.position = new Vector3(4f, 1f, 30.5f);
            var ps = effectGO.AddComponent<ParticleSystem>();
            // Simple burst: 50 particles, short lifetime
            var main = ps.main;
            main.startLifetime = 0.8f;
            main.startSpeed = 5f;
            main.startSize = 0.2f;
            main.startColor = new Color(1f, 0.4f, 0f);
            main.loop = false;
            main.playOnAwake = false;
            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 50) });
            effectGO.SetActive(false);

            // Collision audio on the train (TrainController expects AudioSource on same or child GO)
            var collisionAudioSrc = trainGO.AddComponent<AudioSource>();
            collisionAudioSrc.playOnAwake = false;

            // Wire TrainController (with wall collision enabled)
            var tcSO = new SerializedObject(trainController);
            tcSO.FindProperty("train").objectReferenceValue = trainGO.transform;
            SetTransformArray(tcSO, "approachPath", approachWPs);
            SetTransformArray(tcSO, "inactionPath", inactionWPs);
            SetTransformArray(tcSO, "actionPath", actionWPs);
            SetAnimatorArray(tcSO, "inactionTrackWorkers", inactionWorkers);
            SetAnimatorArray(tcSO, "actionTrackWorkers", actionWorkers);
            tcSO.FindProperty("hasWallCollision").boolValue = true;
            tcSO.FindProperty("wallCollisionEffect").objectReferenceValue = effectGO;
            tcSO.FindProperty("collisionAudio").objectReferenceValue = collisionAudioSrc;
            tcSO.ApplyModifiedProperties();

            // ── Button ────────────────────────────────────────────────────────
            var buttonGO = new GameObject("Button");
            buttonGO.transform.position = new Vector3(0f, 1.0f, 0.6f);

            var buttonMeshGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            buttonMeshGO.name = "ButtonMesh";
            buttonMeshGO.transform.SetParent(buttonGO.transform, false);
            buttonMeshGO.transform.localScale = new Vector3(0.12f, 0.04f, 0.12f);
            buttonMeshGO.transform.localPosition = new Vector3(0f, 0.04f, 0f);

            buttonGO.AddComponent<XRSimpleInteractable>();
            var trolleyButton = buttonGO.AddComponent<TrolleyButton>();
            SetField(trolleyButton, "buttonMesh", buttonMeshGO.transform);

            // ── Wire TrolleyController ─────────────────────────────────────────
            var cSO = new SerializedObject(controller);
            cSO.FindProperty("narrationPlayer").objectReferenceValue = narrationPlayer;
            cSO.FindProperty("decisionTimer").objectReferenceValue = decisionTimer;
            cSO.FindProperty("trainController").objectReferenceValue = trainController;
            cSO.FindProperty("interactable").objectReferenceValue = trolleyButton;
            cSO.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("TrolleyOptionalSetup: TrolleyOptional scene wired and saved.");
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

        static Animator[] SpawnWorkers(string groupName, GameObject prefab,
            RuntimeAnimatorController animController, Vector3 center, int count, float spacing)
        {
            var group = new GameObject(groupName);
            var animators = new Animator[count];
            for (int i = 0; i < count; i++)
            {
                GameObject w;
                if (prefab != null)
                    w = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                else
                {
                    w = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    Debug.LogWarning($"WireOptionalScene: worker prefab not found — created placeholder for {groupName}.");
                }
                w.name = $"Worker_{i + 1}";
                w.transform.SetParent(group.transform);
                float offset = (i - (count - 1) * 0.5f) * spacing;
                w.transform.position = center + new Vector3(offset, 0f, 0f);
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
