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
    /// The master client uses Booth A refs; the non-master uses Booth B refs.
    /// In solo condition only Booth A refs are needed.
    ///
    /// Flow: Reflection (15 s think-aloud) -> Common questions -> Paired-only questions
    ///       -> signal done -> wait for partner done (paired) -> load next scene.
    /// </summary>
    public class QuestionnaireController : MonoBehaviour
    {
        [Header("Question Set")]
        [SerializeField] QuestionSet questionSet;

        [Header("Booth A — Master / Solo player")]
        [SerializeField] GameObject reflectionPanelA;
        [SerializeField] TextMeshProUGUI reflectionPromptTextA;
        [SerializeField] TextMeshProUGUI reflectionTimerTextA;
        [SerializeField] GameObject questionPanelA;
        [SerializeField] TextMeshProUGUI questionBodyTextA;
        [SerializeField] Button[] likertButtonsA;
        [SerializeField] TextMeshProUGUI[] likertLabelsA;
        [SerializeField] GameObject waitingPanelA;
        [SerializeField] TextMeshProUGUI waitingTextA;

        [Header("Booth B — Non-master player (paired only)")]
        [SerializeField] GameObject reflectionPanelB;
        [SerializeField] TextMeshProUGUI reflectionPromptTextB;
        [SerializeField] TextMeshProUGUI reflectionTimerTextB;
        [SerializeField] GameObject questionPanelB;
        [SerializeField] TextMeshProUGUI questionBodyTextB;
        [SerializeField] Button[] likertButtonsB;
        [SerializeField] TextMeshProUGUI[] likertLabelsB;
        [SerializeField] GameObject waitingPanelB;
        [SerializeField] TextMeshProUGUI waitingTextB;

        [Header("Timing")]
        [SerializeField] float reflectionDuration = 15f;

        const string DonePrefix = "questionnaire:done:";

        // Working refs resolved at Start() based on master/non-master role.
        GameObject reflectionPanel;
        TextMeshProUGUI reflectionPromptText;
        TextMeshProUGUI reflectionTimerText;
        GameObject questionPanel;
        TextMeshProUGUI questionBodyText;
        Button[] likertButtons;
        TextMeshProUGUI[] likertLabels;
        GameObject waitingPanel;
        TextMeshProUGUI waitingText;

        string _completedScenario;
        bool _isPaired;
        bool _remoteDone;

        void Start()
        {
            _completedScenario = TrolleyGameState.Instance?.lastCompletedScenarioID ?? "unknown";
            _isPaired = TrolleyGameState.Instance?.condition == TrolleyGameState.Condition.Paired;

            bool useBoothA = !_isPaired || OrchestratorController.Instance.UserIsMaster;
            SelectBooth(useBoothA);

            OrchestratorController.Instance.OnUserMessageReceivedEvent += OnNetworkMessage;
            questionPanel.SetActive(false);
            reflectionPanel.SetActive(false);
            if (waitingPanel != null) waitingPanel.SetActive(false);

            StartCoroutine(RunQuestionnaire());
        }

        void SelectBooth(bool useA)
        {
            reflectionPanel      = useA ? reflectionPanelA      : reflectionPanelB;
            reflectionPromptText = useA ? reflectionPromptTextA  : reflectionPromptTextB;
            reflectionTimerText  = useA ? reflectionTimerTextA   : reflectionTimerTextB;
            questionPanel        = useA ? questionPanelA         : questionPanelB;
            questionBodyText     = useA ? questionBodyTextA      : questionBodyTextB;
            likertButtons        = useA ? likertButtonsA         : likertButtonsB;
            likertLabels         = useA ? likertLabelsA          : likertLabelsB;
            waitingPanel         = useA ? waitingPanelA          : waitingPanelB;
            waitingText          = useA ? waitingTextA           : waitingTextB;
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
