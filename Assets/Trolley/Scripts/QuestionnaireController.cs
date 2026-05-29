using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using TMPro;
using VRT.Orchestrator;
using VRT.OrchestratorComm;

namespace VRT.Pilots.Trolley
{
    public class QuestionnaireController : MonoBehaviour
    {
        [Header("Question Set")]
        [SerializeField] QuestionSet questionSet;

        [Header("Reflection — 'Done' button ends the think-aloud period")]
        [FormerlySerializedAs("stopButtonA")]
        [SerializeField] Button reflectionDoneButtonA;
        [FormerlySerializedAs("stopButtonB")]
        [SerializeField] Button reflectionDoneButtonB;

        [Header("Booth A — Master / Solo player")]
        [SerializeField] GameObject reflectionPanelA;
        [SerializeField] TextMeshProUGUI reflectionPromptTextA;
        [SerializeField] TextMeshProUGUI reflectionTimerTextA;
        [SerializeField] GameObject questionPanelA;
        [SerializeField] TextMeshProUGUI questionBodyTextA;
        [SerializeField] Button[] likertButtonsA;
        [SerializeField] TextMeshProUGUI[] likertLabelsA;
        [SerializeField] Button nextButtonA;
        [SerializeField] TextMeshProUGUI scaleMinLabelA;
        [SerializeField] TextMeshProUGUI scaleMaxLabelA;
        [SerializeField] GameObject waitingPanelA;
        [SerializeField] TextMeshProUGUI waitingTextA;
        [SerializeField] GameObject transitionPanelA;
        [SerializeField] TextMeshProUGUI transitionTextA;
        [SerializeField] Button startButtonA;

        [Header("Booth B — Non-master player (paired only)")]
        [SerializeField] GameObject reflectionPanelB;
        [SerializeField] TextMeshProUGUI reflectionPromptTextB;
        [SerializeField] TextMeshProUGUI reflectionTimerTextB;
        [SerializeField] GameObject questionPanelB;
        [SerializeField] TextMeshProUGUI questionBodyTextB;
        [SerializeField] Button[] likertButtonsB;
        [SerializeField] TextMeshProUGUI[] likertLabelsB;
        [SerializeField] Button nextButtonB;
        [SerializeField] TextMeshProUGUI scaleMinLabelB;
        [SerializeField] TextMeshProUGUI scaleMaxLabelB;
        [SerializeField] GameObject waitingPanelB;
        [SerializeField] TextMeshProUGUI waitingTextB;
        [SerializeField] GameObject transitionPanelB;
        [SerializeField] TextMeshProUGUI transitionTextB;
        [SerializeField] Button startButtonB;

        [Header("Timing")]
        [SerializeField] float reflectionDuration = 60f;

        static readonly Color DefaultBtnColor  = new Color(0.2f, 0.2f, 0.8f);
        static readonly Color SelectedBtnColor = new Color(0.1f, 0.6f, 0.1f);

        // Working refs resolved at Start() based on master/non-master role.
        Button reflectionDoneButton;
        GameObject reflectionPanel;
        TextMeshProUGUI reflectionPromptText, reflectionTimerText;
        GameObject questionPanel;
        TextMeshProUGUI questionBodyText;
        Button[] likertButtons;
        TextMeshProUGUI[] likertLabels;
        Button nextButton;
        TextMeshProUGUI scaleMinLabel, scaleMaxLabel;
        GameObject waitingPanel;
        TextMeshProUGUI waitingText;
        GameObject transitionPanel;
        TextMeshProUGUI transitionText;
        Button startButton;

        string _completedScenario;
        string _lastDecision;
        bool _isPaired;
        bool _remoteDone;

        void Awake()
        {
            VRTOrchestratorSingleton.Comm.RegisterEventType((MessageTypeID)TrolleyMsgID.QuestDone, typeof(TrolleyQuestionnaireDoneMessage));
        }

        void OnEnable()
        {
            VRTOrchestratorSingleton.Comm.Subscribe<TrolleyQuestionnaireDoneMessage>(OnQuestionnaireDone);
        }

        void OnDisable()
        {
            VRTOrchestratorSingleton.Comm?.Unsubscribe<TrolleyQuestionnaireDoneMessage>(OnQuestionnaireDone);
        }

