using System;
using UnityEngine;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// ScriptableObject defining all questionnaire questions.
    /// Create via Assets > Create > Trolley > Question Set.
    /// After updating this script, reset or recreate the TrolleyQuestions asset
    /// so new question arrays appear in the Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "QuestionSet", menuName = "Trolley/Question Set")]
    public class QuestionSet : ScriptableObject
    {
        public enum QuestionType { Likert5, Likert7 }

        [Serializable]
        public class Question
        {
            [TextArea(2, 4)] public string text = "Placeholder question text.";
            public QuestionType type = QuestionType.Likert5;
            public string scaleMin = "Strongly disagree";
            public string scaleMax = "Strongly agree";
        }

        // ── Post-scenario: shown after every scenario, solo and paired ────────

        [Header("Post-scenario — all participants (Q1–Q5)")]
        public Question[] postScenarioCommon = new Question[]
        {
            new Question { text = "The decision felt like mine.",
                           scaleMin = "Strongly disagree", scaleMax = "Strongly agree" },
            new Question { text = "I felt personally responsible for the outcome.",
                           scaleMin = "Strongly disagree", scaleMax = "Strongly agree" },
            new Question { text = "I regret the decision that was made.",
                           scaleMin = "Strongly disagree", scaleMax = "Strongly agree" },
            new Question { text = "I would make the same decision again.",
                           scaleMin = "Strongly disagree", scaleMax = "Strongly agree" },
            new Question { text = "I felt personally at risk during this scenario.",
                           scaleMin = "Strongly disagree", scaleMax = "Strongly agree" },
        };

        // ── Post-scenario: paired condition only ──────────────────────────────

        [Header("Post-scenario — paired condition only (Q6–Q7)")]
        public Question[] postScenarioPairedOnly = new Question[]
        {
            new Question { text = "My partner influenced my decision.",
                           scaleMin = "Strongly disagree", scaleMax = "Strongly agree" },
            new Question { text = "My partner's presence made the situation feel less threatening.",
                           scaleMin = "Strongly disagree", scaleMax = "Strongly agree" },
        };
    }
}
