using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Builds the participant practice scene by duplicating the (working) Bystander scene,
    /// then swapping the single-decision flow for a repeated colour drill.
    ///
    ///   Trolley > Build Tutorial From Bystander
    ///
    /// Result: a TutorialTrainDrill runs a sequence of trains one at a time —
    ///   • RED  train → do nothing (it runs straight/left).
    ///   • BLUE train → press the button (it diverts right).
    /// A top-right counter tracks correct handlings; a ding/buzz plays each round; after all
    /// trains the first real scenario loads. The original TrolleyController/TrainController are
    /// removed from THIS scene only (their scripts are untouched).
    ///
    /// Reuses the Bystander rail spline, train mesh, and A/B button. Non-destructive to the
    /// Bystander scene. Overwrites TrolleyTutorial.unity each run — manual tweaks after the last run.
    /// </summary>
    public static class TrolleyTutorialSetup
    {
        const string SourceScene   = "Assets/Trolley/Scenes/TrolleyBystander.unity";
        const string TutorialScene = "Assets/Trolley/Scenes/TrolleyTutorialBystander.unity";

        const string AudioDir = "Assets/Trolley/Audio/";
        // Round 1 intro (preamble ×2, then one clip per monitor)
        const string IntroPath    = AudioDir + "narration_tutorial_bystander_intro.mp3";
        const string MonitorsPath = AudioDir + "narration_tutorial_bystander_monitors.mp3";
        const string ApproachPath = AudioDir + "narration_tutorial_bystander_monitor_approach.mp3";
        const string SwitchPath   = AudioDir + "narration_tutorial_bystander_monitor_switch.mp3";
        const string MainPath     = AudioDir + "narration_tutorial_bystander_monitor_main.mp3";
        const string SidePath     = AudioDir + "narration_tutorial_bystander_monitor_side.mp3";
        // Round 1 button practice
        const string PressPath    = AudioDir + "narration_tutorial_bystander_button_main.mp3";
        const string BackPath     = AudioDir + "narration_tutorial_bystander_button_side.mp3";
        const string ConfirmPath  = AudioDir + "narration_tutorial_bystander_button_confirm.mp3";
        // Round 2
        const string SortPath     = AudioDir + "narration_tutorial_bystander_sortingtrain.mp3";
        // Closing line (after 5 correct, before the next tutorial)
        const string ClosingPath  = AudioDir + "narration_tutorial_bystander_closing.mp3";
        // SFX
        const string CorrectPath  = AudioDir + "sfx_correct.wav";
        const string WrongPath    = AudioDir + "sfx_wrong.wav";

        [MenuItem("Trolley/Build Tutorial From Bystander")]
        public static void BuildTutorialFromBystander()
        {
            var src = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
            if (!src.IsValid()) { Debug.LogError($"Build Tutorial: could not open {SourceScene}."); return; }

            if (!EditorSceneManager.SaveScene(src, TutorialScene, saveAsCopy: true))
            {
                Debug.LogError($"Build Tutorial: failed to save copy to {TutorialScene}.");
                return;
            }
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene(TutorialScene, OpenSceneMode.Single);

            // ── Grab the train + rail from the Bystander TrainController, then remove it ──
            Object trainRef = null, railRef = null;
            var trainCtrl = Object.FindFirstObjectByType<TrainController>();
            if (trainCtrl != null)
            {
                var tcSO = new SerializedObject(trainCtrl);
                trainRef = tcSO.FindProperty("train").objectReferenceValue;
                railRef  = tcSO.FindProperty("rail").objectReferenceValue;
                Object.DestroyImmediate(trainCtrl); // the train mesh + rail object remain
            }
            else Debug.LogWarning("Build Tutorial: TrainController not found — wire the drill's train/rail manually.");

            // ── Remove the single-decision controller so it doesn't run here ──
            var trolley = Object.FindFirstObjectByType<TrolleyController>();
            if (trolley != null) Object.DestroyImmediate(trolley);

            // ── Remove workers from both tracks (practice — nobody at risk) ────
            foreach (var name in new[] { "InactionTrackWorkers", "ActionTrackWorkers" })
            {
                var go = GameObject.Find(name);
                if (go != null) Object.DestroyImmediate(go);
            }

            // ── Hide the idle scenario timer canvas ────────────────────────────
            var timerCanvas = GameObject.Find("TimerCanvas");
            if (timerCanvas != null) timerCanvas.SetActive(false);

            // ── Reused input: the A/B toggle owns the control buttons + the two lower monitor rims ──
            var toggle = Object.FindFirstObjectByType<TrolleyToggleDecision>();
            GameObject buttonA = null, buttonB = null, rimMain = null, rimSide = null;
            if (toggle != null)
            {
                var tSO = new SerializedObject(toggle);
                buttonA = tSO.FindProperty("buttonA").objectReferenceValue as GameObject;
                buttonB = tSO.FindProperty("buttonB").objectReferenceValue as GameObject;
                rimMain = tSO.FindProperty("rimA").objectReferenceValue as GameObject; // Track1East = main/current
                rimSide = tSO.FindProperty("rimB").objectReferenceValue as GameObject; // Track2East = diverting
            }
            else Debug.LogWarning("Build Tutorial: TrolleyToggleDecision not found — tutorial has no input.");

            // The two upper monitors have no rim yet — clone the existing rim onto them so all four
            // can blink during the intro. (Reuses the same rim object; not new highlight code.)
            var rimApproach = CloneRim(rimMain, "Monitor_WestView",    "RimApproach");
            var rimSwitch   = CloneRim(rimMain, "Monitor_SwitchPoint", "RimSwitch");

            // ── Silence the Bystander NarrationPlayer — the tutorial uses its own clips ──
            var narration = Object.FindFirstObjectByType<NarrationPlayer>();
            if (narration != null)
            {
                var nSO = new SerializedObject(narration);
                nSO.FindProperty("clips").arraySize = 0;
                nSO.ApplyModifiedProperties();
            }

            // ── Tutorial narration source (clips auto-loaded below via AssignClips) ──
            var narrGO = new GameObject("TutorialNarration");
            var narrSrc = narrGO.AddComponent<AudioSource>();
            narrSrc.playOnAwake = false;
            narrSrc.loop = false;

            // ── Top-right score counter (world-space; reposition to taste) ─────
            var scoreText = BuildScoreCanvas();

            // ── SFX source (assign ding/buzz clips after; non-fatal if empty) ──
            var sfxGO = new GameObject("DrillSFX");
            var sfx = sfxGO.AddComponent<AudioSource>();
            sfx.playOnAwake = false;

            // ── The tutorial controller ────────────────────────────────────────
            var drillGO = new GameObject("TutorialTrainDrill");
            var drill = drillGO.AddComponent<TutorialTrainDrill>();
            var dSO = new SerializedObject(drill);
            dSO.FindProperty("rail").objectReferenceValue           = railRef;
            dSO.FindProperty("train").objectReferenceValue          = trainRef;
            dSO.FindProperty("toggle").objectReferenceValue         = toggle;
            dSO.FindProperty("rimApproach").objectReferenceValue    = rimApproach;
            dSO.FindProperty("rimSwitch").objectReferenceValue      = rimSwitch;
            dSO.FindProperty("rimMain").objectReferenceValue        = rimMain;
            dSO.FindProperty("rimSide").objectReferenceValue        = rimSide;
            dSO.FindProperty("buttonA").objectReferenceValue        = buttonA;
            dSO.FindProperty("buttonB").objectReferenceValue        = buttonB;
            dSO.FindProperty("narrationSource").objectReferenceValue = narrSrc;
            dSO.FindProperty("scoreText").objectReferenceValue      = scoreText;
            dSO.FindProperty("sfxSource").objectReferenceValue      = sfx;
            AssignClips(dSO); // 10 narration + 2 SFX clips, loaded by path
            dSO.ApplyModifiedProperties();

            AddToBuildSettings(TutorialScene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Build Tutorial: TrolleyTutorial.unity created (two-round practice).\n" +
                      "Done: workers removed, TrolleyController/TrainController removed; cloned RimApproach/RimSwitch " +
                      "onto the two upper monitors; TutorialNarration source created; TutorialTrainDrill wired " +
                      "(rail/train/toggle/4 rims/2 buttons/10 narration + 2 SFX clips/score/sfx); added to Build Settings.\n" +
                      "MANUAL: (1) reposition DrillScoreCanvas top-right; nudge RimApproach/RimSwitch to sit on their " +
                      "monitors; (2) check trainSpeed and set divertThreshold to the switch point on the rail; " +
                      "(3) run 'Trolley > Build Practice Questionnaire From Questionnaire' so the after-scene exists; " +
                      "(4) to run it in the flow, point AvatarSetupController at TrolleyGameState.tutorialScene " +
                      "(that line is in AvatarSetup — left for you).\n" +
                      "Any 'clip not found' warnings above just mean that recording isn't in Assets/Trolley/Audio yet — " +
                      "drop it in and re-run 'Trolley > Tutorial – Assign Narration & SFX Clips' (non-destructive).");
        }

        /// <summary>
        /// Non-destructive: assigns all 10 narration + 2 SFX clips to the TutorialTrainDrill in the
        /// currently open scene. Use after recording (or re-recording) clips — it touches only the
        /// clip fields, so manual tweaks (rim placement, score canvas, speeds) are preserved.
        /// </summary>
        [MenuItem("Trolley/Tutorial – Assign Narration & SFX Clips")]
        public static void AssignTutorialClips()
        {
            var drill = Object.FindFirstObjectByType<TutorialTrainDrill>();
            if (drill == null)
            {
                Debug.LogError("Assign Tutorial Clips: no TutorialTrainDrill in the open scene — " +
                               "open TrolleyTutorial.unity first.");
                return;
            }
            var dSO = new SerializedObject(drill);
            AssignClips(dSO);
            dSO.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(drill.gameObject.scene);
            EditorSceneManager.SaveScene(drill.gameObject.scene);
            Debug.Log("Assign Tutorial Clips: wired 10 narration + 2 SFX clips to TutorialTrainDrill " +
                      "(non-destructive). Any 'clip not found' warnings above are still-missing files.");
        }

        // Loads every narration + SFX clip by path and assigns it to its field. Missing files warn
        // (via LoadClip) and are left null. Shared by the full builder and the targeted assign menu.
        static void AssignClips(SerializedObject dSO)
        {
            void Set(string field, string path, string label) =>
                dSO.FindProperty(field).objectReferenceValue = LoadClip(path, label);

            Set("introClip",         IntroPath,    "bystander_intro");
            Set("monitorsClip",      MonitorsPath, "bystander_monitors");
            Set("introApproachClip", ApproachPath, "bystander_monitor_approach");
            Set("introSwitchClip",   SwitchPath,   "bystander_monitor_switch");
            Set("introMainClip",     MainPath,     "bystander_monitor_main");
            Set("introSideClip",     SidePath,     "bystander_monitor_side");
            Set("pressClip",         PressPath,    "bystander_button_main");
            Set("backClip",          BackPath,     "bystander_button_side");
            Set("confirmClip",       ConfirmPath,  "bystander_button_confirm");
            Set("sortClip",          SortPath,     "bystander_sortingtrain");
            Set("closingClip",       ClosingPath,  "bystander_closing");
            Set("correctClip",       CorrectPath,  "sfx_correct");
            Set("wrongClip",         WrongPath,    "sfx_wrong");
        }

        static TextMeshProUGUI BuildScoreCanvas()
        {
            var canvasGO = new GameObject("DrillScoreCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.GetComponent<RectTransform>().sizeDelta = new Vector2(500f, 200f);
            // Reasonable spot in the control room, upper-right, facing the player. Reposition in-editor.
            // Rotation matches the monitors (identity) so the text reads the right way round, not mirrored.
            canvasGO.transform.position   = new Vector3(1.6f, 2.4f, 2.2f);
            canvasGO.transform.rotation   = Quaternion.identity;
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

        // Clones the existing monitor rim onto another monitor so all four can blink in the intro.
        static GameObject CloneRim(GameObject rimSource, string monitorName, string newName)
        {
            if (rimSource == null)
            {
                Debug.LogWarning($"Build Tutorial: no source rim (toggle RimA) — cannot create {newName}.");
                return null;
            }
            var monitor = FindInScene(monitorName);
            if (monitor == null)
            {
                Debug.LogWarning($"Build Tutorial: monitor '{monitorName}' not found — cannot place {newName}.");
                return null;
            }
            var clone = Object.Instantiate(rimSource, monitor.transform);
            clone.name = newName;
            clone.transform.localPosition = rimSource.transform.localPosition;
            clone.transform.localRotation = rimSource.transform.localRotation;
            clone.transform.localScale    = rimSource.transform.localScale;
            clone.SetActive(false);
            return clone;
        }

        static AudioClip LoadClip(string path, string label)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
                Debug.LogWarning($"Build Tutorial: {label} not found at {path} — record + assign it on TutorialTrainDrill.");
            return clip;
        }

        static GameObject FindInScene(string name)
        {
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name) return t.gameObject;
            return null;
        }

        static void AddToBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == scenePath)) return;
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"Build Tutorial: added {scenePath} to Build Settings.");
        }
    }
}
