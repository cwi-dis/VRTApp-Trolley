using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using VRT.Orchestrator;
using VRT.OrchestratorComm;

namespace VRT.Pilots.Trolley
{
    public class QuestionnaireController : MonoBehaviour
    {
        [Header("Question Set")]
        [SerializeField] QuestionSet questionSet;

        [Header("Recording (optional — falls back to 15s timer if unassigned)")]
        [SerializeField] Button recordButtonA;
        [SerializeField] Button stopButtonA;
        [SerializeField] Button recordButtonB;
        [SerializeField] Button stopButtonB;

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
        Button recordButton, stopButton;
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

        // Recording state
        string _micDevice;
        AudioClip _recording;

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
            _isPaired = TrolleyGameState.Instance?.condition == TrolleyGameState.Condition.Paired;

            bool useBoothA = !_isPaired || VRTOrchestratorSingleton.Comm.UserIsMaster;
            recordButton = useBoothA ? recordButtonA : recordButtonB;
            stopButton   = useBoothA ? stopButtonA   : stopButtonB;
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
            {
                yield return StartCoroutine(ShowQuestions(questionSet.postScenarioPairedOnly, offset));
                offset += questionSet.postScenarioPairedOnly.Length;
            }

            // Q10: threat perception — self-harm scenario only
            if (_completedScenario == "selfharm")
                yield return StartCoroutine(ShowQuestions(questionSet.postScenarioSelfHarmOnly, offset));

            // ITC-SOPI co-presence + closeness — paired, after all 3 scenarios
            if (_isPaired && !TrolleyGameState.Instance.HasMoreScenarios())
                yield return StartCoroutine(ShowITCSOPI());

            var doneMsg = new TrolleyQuestionnaireDoneMessage();
            if (VRTOrchestratorSingleton.Comm.UserIsMaster)
                VRTOrchestratorSingleton.Comm.SendTypeEventToAll(doneMsg);
            else
                VRTOrchestratorSingleton.Comm.SendTypeEventToMaster(doneMsg);
            yield return StartCoroutine(ShowTransition());

            LoadNextScene();
        }

        IEnumerator ShowITCSOPI()
        {
            // Index offset 100 avoids collision with per-scenario question indices in the log.
            yield return StartCoroutine(ShowQuestions(questionSet.itcSopiItems, 100, "itcsopi"));
            if (questionSet.closenessItem != null && questionSet.closenessItem.Length > 0)
                yield return StartCoroutine(ShowQuestions(questionSet.closenessItem, 110, "itcsopi"));
        }

        IEnumerator ShowReflection()
        {
            reflectionPanel.SetActive(true);
            string prompt = BuildConsequenceText(_completedScenario, _lastDecision);
            if (_isPaired) prompt += "\n\nDid the other person affect your decision? If so, how?";
            reflectionPromptText.text = prompt;

            if (recordButton != null)
            {
                // Recording flow: user presses Record, then Stop
                recordButton.gameObject.SetActive(true);
                if (stopButton != null) stopButton.gameObject.SetActive(false);
                if (reflectionTimerText != null) reflectionTimerText.text = "";

                bool done = false;

                recordButton.onClick.RemoveAllListeners();
                recordButton.onClick.AddListener(() =>
                {
                    StartVoiceRecording();
                    recordButton.gameObject.SetActive(false);
                    if (stopButton != null) stopButton.gameObject.SetActive(true);
                });

                if (stopButton != null)
                {
                    stopButton.onClick.RemoveAllListeners();
                    stopButton.onClick.AddListener(() =>
                    {
                        StopVoiceRecording();
                        stopButton.gameObject.SetActive(false);
                        done = true;
                    });
                }

                yield return new WaitUntil(() => done);
            }
            else
            {
                // Fallback: auto-timer
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
            const string prompt = "In a few sentences — what was going through your mind? What did you decide and why?";

            if (scenarioID == "selfharm")
            {
                return decision == "action"
                    ? $"You diverted the train into the cliff, saving the five workers. The impact put your own safety at risk.\n\n{prompt}"
                    : $"You did not divert the train, and it continued toward the five workers.\n\n{prompt}";
            }

            string control = scenarioID == "driver" ? "button" : "lever";
            return decision == "action"
                ? $"You pressed the {control}, diverting the train and resulting in one person being harmed.\n\n{prompt}"
                : $"You did not press the {control}, and the train continued toward the five workers.\n\n{prompt}";
        }

        void StartVoiceRecording()
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogWarning("QuestionnaireController: no microphone found.");
                return;
            }
            _micDevice = Microphone.devices[0];
            _recording = Microphone.Start(_micDevice, false, 120, 44100);
            if (reflectionTimerText != null) reflectionTimerText.text = "● REC";
        }

        void StopVoiceRecording()
        {
            if (string.IsNullOrEmpty(_micDevice)) return;
            int pos = Microphone.GetPosition(_micDevice);
            Microphone.End(_micDevice);

            if (_recording != null && pos > 0)
            {
                string filename = $"{_completedScenario}_reflection_{DateTime.Now:yyyyMMdd_HHmmss}.wav";
                string path = Path.Combine(Application.persistentDataPath, filename);
                WavUtility.Save(path, _recording, pos);
                DataLogger.Instance?.LogReflection(_completedScenario, _lastDecision, filename);
                Debug.Log($"Reflection saved: {path}");
                if (reflectionTimerText != null) reflectionTimerText.text = "Saved";
            }

            _micDevice = null;
            _recording = null;
        }

        IEnumerator ShowQuestions(QuestionSet.Question[] questions, int indexOffset,
                                  string scenarioOverride = null)
        {
            string scenario = scenarioOverride ?? _completedScenario;
            for (int i = 0; i < questions.Length; i++)
            {
                string answer = null;
                yield return StartCoroutine(ShowSingleQuestion(questions[i], i + indexOffset, a => answer = a));
                DataLogger.Instance.LogQuestionnaireAnswer(
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
