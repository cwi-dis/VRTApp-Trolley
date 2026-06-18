using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Builds the DRIVER practice scene by duplicating the (working) Driver scene, then swapping the
    /// single-decision flow for a repeated signal drill — the first-person counterpart of the Bystander
    /// tutorial.
    ///
    ///   Trolley > Build Driver Tutorial From Driver
    ///
    /// Result: a TutorialDriverDrill runs reps where a signal light ahead turns RED or BLUE —
    ///   • BLUE → press the right button (the tram diverts).
    ///   • RED  → do nothing (stay on the main track).
    /// It copies the Driver's movement params (approach dir/speed, divert pivot/angle/radius) off the
    /// DriverTrainController, then removes TrolleyController + DriverTrainController and both worker groups
    /// from THIS scene only (their scripts are untouched).
    ///
    /// Does NOT add the scene to Build Settings — register/order it yourself.
    /// Non-destructive to the Driver scene. Overwrites TrolleyTutorialDriver.unity each run — make manual
    /// tweaks (signal placement, speeds, score canvas) only after the last run.
    /// </summary>
    public static class TrolleyDriverTutorialSetup
    {
        const string SourceScene   = "Assets/Trolley/Scenes/TrolleyDriver.unity";
        const string TutorialScene = "Assets/Trolley/Scenes/TrolleyTutorialDriver.unity";

        const string AudioDir = "Assets/Trolley/Audio/";
        const string IntroPath   = AudioDir + "narration_tutorial_driver_intro.mp3";
        const string ButtonsPath  = AudioDir + "narration_tutorial_driver_buttons.mp3";
        const string SignalPath   = AudioDir + "narration_tutorial_driver_signal.mp3";
        const string PressPath    = AudioDir + "narration_tutorial_driver_button_main.mp3";
        const string BackPath     = AudioDir + "narration_tutorial_driver_button_side.mp3";
        const string ConfirmPath  = AudioDir + "narration_tutorial_driver_button_confirm.mp3";
        const string SortPath     = AudioDir + "narration_tutorial_driver_sortingtrain.mp3";
        const string ClosingPath  = AudioDir + "narration_tutorial_driver_closing.mp3";
        const string CorrectPath  = AudioDir + "sfx_correct.wav";
        const string WrongPath    = AudioDir + "sfx_wrong.wav";

        [MenuItem("Trolley/Build Driver Tutorial From Driver")]
        public static void BuildDriverTutorialFromDriver()
        {
            var src = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
            if (!src.IsValid()) { Debug.LogError($"Build Driver Tutorial: could not open {SourceScene}."); return; }
            if (!EditorSceneManager.SaveScene(src, TutorialScene, saveAsCopy: true))
            {
                Debug.LogError($"Build Driver Tutorial: failed to save copy to {TutorialScene}.");
                return;
            }
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene(TutorialScene, OpenSceneMode.Single);

            // ── Copy the movement params off DriverTrainController, then remove it ──
            Transform env = null, divertPivot = null;
            Vector3 approachDir = Vector3.back;
            float approachSpeed = 9.5f, branchTurnAngle = -90f, branchRadius = 95f;
            var dtc = Object.FindFirstObjectByType<DriverTrainController>();
            if (dtc != null)
            {
                env = dtc.transform; // DriverTrainController lives on the moving TrackEnvironment
                var s = new SerializedObject(dtc);
                approachDir     = s.FindProperty("approachDirection").vector3Value;
                approachSpeed   = s.FindProperty("approachSpeed").floatValue;
                divertPivot     = s.FindProperty("divertPivot").objectReferenceValue as Transform;
                branchTurnAngle = s.FindProperty("branchTurnAngle").floatValue;
                branchRadius    = s.FindProperty("branchRadius").floatValue;
                Object.DestroyImmediate(dtc);
            }
            else Debug.LogWarning("Build Driver Tutorial: DriverTrainController not found — wire environment/divertPivot manually.");

            // ── Remove the single-decision controller so it doesn't run here ──
            var trolley = Object.FindFirstObjectByType<TrolleyController>();
            if (trolley != null) Object.DestroyImmediate(trolley);

            // ── Remove workers from both tracks (practice — nobody at risk) ────
            foreach (var name in new[] { "InactionTrackWorkers", "ActionTrackWorkers" })
            {
                var go = GameObject.Find(name);
                if (go != null) Object.DestroyImmediate(go);
            }

            // ── Reused input: the A/B toggle ───────────────────────────────────
            var toggle = Object.FindFirstObjectByType<TrolleyToggleDecision>();
            if (toggle == null) Debug.LogWarning("Build Driver Tutorial: TrolleyToggleDecision not found — tutorial has no input.");

            // ── Silence the Driver NarrationPlayer — the tutorial uses its own clips ──
            var narration = Object.FindFirstObjectByType<NarrationPlayer>();
            if (narration != null)
            {
                var nSO = new SerializedObject(narration);
                var clips = nSO.FindProperty("clips");
                if (clips != null) clips.arraySize = 0;
                nSO.ApplyModifiedProperties();
            }

            // ── Signal light ahead (reposition in-editor to taste) ─────────────
            var signal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            signal.name = "SignalLight";
            signal.transform.position = new Vector3(0f, 2.2f, 14f);
            signal.transform.localScale = Vector3.one * 0.8f;
            var sigCol = signal.GetComponent<Collider>();
            if (sigCol != null) Object.DestroyImmediate(sigCol);
            var sigShader = Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
            signal.GetComponent<Renderer>().sharedMaterial = new Material(sigShader) { name = "M_SignalLight" };
            signal.SetActive(false);

            // ── Score counter + SFX + narration source ─────────────────────────
            var scoreText = BuildScoreCanvas();
            var sfxGO = new GameObject("DrillSFX");
            var sfx = sfxGO.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            var narrGO = new GameObject("TutorialNarration");
            var narrSrc = narrGO.AddComponent<AudioSource>();
            narrSrc.playOnAwake = false; narrSrc.loop = false;

            // ── The tutorial controller ────────────────────────────────────────
            var drillGO = new GameObject("TutorialDriverDrill");
            var drill = drillGO.AddComponent<TutorialDriverDrill>();
            var dSO = new SerializedObject(drill);
            dSO.FindProperty("environment").objectReferenceValue       = env;
            dSO.FindProperty("approachDirection").vector3Value         = approachDir;
            dSO.FindProperty("approachSpeed").floatValue               = approachSpeed;
            dSO.FindProperty("divertPivot").objectReferenceValue       = divertPivot;
            dSO.FindProperty("branchTurnAngle").floatValue             = branchTurnAngle;
            dSO.FindProperty("branchRadius").floatValue                = branchRadius;
            dSO.FindProperty("toggle").objectReferenceValue            = toggle;
            dSO.FindProperty("signalLight").objectReferenceValue       = signal;
            dSO.FindProperty("narrationSource").objectReferenceValue   = narrSrc;
            dSO.FindProperty("scoreText").objectReferenceValue         = scoreText;
            dSO.FindProperty("sfxSource").objectReferenceValue         = sfx;
            AssignClips(dSO);
            dSO.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Build Driver Tutorial: TrolleyTutorialDriver.unity created (signal drill).\n" +
                      "Done: copied movement params off DriverTrainController, removed it + TrolleyController + " +
                      "both worker groups; created SignalLight + score canvas + SFX + narration source; wired " +
                      "TutorialDriverDrill.\n" +
                      "MANUAL: (1) ADD TrolleyTutorialDriver to Build Settings yourself (order is yours); " +
                      "(2) reposition SignalLight in front of the cab and DrillScoreCanvas; (3) record the driver " +
                      "narration (narration_tutorial_driver_*.mp3) — any 'clip not found' warnings above list the " +
                      "missing files — then re-run 'Trolley > Driver Tutorial – Assign Narration & SFX Clips'; " +
                      "(4) tune approachDistance / postForkDistance to the rail; (5) point the Bystander tutorial's " +
                      "nextSceneAfterDrill at this scene (already set to TrolleyTutorialDriver).");
        }

        /// <summary>Non-destructive: (re)assign the driver-tutorial narration + SFX clips after recording.</summary>
        [MenuItem("Trolley/Driver Tutorial – Assign Narration & SFX Clips")]
        public static void AssignDriverTutorialClips()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(TutorialScene, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"Assign Driver Tutorial Clips: could not open {TutorialScene} — " +
                               "run 'Trolley > Build Driver Tutorial From Driver' first.");
                return;
            }
            var drill = Object.FindFirstObjectByType<TutorialDriverDrill>(FindObjectsInactive.Include);
            if (drill == null)
            {
                Debug.LogError($"Assign Driver Tutorial Clips: no TutorialDriverDrill in {TutorialScene} — " +
                               "run 'Trolley > Build Driver Tutorial From Driver' first.");
                return;
            }
            var dSO = new SerializedObject(drill);
            AssignClips(dSO);
            dSO.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Assign Driver Tutorial Clips: wired narration + SFX (any 'not found' warnings = missing files).");
        }

        static void AssignClips(SerializedObject dSO)
        {
            void Set(string field, string path, string label) =>
                dSO.FindProperty(field).objectReferenceValue = LoadClip(path, label);

            Set("introClip",   IntroPath,   "driver_intro");
            Set("buttonsClip", ButtonsPath, "driver_buttons");
            Set("signalClip",  SignalPath,  "driver_signal");
            Set("pressClip",   PressPath,   "driver_button_main");
            Set("backClip",    BackPath,    "driver_button_side");
            Set("confirmClip", ConfirmPath, "driver_button_confirm");
            Set("sortClip",    SortPath,    "driver_sortingtrain");
            Set("closingClip", ClosingPath, "driver_closing");
            Set("correctClip", CorrectPath, "sfx_correct");
            Set("wrongClip",   WrongPath,   "sfx_wrong");
        }

        static TextMeshProUGUI BuildScoreCanvas()
        {
            var canvasGO = new GameObject("DrillScoreCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.GetComponent<RectTransform>().sizeDelta = new Vector2(500f, 200f);
            canvasGO.transform.position   = new Vector3(1.4f, 2.2f, 3.5f);
            canvasGO.transform.rotation   = Quaternion.identity; // faces the player, not mirrored
            canvasGO.transform.localScale = Vector3.one * 0.004f;

            var textGO = new GameObject("ScoreText");
            textGO.transform.SetParent(canvasGO.transform, false);
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "Correct decisions: 0 / 5";
            tmp.fontSize = 60;
            tmp.alignment = TextAlignmentOptions.TopRight;
            tmp.color = Color.white;
            var rect = textGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return tmp;
        }

        static AudioClip LoadClip(string path, string label)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
                Debug.LogWarning($"Build Driver Tutorial: {label} not found at {path} — record + assign it.");
            return clip;
        }
    }
}
