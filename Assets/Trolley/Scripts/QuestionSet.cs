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

        // Agency (1–3), Responsibility (4–6), Decision evaluation (7–11), Threat/seriousness (12–14).
        [Header("Post-scenario — all participants")]
        public Question[] postScenarioCommon = new Question[]
        {
            new Question { text = "The decision felt like mine." },
            new Question { text = "I was in control of the decision." },
            new Question { text = "The outcome depended on my actions." },
            new Question { text = "I felt personally responsible for the outcome." },
            new Question { text = "The outcome was a consequence of my decision." },
            new Question { text = "I should be held accountable for what happened." },
            new Question { text = "It was the right decision." },
            new Question { text = "I regret the choice that was made." },
            new Question { text = "I would go for the same choice if I had to do it over again." },
            new Question { text = "The decision did me a lot of harm." },
            new Question { text = "The decision was a wise one." },
            new Question { text = "I felt personally at risk during the scenario." },
            new Question { text = "The situation felt threatening to me." },
            new Question { text = "The consequences of the decision felt serious." },
        };

        // ── Post-scenario: paired condition only ──────────────────────────────

        [Header("Post-scenario — paired condition only")]
        public Question[] postScenarioPairedOnly = new Question[]
        {
            new Question { text = "My partner influenced my decision." },
            new Question { text = "I took my partner's views into account when deciding." },
            new Question { text = "My decision would have been different if my partner had not been present." },
            new Question { text = "I felt pressure to agree with my partner." },
            new Question { text = "I changed my decision because of my partner." },
        };

        // ── Page grouping — questions shown together on one screen, no visible labels ──
        // Sizes are consumed in order against the question arrays above; they should sum to each array's
        // length (common 3+3+5+3 = 14, paired 5). No page titles are shown to the participant (construct
        // names would bias answers). The controller clamps to the available rows and question count, so a
        // shorter set (e.g. the practice set) still paginates safely.
        [Header("Page grouping — questions per page (no titles shown)")]
        public int[] commonPageSizes = { 3, 3, 5, 3 };
        public int[] pairedPageSizes = { 5 };
    }
}
