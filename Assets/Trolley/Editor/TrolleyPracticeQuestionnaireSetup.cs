using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Builds the practice ("fake") questionnaire scene by duplicating the real, hand-tuned
    /// Questionnaire scene, then flipping the QuestionnaireController into practice mode.
    ///
    ///   Trolley > Build Practice Questionnaire From Questionnaire
    ///
    /// Result: TrolleyPracticeQuestionnaire.unity — identical booths/UI to the real one, but
    ///   • practiceMode = true (generic reflection prompt, no DataLogger writes),
    ///   • a 2-question PracticeQuestions set so participants rehearse the slider + Done/Record flow,
    ///   • loads the first real scenario when finished.
    ///
    /// Non-destructive to TrolleyQuestionnaire.unity. Overwrites TrolleyPracticeQuestionnaire.unity
    /// each run — make manual tweaks only after the final run. The TutorialBystanderDrill loads this
    /// scene by name after the colour drill.
    /// </summary>
    public static class TrolleyPracticeQuestionnaireSetup
    {
        const string SourceScene   = "Assets/Trolley/Scenes/TrolleyQuestionnaire.unity";
        const string PracticeScene = "Assets/Trolley/Scenes/TrolleyPracticeQuestionnaire.unity";
        const string QuestionsPath = "Assets/Trolley/PracticeQuestions.asset";

        [MenuItem("Trolley/Build Practice Questionnaire From Questionnaire")]
        public static void BuildPracticeQuestionnaire()
        {
            var src = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
            if (!src.IsValid()) { Debug.LogError($"Build Practice Questionnaire: could not open {SourceScene}."); return; }

            if (!EditorSceneManager.SaveScene(src, PracticeScene, saveAsCopy: true))
            {
                Debug.LogError($"Build Practice Questionnaire: failed to save copy to {PracticeScene}.");
                return;
            }
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene(PracticeScene, OpenSceneMode.Single);

            var practiceQuestions = GetOrCreatePracticeQuestions();

            var ctrl = Object.FindFirstObjectByType<QuestionnaireController>();
            if (ctrl == null)
            {
                Debug.LogError("Build Practice Questionnaire: QuestionnaireController not found in the scene — " +
                               "cannot enable practice mode.");
                return;
            }

            var so = new SerializedObject(ctrl);
            SetBool(so, "practiceMode", true);
            SetObject(so, "practiceQuestionSet", practiceQuestions);
            // practiceNextScene left empty → falls back to the first scenario from the order.
            so.ApplyModifiedProperties();

            AddToBuildSettings(PracticeScene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Build Practice Questionnaire: TrolleyPracticeQuestionnaire.unity created.\n" +
                      "Done: duplicated the questionnaire scene, set practiceMode=true, assigned PracticeQuestions " +
                      "(2 items), added to Build Settings. No data is logged in this scene; it loads the first " +
                      "real scenario when finished.\n" +
                      "MANUAL: nothing required. The tutorial drill already points at this scene by name " +
                      "(TutorialBystanderDrill.practiceQuestionnaireScene).");
        }

        static QuestionSet GetOrCreatePracticeQuestions()
        {
            var existing = AssetDatabase.LoadAssetAtPath<QuestionSet>(QuestionsPath);
            if (existing != null) return existing;

            var qs = ScriptableObject.CreateInstance<QuestionSet>();
            qs.postScenarioCommon = new[]
            {
                new QuestionSet.Question
                {
                    text = "This is a practice question. Drag the slider to any point to try it out.",
                    type = QuestionSet.QuestionType.Likert5,
                    scaleMin = "Strongly disagree", scaleMax = "Strongly agree",
                },
                new QuestionSet.Question
                {
                    text = "This is a practice question. I now understand how to answer with the scale.",
                    type = QuestionSet.QuestionType.Likert5,
                    scaleMin = "Strongly disagree", scaleMax = "Strongly agree",
                },
            };
            qs.postScenarioPairedOnly = new QuestionSet.Question[0];

            AssetDatabase.CreateAsset(qs, QuestionsPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Build Practice Questionnaire: created {QuestionsPath} (2 practice questions).");
            return qs;
        }

        static void SetBool(SerializedObject so, string field, bool value)
        {
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"Build Practice Questionnaire: field '{field}' not found on QuestionnaireController."); return; }
            p.boolValue = value;
        }

        static void SetObject(SerializedObject so, string field, Object value)
        {
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"Build Practice Questionnaire: field '{field}' not found on QuestionnaireController."); return; }
            p.objectReferenceValue = value;
        }

        static void AddToBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == scenePath)) return;
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"Build Practice Questionnaire: added {scenePath} to Build Settings.");
        }
    }
}
