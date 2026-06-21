using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Builds the DRIVER practice scene by duplicating the (working) Driver scene, then swapping the
    /// single-decision flow for a repeated rock-blocker drill — the first-person counterpart of the
    /// Bystander tutorial.
    ///
    ///   Trolley > Build Driver Tutorial From Driver
    ///
    /// Result: a TutorialDriverDrill runs reps where one track ahead is blocked by a rocky barrier —
    ///   • rocks on the MAIN track → press the right button (the tram diverts to the clear side track).
    ///   • rocks on the SIDE track → do nothing (stay on the clear main track).
    /// It copies the Driver's movement params (approach dir/speed, divert pivot/angle/radius) off the
    /// DriverTrainController, then removes TrolleyController + DriverTrainController; the two worker groups
    /// are replaced by rocky-mountain barriers (one per track) reusing the self-harm look, in THIS scene
    /// only (their scripts are untouched).
    ///
    /// Does NOT add the scene to Build Settings — register/order it yourself.
    /// Non-destructive to the Driver scene. Overwrites TrolleyTutorialDriver.unity each run — make manual
    /// tweaks (rock placement, speeds, score canvas) only after the last run.
    /// </summary>
    public static class TrolleyDriverTutorialSetup
    {
        const string SourceScene   = "Assets/Trolley/Scenes/TrolleyDriver.unity";
        const string TutorialScene = "Assets/Trolley/Scenes/TrolleyTutorialDriver.unity";

        const string AudioDir = "Assets/Trolley/Audio/";
        const string IntroPath   = AudioDir + "narration_tutorial_driver_intro.mp3";
        const string WindowPath  = AudioDir + "narration_tutorial_driver_window.mp3";
        const string SortPath    = AudioDir + "narration_tutorial_driver_sortingtrain.mp3";
        const string ClosingPath = AudioDir + "narration_tutorial_driver_closing.mp3";
        const string CorrectPath = AudioDir + "sfx_correct.wav";
        const string WrongPath   = AudioDir + "sfx_wrong.wav";

        static readonly Color RockColor = new Color(0.42f, 0.36f, 0.30f); // grey-brown rock (matches self-harm)

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

            // ── Swap the workers on both tracks for rocky-mountain barriers (practice — nobody at
            //    risk). Capture each worker group's parent + local position FIRST so the rock blocker
            //    inherits the exact track position and the same moving parent (rides with TrackEnvironment).
            Transform mainParent = env, sideParent = env;
            Vector3 mainLocal = new Vector3(0f, 0f, -15f), sideLocal = new Vector3(6f, 0f, -15f);
            var inaction = GameObject.Find("InactionTrackWorkers");   // five workers on the MAIN track
            if (inaction != null)
            {
                mainParent = inaction.transform.parent != null ? inaction.transform.parent : env;
                mainLocal  = inaction.transform.localPosition;
                Object.DestroyImmediate(inaction);
            }
            else Debug.LogWarning("Build Driver Tutorial: InactionTrackWorkers not found — main-track rocks placed at a fallback; reposition manually.");
            var action = GameObject.Find("ActionTrackWorkers");       // one worker on the SIDE track
            if (action != null)
            {
                sideParent = action.transform.parent != null ? action.transform.parent : env;
                sideLocal  = action.transform.localPosition;
                Object.DestroyImmediate(action);
            }
            else Debug.LogWarning("Build Driver Tutorial: ActionTrackWorkers not found — side-track rocks placed at a fallback; reposition manually.");

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

            // ── Rock blockers, one per track (the blocked track is the one to avoid) ──
            // Reuses the self-harm rocky-mountain look. Each rides with its track; the drill shows only
            // one per round. Start hidden.
            var mainBlocker = BuildRockBlocker("RockBlocker_Main", mainParent, mainLocal);
            var sideBlocker = BuildRockBlocker("RockBlocker_Side", sideParent, sideLocal);
            mainBlocker.SetActive(false);
            sideBlocker.SetActive(false);

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
            dSO.FindProperty("mainTrackBlocker").objectReferenceValue  = mainBlocker;
            dSO.FindProperty("sideTrackBlocker").objectReferenceValue  = sideBlocker;
            dSO.FindProperty("narrationSource").objectReferenceValue   = narrSrc;
            dSO.FindProperty("scoreText").objectReferenceValue         = scoreText;
            dSO.FindProperty("sfxSource").objectReferenceValue         = sfx;
            AssignClips(dSO);
            dSO.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Build Driver Tutorial: TrolleyTutorialDriver.unity created (rock-blocker drill).\n" +
                      "Done: copied movement params off DriverTrainController, removed it + TrolleyController; " +
                      "replaced both worker groups with RockBlocker_Main / RockBlocker_Side; created score canvas + " +
                      "SFX + narration source; wired TutorialDriverDrill.\n" +
                      "MANUAL: (1) ADD TrolleyTutorialDriver to Build Settings yourself (order is yours); " +
                      "(2) check RockBlocker_Main / RockBlocker_Side sit visibly on their tracks ahead of the cab, " +
                      "and reposition DrillScoreCanvas; (3) record the driver narration (narration_tutorial_driver_*.mp3) " +
                      "— any 'clip not found' warnings above list the missing files — then re-run 'Trolley > Driver " +
                      "Tutorial – Assign Narration & SFX Clips'; (4) tune approachDistance / postForkDistance to the rail; " +
                      "(5) point the Bystander tutorial's nextSceneAfterDrill at this scene (already set to TrolleyTutorialDriver).");
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
            Set("windowClip",  WindowPath,  "driver_window");
            Set("sortClip",    SortPath,    "driver_sortingtrain");
            Set("closingClip", ClosingPath, "driver_closing");
            Set("correctClip", CorrectPath, "sfx_correct");
            Set("wrongClip",   WrongPath,   "sfx_wrong");
        }

        // A rocky-mountain barrier (same look as the self-harm scene). Parented to the track so it rides
        // toward the seated player; collider stripped — it's a visual practice cue, not a physical block.
        static GameObject BuildRockBlocker(string name, Transform parent, Vector3 localPos)
        {
            var root = new GameObject(name);
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;

            AddRock(root.transform, new Vector3(0f, 2f, 0f),      new Vector3(6f, 8f, 4f),  0f);
            AddRock(root.transform, new Vector3(-2.5f, 1f, 1f),   new Vector3(3f, 4f, 3f),  20f);
            AddRock(root.transform, new Vector3(2.5f, 1.2f, -1f), new Vector3(3.5f, 5f, 3f),-15f);
            return root;
        }

        static void AddRock(Transform parent, Vector3 localPos, Vector3 scale, float yaw)
        {
            var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = "Rock";
            rock.transform.SetParent(parent, false);
            rock.transform.localPosition = localPos;
            rock.transform.localScale = scale;
            rock.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var col = rock.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            var rend = rock.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = Object.Instantiate(rend.sharedMaterial);  // default URP Lit — no magenta
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", RockColor);
                if (mat.HasProperty("_Color"))     mat.SetColor("_Color", RockColor);
                rend.sharedMaterial = mat;
            }
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
            tmp.text = "Correct decisions: 0 / 3";
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
