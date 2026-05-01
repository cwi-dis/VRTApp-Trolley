using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Run once via menu: Trolley > Wire Driver Scene
    /// Same layout as Bystander but uses TrolleyButton (XRSimpleInteractable) instead of a lever.
    /// </summary>
    public static class TrolleyDriverSetup
    {
        const string ScenePath = "Assets/Trolley/Scenes/TrolleyDriver.unity";
        const string WorkerFbxPath = "Assets/Trolley/Animations/Ch17_nonPBR.fbx";
        const string WorkerControllerPath = "Assets/Trolley/Animations/WorkerController.controller";
        const string TrainPrefabPath = "Assets/Polyeler/Simple Train Pack/Prefabs/Train/Train_Type B.prefab";

        [MenuItem("Trolley/Wire Driver Scene")]
        public static void WireDriverScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            foreach (string name in new[] {
                "TrolleyController", "NarrationPlayer", "TimerCanvas",
                "Train_TypeB", "Train_TypeB [PLACEHOLDER — assign real prefab]",
                "TrainPaths", "InactionTrackWorkers", "ActionTrackWorkers", "Button" })
            {
                var existing = GameObject.Find(name);
                if (existing != null) Object.DestroyImmediate(existing);
            }

            // ── TrolleyController ─────────────────────────────────────────────
            var controllerGO = new GameObject("TrolleyController");
            var controller = controllerGO.AddComponent<TrolleyController>();
            controller.scenarioID = "driver";

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
            canvasGO.AddComponent<GraphicRaycaster>();
            var canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(400f, 150f);
            canvasGO.transform.position = new Vector3(0f, 2.8f, 1.5f);
            canvasGO.transform.localScale = Vector3.one * 0.005f;

            var timerTextGO = new GameObject("TimerText");
            timerTextGO.transform.SetParent(canvasGO.transform, false);
            var timerTMP = timerTextGO.AddComponent<TextMeshProUGUI>();
            timerTMP.text = "5.0";
            timerTMP.fontSize = 120;
            timerTMP.alignment = TextAlignmentOptions.Center;
            timerTMP.color = Color.white;
            var timerRect = timerTextGO.GetComponent<RectTransform>();
            timerRect.anchorMin = Vector2.zero;
            timerRect.anchorMax = Vector2.one;
            timerRect.offsetMin = Vector2.zero;
            timerRect.offsetMax = Vector2.zero;

            var decisionTimer = canvasGO.AddComponent<DecisionTimer>();
            SetField(decisionTimer, "timerText", timerTMP);

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
                Debug.LogWarning("WireDriverScene: Train_Type B prefab not found — created placeholder cube.");
            }
            trainGO.transform.position = new Vector3(0f, 0f, -15f);
            var trainController = trainGO.AddComponent<TrainController>();

            // ── Train waypoints ────────────────────────────────────────────────
            var pathsGO = new GameObject("TrainPaths");

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

            var tcSO = new SerializedObject(trainController);
            tcSO.FindProperty("train").objectReferenceValue = trainGO.transform;
            SetTransformArray(tcSO, "inactionPath", inactionWPs);
            SetTransformArray(tcSO, "actionPath", actionWPs);
            SetAnimatorArray(tcSO, "inactionTrackWorkers", inactionWorkers);
            SetAnimatorArray(tcSO, "actionTrackWorkers", actionWorkers);
            tcSO.ApplyModifiedProperties();

            // ── Button ────────────────────────────────────────────────────────
            // Placed in front of the player, at arm height, as if on a dashboard.
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
                    Debug.LogWarning($"WireDriverScene: worker prefab not found — created placeholder for {groupName}.");
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
