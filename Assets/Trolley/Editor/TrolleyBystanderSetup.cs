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
    /// Run once via menu: Trolley > Wire Bystander Scene
    /// Populates TrolleyBystander with all required GameObjects, components, and references.
    /// </summary>
    public static class TrolleyBystanderSetup
    {
        const string ScenePath = "Assets/Trolley/Scenes/TrolleyBystander.unity";
        const string WorkerFbxPath = "Assets/Trolley/Animations/Ch17_nonPBR.fbx";
        const string WorkerControllerPath = "Assets/Trolley/Animations/WorkerController.controller";
        const string TrainPrefabPath = "Assets/Polyeler/Simple Train Pack/Prefabs/Train/Train_Type B.prefab";

        [MenuItem("Trolley/Wire Bystander Scene")]
        public static void WireBystanderScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // Remove previously generated objects so re-running is safe.
            foreach (string name in new[] {
                "TrolleyController", "NarrationPlayer", "TimerCanvas",
                "Train_TypeB", "Train_TypeB [PLACEHOLDER — assign real prefab]",
                "TrainPaths", "InactionTrackWorkers", "ActionTrackWorkers", "Lever" })
            {
                var existing = GameObject.Find(name);
                if (existing != null) Object.DestroyImmediate(existing);
            }

            // ── TrolleyController ─────────────────────────────────────────────
            var controllerGO = new GameObject("TrolleyController");
            var controller = controllerGO.AddComponent<TrolleyController>();
            controller.scenarioID = "bystander";

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
                Debug.LogWarning("WireBystanderScene: Train_Type B prefab not found — created placeholder cube.");
            }
            trainGO.transform.position = new Vector3(0f, 0f, -250f);
            var trainController = trainGO.AddComponent<TrainController>();

            // ── Train waypoints ────────────────────────────────────────────────
            // Train starts at z=-250. Approach path leads it to the fork at z=0.
            // approachDuration=38s matches narration length — speed auto-calculated.
            // After the decision, it branches to action (right) or inaction (straight).
            var pathsGO = new GameObject("TrainPaths");

            var approachPathGO = new GameObject("ApproachPath");
            approachPathGO.transform.SetParent(pathsGO.transform);
            var approachWPs = CreateWaypoints(approachPathGO,
                new Vector3(0f, 0f, -150f),
                new Vector3(0f, 0f, -50f),
                new Vector3(0f, 0f,   0f));

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
                new Vector3(4f, 0f, 35f));

            // ── Workers ───────────────────────────────────────────────────────
            var workerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorkerFbxPath);
            var workerController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(WorkerControllerPath);

            var inactionWorkers = SpawnWorkers("InactionTrackWorkers", workerPrefab, workerController,
                center: new Vector3(0f, 0f, 22f), count: 2, spacing: 1.2f);
            var actionWorkers = SpawnWorkers("ActionTrackWorkers", workerPrefab, workerController,
                center: new Vector3(4f, 0f, 17f), count: 2, spacing: 1.2f);

            // Wire TrainController via SerializedObject
            var tcSO = new SerializedObject(trainController);
            tcSO.FindProperty("train").objectReferenceValue = trainGO.transform;
            tcSO.FindProperty("approachDuration").floatValue = 38f;
            SetTransformArray(tcSO, "approachPath", approachWPs);
            SetTransformArray(tcSO, "inactionPath", inactionWPs);
            SetTransformArray(tcSO, "actionPath", actionWPs);
            SetAnimatorArray(tcSO, "inactionTrackWorkers", inactionWorkers);
            SetAnimatorArray(tcSO, "actionTrackWorkers", actionWorkers);
            tcSO.ApplyModifiedProperties();

            // ── Lever ─────────────────────────────────────────────────────────
            var leverGO = new GameObject("Lever");
            leverGO.transform.position = new Vector3(-1.5f, 0.9f, -0.5f);

            var pivotGO = new GameObject("LeverPivot");
            pivotGO.transform.SetParent(leverGO.transform, false);

            var meshGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            meshGO.name = "LeverMesh";
            meshGO.transform.SetParent(pivotGO.transform, false);
            meshGO.transform.localScale = new Vector3(0.07f, 0.45f, 0.07f);
            meshGO.transform.localPosition = new Vector3(0f, 0.225f, 0f);
            Object.DestroyImmediate(meshGO.GetComponent<BoxCollider>());
            ColorMesh(meshGO, new Color(0.85f, 0.15f, 0.1f));

            var grab = leverGO.AddComponent<XRGrabInteractable>();
            var lever = leverGO.AddComponent<TrolleyLever>();
            SetField(lever, "leverPivot", pivotGO.transform);

            // ── Wire TrolleyController ─────────────────────────────────────────
            var cSO = new SerializedObject(controller);
            cSO.FindProperty("narrationPlayer").objectReferenceValue = narrationPlayer;
            cSO.FindProperty("decisionTimer").objectReferenceValue = decisionTimer;
            cSO.FindProperty("trainController").objectReferenceValue = trainController;
            cSO.FindProperty("interactable").objectReferenceValue = lever;
            cSO.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("TrolleyBystanderSetup: TrolleyBystander scene wired and saved.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

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
                    Debug.LogWarning($"WireBystanderScene: worker prefab not found — created placeholder capsule for {groupName}.");
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

        static void ColorMesh(GameObject go, Color color)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            var mat = Object.Instantiate(r.sharedMaterial);
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            r.sharedMaterial = mat;
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