        void Start()
        {
            _completedScenario = TrolleyGameState.Instance?.lastCompletedScenarioID ?? "unknown";
            _lastDecision = TrolleyGameState.Instance?.lastDecision ?? "unknown";
            Debug.Log($"[Questionnaire] Start — scenario={_completedScenario}, decision={_lastDecision}");
            _isPaired = TrolleyGameState.Instance?.condition == TrolleyGameState.Condition.Paired;

            bool useBoothA = !_isPaired || VRTOrchestratorSingleton.Comm.UserIsMaster;
            reflectionDoneButton = useBoothA ? reflectionDoneButtonA : reflectionDoneButtonB;
            SelectBooth(useBoothA);

            questionPanel.SetActive(false);
            reflectionPanel.SetActive(false);
            if (waitingPanel != null) waitingPanel.SetActive(false);
            if (transitionPanel != null) transitionPanel.SetActive(false);

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
            nextButton           = useA ? nextButtonA            : nextButtonB;
            scaleMinLabel        = useA ? scaleMinLabelA         : scaleMinLabelB;
            scaleMaxLabel        = useA ? scaleMaxLabelA         : scaleMaxLabelB;
            waitingPanel         = useA ? waitingPanelA          : waitingPanelB;
            waitingText          = useA ? waitingTextA           : waitingTextB;
            transitionPanel      = useA ? transitionPanelA       : transitionPanelB;
            transitionText       = useA ? transitionTextA        : transitionTextB;
            startButton          = useA ? startButtonA           : startButtonB;
        }

        IEnumerator RunQuestionnaire()
        {
            yield return StartCoroutine(ShowReflection());
            yield return StartCoroutine(ShowQuestions(questionSet.postScenarioCommon, 0));

            int offset = questionSet.postScenarioCommon.Length;
            if (_isPaired)
                yield return StartCoroutine(ShowQuestions(questionSet.postScenarioPairedOnly, offset));

            var doneMsg = new TrolleyQuestionnaireDoneMessage();
            if (VRTOrchestratorSingleton.Comm.UserIsMaster)
                VRTOrchestratorSingleton.Comm.SendTypeEventToAll(doneMsg);
            else
                VRTOrchestratorSingleton.Comm.SendTypeEventToMaster(doneMsg);
            yield return StartCoroutine(ShowTransition());

            LoadNextScene();
        }

        IEnumerator ShowReflection()
        {
            reflectionPanel.SetActive(true);
            string prompt = BuildConsequenceText(_completedScenario, _lastDecision);
            if (_isPaired) prompt += "\nDid another person affect your decision? If so, how?";
            reflectionPromptText.text = prompt;
            if (reflectionTimerText != null) reflectionTimerText.text = "";

            if (reflectionDoneButton != null)
            {
                reflectionDoneButton.gameObject.SetActive(true);
                bool done = false;
                reflectionDoneButton.onClick.RemoveAllListeners();
                reflectionDoneButton.onClick.AddListener(() =>
                {
                    // TODO: emit AddMarker("reflection_done") on RecordUserVoice when implemented
                    DataLogger.Instance?.LogReflection(_completedScenario, _lastDecision, "");
                    done = true;
                });
                yield return new WaitUntil(() => done);
                reflectionDoneButton.gameObject.SetActive(false);
            }
            else
            {
                // Fallback: auto-timer (no button assigned)
                float elapsed = 0f;
                while (elapsed < reflectionDuration)
                {
                    elapsed += Time.deltaTime;
                    if (reflectionTimerText != null)
                        reflectionTimerText.text = Mathf.CeilToInt(reflectionDuration - elapsed).ToString();
                    yield return null;
                }
            }

            reflectionPanel.SetActive(false);
        }

        string BuildConsequenceText(string scenarioID, string decision)
        {
            const string prompt = "Please think out loud: what was going through your mind? What did you decide, and why?";

            if (scenarioID == "selfharm")
            {
                return decision == "action"
                    ? $"You diverted the train into the cliff, saving the five workers. The impact put your own safety at risk.\n\n{prompt}"
                    : $"You did not divert the train, and it continued toward the five workers.\n\n{prompt}";
            }

            return decision == "action"
                ? $"You pressed the button, diverting the train and resulting in one person being harmed.\n\n{prompt}"
                : $"You did not press the button, and the train continued toward the five workers.\n\n{prompt}";
        }

        IEnumerator ShowQuestions(QuestionSet.Question[] questions, int indexOffset,
                                  string scenarioOverride = null)
        {
            string scenario = scenarioOverride ?? _completedScenario;
            for (int i = 0; i < questions.Length; i++)
            {
                string answer = null;
                yield return StartCoroutine(ShowSingleQuestion(questions[i], i + indexOffset, a => answer = a));
                DataLogger.Instance?.LogQuestionnaireAnswer(
                    scenario, i + indexOffset, questions[i].text, answer);
            }
        }

