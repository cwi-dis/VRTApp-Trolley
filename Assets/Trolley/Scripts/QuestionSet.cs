using System;
using UnityEngine;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// ScriptableObject defining all questionnaire questions.
    /// Create via Assets > Create > Trolley > Question Set.
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
            public string scaleMin = "Not at all";
            public string scaleMax = "Very much";
        }

        [Header("Post-scenario — shown after every scenario (solo and paired)")]
        public Question[] postScenarioCommon = new Question[]
        {
            new Question { text = "I found the decision difficult to make.", type = QuestionType.Likert5 },
            new Question { text = "I feel confident about the decision I made.", type = QuestionType.Likert5 },
            new Question { text = "I felt emotionally affected by the scenario.", type = QuestionType.Likert5 },
        };

        [Header("Post-scenario — paired condition only")]
        public Question[] postScenarioPairedOnly = new Question[]
        {
            new Question { text = "I am satisfied with the joint decision we made.", type = QuestionType.Likert5 },
            new Question { text = "My partner influenced my decision.", type = QuestionType.Likert5 },
            new Question { text = "I felt pressure from my partner during the decision.", type = QuestionType.Likert5 },
        };

        [Header("Overall — shown once at the end of all scenarios")]
        public Question[] overallQuestions = new Question[]
        {
            new Question { text = "Overall, I felt present in the virtual environment.", type = QuestionType.Likert7 },
            new Question { text = "The experience felt realistic to me.", type = QuestionType.Likert7 },
        };
    }
}
