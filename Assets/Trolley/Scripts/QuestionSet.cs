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

        [Header("Post-scenario — all participants (Q1–Q7, Q10)")]
        public Question[] postScenarioCommon = new Question[]
        {
            new Question { text = "I felt in control of the decision I made.",
                           scaleMin = "Strongly disagree", scaleMax = "Strongly agree" },
            new Question { text = "I felt personally responsible for the outcome.",
                           scaleMin = "Strongly disagree", scaleMax = "Strongly agree" },
            new Question { text = "I am satisfied with the decision I made.",
                           scaleMin = "Strongly disagree", scaleMax = "Strongly agree" },
            new Question { text = "I acted on instinct rather than deliberation.",
                           scaleMin = "Strongly disagree", scaleMax = "Strongly agree" },
            new Question { text = "The time pressure significantly affected my decision.",
                           scaleMin = "Strongly disagree", scaleMax = "Strongly agree" },
            new Question { text = "I considered not acting at all.",
                           scaleMin = "Strongly disagree", scaleMax = "Strongly agree" },
            new Question { text = "This situation felt real to me.",
                           scaleMin = "Strongly disagree", scaleMax = "Strongly agree" },
            new Question { text = "I felt personally at risk during this scenario.",
                           scaleMin = "Strongly disagree", scaleMax = "Strongly agree" },
        };

        // ── Post-scenario: paired condition only ──────────────────────────────

        [Header("Post-scenario — paired condition only (Q8–Q9, Q11)")]
        public Question[] postScenarioPairedOnly = new Question[]
        {
            new Question { text = "My partner influenced my decision.",
                           scaleMin = "Strongly disagree", scaleMax = "Strongly agree" },
            new Question { text = "I was aware of my partner's presence during the decision.",
                           scaleMin = "Strongly disagree", scaleMax = "Strongly agree" },
            new Question { text = "My partner's presence made the situation feel less threatening.",
                           scaleMin = "Strongly disagree", scaleMax = "Strongly agree" },
        };

        // ── ITC-SOPI co-presence: between scenarios 2 and 3, paired only ─────
        // Items adapted from ITC-SOPI (Lessiter et al., 2001) co-presence subscale.
        // Verify wording against the licensed scale before data collection.

        [Header("ITC-SOPI co-presence — between scenarios 2–3, paired only (6 items, 5-pt)")]
        public Question[] itcSopiItems = new Question[]
        {
            new Question { text = "I had a sense of sharing the virtual space with my partner.",
                           scaleMin = "Strongly disagree", scaleMax = "Strongly agree" },
            new Question { text = "I felt as though my partner and I were in the same place.",
                           scaleMin = "Strongly disagree", scaleMax = "Strongly agree" },
            new Question { text = "My partner felt like a real presence in the environment.",
                           scaleMin = "Strongly disagree", scaleMax = "Strongly agree" },
            new Question { text = "I was aware of what my partner was doing during the scenarios.",
                           scaleMin = "Strongly disagree", scaleMax = "Strongly agree" },
            new Question { text = "The feeling of being together with my partner in VR felt natural.",
                           scaleMin = "Strongly disagree", scaleMax = "Strongly agree" },
            new Question { text = "I could tell when my partner was paying attention to the scenario.",
                           scaleMin = "Strongly disagree", scaleMax = "Strongly agree" },
        };

        // ── Closeness item: between scenarios 2 and 3, paired only ───────────

        [Header("Closeness — between scenarios 2–3, paired only (1 item, 7-pt)")]
        public Question[] closenessItem = new Question[]
        {
            new Question { text = "How close do you feel to your partner right now?",
                           type = QuestionType.Likert7,
                           scaleMin = "Not at all close", scaleMax = "Extremely close" },
        };
    }
}
