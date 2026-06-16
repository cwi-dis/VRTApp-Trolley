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
        const string TutorialScene = "Assets/Trolley/Scenes/TrolleyTutorial.unity";
        const string NarrationPath = "Assets/Trolley/Audio/narration_tutorial.mp3";

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

            // ── Keep the reused pieces ─────────────────────────────────────────
            var toggle    = Object.FindFirstObjectByType<TrolleyToggleDecision>();
            var narration = Object.FindFirstObjectByType<NarrationPlayer>();
            if (toggle == null) Debug.LogWarning("Build Tutorial: TrolleyToggleDecision not found — drill has no input.");

            // narration: tutorial clip if present, else clear so Bystander narration doesn't play
            if (narration != null)
            {
                var nSO = new SerializedObject(narration);
                var clipsProp = nSO.FindProperty("clips");
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(NarrationPath);
                if (clip != null) { clipsProp.arraySize = 1; clipsProp.GetArrayElementAtIndex(0).objectReferenceValue = clip; }
                else { clipsProp.arraySize = 0; Debug.LogWarning("Build Tutorial: narration_tutorial.mp3 not found — cleared clips (record + assign it)."); }
                nSO.ApplyModifiedProperties();
            }

            // ── Top-right score counter (world-space; reposition to taste) ─────
            var scoreText = BuildScoreCanvas();

            // ── SFX source (assign ding/buzz clips after; non-fatal if empty) ──
            var sfxGO = new GameObject("DrillSFX");
            var sfx = sfxGO.AddComponent<AudioSource>();
            sfx.playOnAwake = false;

            // ── The drill controller ───────────────────────────────────────────
            var drillGO = new GameObject("TutorialTrainDrill");
            var drill = drillGO.AddComponent<TutorialTrainDrill>();
            var dSO = new SerializedObject(drill);
            dSO.FindProperty("rail").objectReferenceValue           = railRef;
            dSO.FindProperty("train").objectReferenceValue          = trainRef;
            dSO.FindProperty("toggle").objectReferenceValue         = toggle;
            dSO.FindProperty("narrationPlayer").objectReferenceValue = narration;
            dSO.FindProperty("scoreText").objectReferenceValue      = scoreText;
            dSO.FindProperty("sfxSource").objectReferenceValue      = sfx;
            dSO.ApplyModifiedProperties();

            AddToBuildSettings(TutorialScene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Build Tutorial: TrolleyTutorial.unity created.\n" +
                      "Done: workers removed, TrolleyController/TrainController removed, TutorialTrainDrill wired " +
                      "(rail/train/toggle/score), added to Build Settings.\n" +
                      "MANUAL: (1) assign ding/buzz clips to DrillSFX (correctClip/wrongClip) on TutorialTrainDrill; " +
                      "(2) record + assign narration_tutorial.mp3; (3) reposition DrillScoreCanvas to the top-right of " +
                      "the player's view; (4) check trainSpeed/decisionWindow feel; (5) to run it in the flow, point " +
                      "AvatarSetupController at TrolleyGameState.tutorialScene (that line is in AvatarSetup — left for you).");
        }

        static TextMeshProUGUI BuildScoreCanvas()
        {
            var canvasGO = new GameObject("DrillScoreCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.GetComponent<RectTransform>().sizeDelta = new Vector2(500f, 200f);
            // Reasonable spot in the control room, upper-right, facing the player. Reposition in-editor.
            canvasGO.transform.position   = new Vector3(1.6f, 2.4f, 2.2f);
            canvasGO.transform.rotation   = Quaternion.Euler(0f, 180f, 0f);
            canvasGO.transform.localScale = Vector3.one * 0.004f;

            var textGO = new GameObject("ScoreText");
            textGO.transform.SetParent(canvasGO.transform, false);
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "Correct: 0 / 10";
            tmp.fontSize = 60;
            tmp.alignment = TextAlignmentOptions.TopRight;
            tmp.color = Color.white;
            var rect = textGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return tmp;
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
