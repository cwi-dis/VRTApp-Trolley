using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using VRT.Orchestrator.Wrapping;
using VRT.Orchestrator.Responses;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Runs the post-scenario questionnaire. Both paired participants are in this
    /// scene simultaneously but in separate booths (no eye/ear contact).
    ///
    /// Flow: Reflection (15 s think-aloud) -> Common questions -> Paired-only questions
    ///       -> signal done -> wait for partner done (paired) -> load next scene.
    /// </summary>
    public class QuestionnaireController : MonoBehaviour
    {
        [Header("Question Set")]
        [SerializeField] QuestionSet questionSet;

        [Header("Reflection UI")]
        [SerializeField] GameObject reflectionPanel;
        [SerializeField] TextMeshProUGUI reflectionPromptText;
        [SerializeField] TextMeshProUGUI reflectionTimerText;
        [SerializeField] float reflectionDuration = 15f;

        [Header("Question UI")]
        [SerializeField] GameObject questionPanel;
        [SerializeField] TextMeshProUGUI questionBodyText;

        [Header("Likert Buttons (provide 7; last 2 hidden for 5-point scales)")]
        [SerializeField] Button[] likertButtons;
        [SerializeField] TextMeshProUGUI[] likertLabels;

        [Header("End Panel")]
        [SerializeField] GameObject waitingPanel;
        [SerializeField] TextMeshProUGUI waitingText;

        const string DonePrefix = "questionnaire:done:";

        string _completedScenario;
        bool _isPaired;
        bool _remoteDone;

        void Start()
        {
            _completedScenario = TrolleyGameState.Instance?.lastCompletedScenarioID ?? "unknown";
            _isPaired = TrolleyGameState.Instance?.condition == TrolleyGameState.Condition.Paired;

            OrchestratorController.Instance.OnUserMessageReceivedEvent += OnNetworkMessage;
            questionPanel.SetActive(false);
            reflectionPanel.SetActive(false);
            if (waitingPanel != null) waitingPanel.SetActive(false);

            StartCoroutine(RunQuestionnaire());
        }

        void OnDestroy()
        {
            if (OrchestratorController.Instance != null)
                OrchestratorController.Instance.OnUserMessageReceivedEvent -= OnNetworkMessage;
        }

        IEnumerator RunQuestionnaire()
        {
            yield return StartCoroutine(ShowReflection());
            yield return StartCoroutine(ShowQuestions(questionSet.postScenarioCommon, 0));

            int offset = questionSet.postScenarioCommon.Length;
            if (_isPaired)
                yield return StartCoroutine(ShowQuestions(questionSet.postScenarioPairedOnly, offset));

            // Signal done and wait for partner if paired.
            string myId = OrchestratorController.Instance.SelfUser.userId;
            OrchestratorController.Instance.SendMessageToAll($"{DonePrefix}{myId}");

            if (_isPaired)
            {
                if (waitingPanel != null)
                {
                    waitingPanel.SetActive(true);
                    if (waitingText != null) waitingText.text = "Waiting for your partner...";
                }
                yield return new WaitUntil(() => _remoteDone);
                if (waitingPanel != null) waitingPanel.SetActive(false);
            }

            LoadNextScene();
        }

        IEnumerator ShowReflection()
        {
            reflectionPanel.SetActive(true);
            reflectionPromptText.text =
                "Please reflect aloud:\nWhy did you make this decision?\nWhat were you thinking during the scenario?";
            float elapsed = 0f;
            while (elapsed < reflectionDuration)
            {
                elapsed += Time.deltaTime;
                int remaining = Mathf.CeilToInt(reflectionDuration - elapsed);
                if (reflectionTimerText != null) reflectionTimerText.text = remaining.ToString();
                yield return null;
            }
            reflectionPanel.SetActive(false);
        }

        IEnumerator ShowQuestions(QuestionSet.Question[] questions, int indexOffset)
        {
            for (int i = 0; i < questions.Length; i++)
            {
                string answer = null;
                yield return StartCoroutine(ShowSingleQuestion(questions[i], i + indexOffset, a => answer = a));
                DataLogger.Instance.LogQuestionnaireAnswer(
                    _completedScenario, i + indexOffset, questions[i].text, answer);
            }
        }

        IEnumerator ShowSingleQuestion(QuestionSet.Question q, int index, System.Action<string> onAnswer)
        {
            questionPanel.SetActive(true);
            questionBodyText.text = $"Q{index + 1}. {q.text}";
            int numPoints = q.type == QuestionSet.QuestionType.Likert7 ? 7 : 5;
            SetupLikertButtons(numPoints);

            string answer = null;
            for (int i = 0; i < numPoints; i++)
            {
                int captured = i;
                likertButtons[i].onClick.RemoveAllListeners();
                likertButtons[i].onClick.AddListener(() => answer = (captured + 1).ToString());
            }

            yield return new WaitUntil(() => answer != null);
            questionPanel.SetActive(false);
            onAnswer(answer);
        }

        void SetupLikertButtons(int numPoints)
        {
            for (int i = 0; i < likertButtons.Length; i++)
            {
                bool show = i < numPoints;
                likertButtons[i].gameObject.SetActive(show);
                if (show && i < likertLabels.Length)
                    likertLabels[i].text = (i + 1).ToString();
            }
        }

        void LoadNextScene()
        {
            string next;
            if (TrolleyGameState.Instance != null && TrolleyGameState.Instance.HasMoreScenarios())
                next = TrolleyGameState.Instance.NextScenarioScene();
            else
                next = TrolleyGameState.Instance?.endScene ?? "VRTLoginManager";

            SceneManager.LoadScene(next);
        }

        void OnNetworkMessage(UserMessage msg)
        {
            if (msg.message.StartsWith(DonePrefix))
                _remoteDone = true;
        }
    }
}