        IEnumerator ShowSingleQuestion(QuestionSet.Question q, int index, System.Action<string> onAnswer)
        {
            questionPanel.SetActive(true);
            questionBodyText.text = $"Q{index + 1}. {q.text}";
            int numPoints = q.type == QuestionSet.QuestionType.Likert7 ? 7 : 5;
            SetupLikertButtons(numPoints, q.scaleMin, q.scaleMax);

            if (nextButton != null)
            {
                nextButton.interactable = false;
                nextButton.gameObject.SetActive(true);
            }

            string answer = null;

            for (int i = 0; i < numPoints; i++)
            {
                int captured = i;
                likertButtons[i].onClick.RemoveAllListeners();
                likertButtons[i].onClick.AddListener(() =>
                {
                    answer = (captured + 1).ToString();
                    for (int j = 0; j < numPoints; j++)
                    {
                        var img = likertButtons[j].GetComponent<Image>();
                        if (img != null)
                            img.color = j == captured ? SelectedBtnColor : DefaultBtnColor;
                    }
                    if (nextButton != null) nextButton.interactable = true;
                });
            }

            if (nextButton != null)
            {
                bool nextClicked = false;
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(() => nextClicked = true);
                yield return new WaitUntil(() => nextClicked);
                nextButton.gameObject.SetActive(false);
            }
            else
            {
                yield return new WaitUntil(() => answer != null);
            }

            for (int i = 0; i < numPoints; i++)
            {
                var img = likertButtons[i].GetComponent<Image>();
                if (img != null) img.color = DefaultBtnColor;
            }

            questionPanel.SetActive(false);
            onAnswer(answer);
        }

        void SetupLikertButtons(int numPoints, string scaleMin, string scaleMax)
        {
            for (int i = 0; i < likertButtons.Length; i++)
            {
                bool show = i < numPoints;
                likertButtons[i].gameObject.SetActive(show);
                if (show && i < likertLabels.Length)
                    likertLabels[i].text = (i + 1).ToString();
                var img = likertButtons[i].GetComponent<Image>();
                if (img != null) img.color = DefaultBtnColor;
            }
            if (scaleMinLabel != null) scaleMinLabel.text = scaleMin;
            if (scaleMaxLabel != null) scaleMaxLabel.text = scaleMax;
        }

        IEnumerator ShowTransition()
        {
            if (transitionPanel == null)
            {
                if (_isPaired) yield return new WaitUntil(() => _remoteDone);
                yield break;
            }

            transitionPanel.SetActive(true);

            bool hasMore = TrolleyGameState.Instance != null && TrolleyGameState.Instance.HasMoreScenarios();

            if (_isPaired)
            {
                if (transitionText != null)
                {
                    transitionText.text = hasMore
                        ? "The next scenario will begin automatically\nwhen your partner is also done."
                        : "You have completed all scenarios.\nPlease wait for your partner.";
                }
                if (startButton != null) startButton.gameObject.SetActive(false);
                yield return new WaitUntil(() => _remoteDone);
            }
            else
            {
                if (transitionText != null)
                {
                    transitionText.text = hasMore
                        ? "The next scenario is about to begin.\n\nPlease prepare yourself."
                        : "You have completed all scenarios.\nThank you for your participation.";
                }
                if (startButton != null)
                {
                    startButton.gameObject.SetActive(true);
                    bool started = false;
                    startButton.onClick.RemoveAllListeners();
                    startButton.onClick.AddListener(() => started = true);
                    yield return new WaitUntil(() => started);
                }
            }

            transitionPanel.SetActive(false);
        }

        void LoadNextScene()
        {
            string next;
            if (TrolleyGameState.Instance != null && TrolleyGameState.Instance.HasMoreScenarios())
                next = TrolleyGameState.Instance.NextScenarioScene();
            else
                next = TrolleyGameState.Instance?.endScene ?? "VRTLoginManager";
            if (SceneFader.Instance == null)
                new GameObject("SceneFader").AddComponent<SceneFader>();
            SceneFader.Instance.FadeToBlack(() => SceneManager.LoadScene(next));
        }

        void OnQuestionnaireDone(TrolleyQuestionnaireDoneMessage msg)
        {
            if (VRTOrchestratorSingleton.Comm.UserIsMaster)
                VRTOrchestratorSingleton.Comm.SendTypeEventToAll(msg, true);
            if (msg.SenderId == VRTOrchestratorSingleton.Comm.SelfUser?.userId) return;
            _remoteDone = true;
        }

    }
}
