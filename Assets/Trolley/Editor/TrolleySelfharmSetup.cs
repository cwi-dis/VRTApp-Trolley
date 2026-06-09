using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.Splines;
using TMPro;
using VRT.Pilots.Common;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Run once via menu: Trolley > Wire Selfharm Scene
    /// Same mechanics as Driver. The inaction path leads to 5 workers;
    /// the action path leads to a cliff/rocky mountain (participant harms themselves).
    /// </summary>
    public static class TrolleySelfharmSetup
    {
        const string ScenePath = "Assets/Trolley/Scenes/TrolleySelfharm.unity";
        const string WorkerFbxPath = "Assets/Trolley/Animations/Ch17_nonPBR.fbx";
        const string WorkerControllerPath = "Assets/Trolley/Animations/WorkerController.controller";
        const string TrainPrefabPath = "Assets/Polyeler/Simple Train Pack/Prefabs/Train/Train_Type B.prefab";

        [MenuItem("Trolley/Wire Selfharm Scene")]
        public static void WireSelfharmScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            foreach (string name in new[] {
                "TrolleyController", "NarrationPlayer", "TimerCanvas",
                "Train_TypeB", "Train_TypeB [PLACEHOLDER — assign real prefab]",
                "Rail", "TrainPaths", "InactionTrackWorkers",
                "Cliff", "CliffCollisionEffect",
                "TransitionReadyTrigger", "TransitionBarrier", "TransitionProceedTrigger" })
            {
                var existing = GameObject.Find(name);
                if (existing != null) Object.DestroyImmediate(existing);
            }

            const string menuItem = "Trolley/Wire Selfharm Scene";

            // ── TrolleyController ─────────────────────────────────────────────
            var controllerGO = new GameObject("TrolleyController");
            controllerGO.AddComponent<ManagedBySetupScript>().menuItem = menuItem;
            var controller = controllerGO.AddComponent<TrolleyController>();
            controller.scenarioID = "selfharm";

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
                Debug.LogWarning("WireSelfharmScene: Train_Type B prefab not found — created placeholder cube.");
            }
            trainGO.transform.position = new Vector3(0f, 0f, -15f);
            trainGO.AddComponent<ManagedBySetupScript>().menuItem = menuItem;
            var trainController = trainGO.AddComponent<TrainController>();

            // ── Workers (inaction track only — action track leads to cliff) ───
            var workerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorkerFbxPath);
            var workerController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(WorkerControllerPath);

            SpawnWorkers("InactionTrackWorkers", workerPrefab, workerController,
                center: new Vector3(0f, 0f, 22f), count: 5, spacing: 1.2f, menuItem: menuItem);

            // ── Rail SplineContainer ──────────────────────────────────────────
            // Index 0 = straight (inaction, toward workers), index 1 = branch (action, toward cliff).
            var railGO = new GameObject("Rail");
            railGO.AddComponent<ManagedBySetupScript>().menuItem = menuItem;
            var railContainer = railGO.AddComponent<SplineContainer>();
            var railSO = new SerializedObject(railContainer);
            var splinesProp = railSO.FindProperty("m_Splines");
            if (splinesProp != null && splinesProp.arraySize < 2)
            {
                splinesProp.arraySize = 2;
                railSO.ApplyModifiedProperties();
            }

            // ── Cliff / rocky mountain (placeholder geometry) ─────────────────
            var cliffGO = new GameObject("Cliff");
            cliffGO.AddComponent<ManagedBySetupScript>().menuItem = menuItem;

            var cliffFace = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cliffFace.name = "CliffFace";
            cliffFace.transform.SetParent(cliffGO.transform, false);
            cliffFace.transform.position = new Vector3(4f, 2f, 31f);
            cliffFace.transform.localScale = new Vector3(5f, 6f, 2f);
            var cliffMat = cliffFace.GetComponent<Renderer>();
            if (cliffMat != null) cliffMat.sharedMaterial.color = new Color(0.45f, 0.38f, 0.30f);

            var cliffEdge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cliffEdge.name = "CliffEdge";
            cliffEdge.transform.SetParent(cliffGO.transform, false);
            cliffEdge.transform.position = new Vector3(4f, -1f, 29f);
            cliffEdge.transform.localScale = new Vector3(5f, 0.3f, 3f);

            // ── Collision effect (activated on impact) ────────────────────────
            var effectGO = new GameObject("CliffCollisionEffect");
            effectGO.AddComponent<ManagedBySetupScript>().menuItem = menuItem;
            effectGO.transform.position = new Vector3(4f, 1f, 30.5f);
            var ps = effectGO.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 1.0f;
            main.startSpeed = 4f;
            main.startSize = 0.3f;
            main.startColor = new Color(0.55f, 0.45f, 0.35f); // dusty brown
            main.loop = false;
            main.playOnAwake = false;
            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });
            effectGO.SetActive(false);

            var collisionAudioSrc = trainGO.AddComponent<AudioSource>();
            collisionAudioSrc.playOnAwake = false;

            // ── Wire TrainController ──────────────────────────────────────────
            var tcSO = new SerializedObject(trainController);
            tcSO.FindProperty("train").objectReferenceValue = trainGO.transform;
            tcSO.FindProperty("rail").objectReferenceValue  = railContainer;
            tcSO.ApplyModifiedProperties();

            // ── Wire TrolleyController ────────────────────────────────────────
            var cSO = new SerializedObject(controller);
            cSO.FindProperty("narrationPlayer").objectReferenceValue = narrationPlayer;
            cSO.FindProperty("decisionTimer").objectReferenceValue = decisionTimer;
            cSO.FindProperty("trainController").objectReferenceValue = trainController;
            TrolleySetupBarrierUtils.AddTransitionBarrier(cSO, menuItem);
            cSO.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("TrolleySelfharmSetup: TrolleySelfharm scene wired and saved.");
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
            RuntimeAnimatorController animController, Vector3 center, int count, float spacing,
            string menuItem = null)
        {
            var group = new GameObject(groupName);
            if (menuItem != null) group.AddComponent<ManagedBySetupScript>().menuItem = menuItem;
            var animators = new Animator[count];
            for (int i = 0; i < count; i++)
            {
                GameObject w;
                if (prefab != null)
                    w = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                else
                {
                    w = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    Debug.LogWarning($"WireSelfharmScene: worker prefab not found — created placeholder for {groupName}.");
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
