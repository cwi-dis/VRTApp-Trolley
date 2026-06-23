using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using TMPro;
using VRT.Orchestrator;
using VRT.OrchestratorComm;
using VRT.Pilots.Common;

namespace VRT.Pilots.Trolley
{
    public class QuestionnaireController : MonoBehaviour
    {
        [Header("Question Set")]
        [SerializeField] QuestionSet questionSet;

        [Header("Practice mode (tutorial run-through — no data logged)")]
        [Tooltip("When set, this is the fake questionnaire after the tutorial drill: uses practiceQuestionSet, " +
                 "a generic reflection prompt, no DataLogger writes, and loads the first real scenario after.")]
        [SerializeField] bool practiceMode = false;
        [SerializeField] QuestionSet practiceQuestionSet;

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

        [Header("Output")]
        [Tooltip("JSON output filename. Supports {session}, {scenario}, {player}, {time} tokens. " +
                 "Resolved via VRTConfig.ConfigFilename so the config-file folder controls output location.")]
        [SerializeField] string outputFilename = "questionnaire_{session}_{scenario}_p{player}.json";

        [Header("Timing")]
        [SerializeField] float reflectionDuration = 60f;

        [Header("Scene Transition")]
        [SerializeField] NetworkTrigger readyTrigger;
        [SerializeField] BarrierController transitionBarrier;
        [SerializeField] NetworkTrigger proceedTrigger;

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
        QuestionnaireRecord _record;


        void Start()
        {
            _completedScenario = TrolleyGameState.Instance?.lastCompletedScenarioID ?? "unknown";
            _lastDecision = TrolleyGameState.Instance?.lastDecision ?? "unknown";
            if (practiceMode) { _completedScenario = "practice"; _lastDecision = "practice"; }
            Debug.Log($"[Questionnaire] Start — scenario={_completedScenario}, decision={_lastDecision}, practice={practiceMode}");
            _isPaired = VRTPilotConfig.InstanceExists() && VRTPilotConfig.Instance.researcherConfig.IsPaired;
            _record = QuestionnaireRecord.Create(_completedScenario, _lastDecision, practiceMode,
                DataLogger.Instance?.SessionID);

            readyTrigger.OnTrigger.AddListener(transitionBarrier.Trigger);
            transitionBarrier.OnAllReady.AddListener(proceedTrigger.Trigger);
            proceedTrigger.OnTrigger.AddListener(ExecuteSceneLoad);

            // Booth A = solo / master, Booth B = non-master. Guard against a paired session
            // with no orchestrator (would NRE) by defaulting to booth A.
            var comm = VRTOrchestratorSingleton.Comm;
            bool isMaster = comm == null || comm.UserIsMaster;
            bool useBoothA = !_isPaired || isMaster;
            reflectionDoneButton = useBoothA ? reflectionDoneButtonA : reflectionDoneButtonB;
            SelectBooth(useBoothA);

            // Diagnostic for the "Player 2 sees a blank booth" issue: confirms which booth this
            // client drives and whether the camera is actually near that booth's canvas. If the
            // distance is large, the player was spawned at the wrong booth (a spawn-placement bug,
            // not a UI bug). Visible in the Quest logcat / Editor console.
            var cam = Camera.main;
            Vector3 camPos = cam != null ? cam.transform.position : Vector3.zero;
            Vector3 panelPos = questionPanel != null ? questionPanel.transform.position : Vector3.zero;
            float dist = cam != null ? Vector3.Distance(camPos, panelPos) : -1f;
            Debug.Log($"[Questionnaire] paired={_isPaired}, isMaster={isMaster}, booth={(useBoothA ? "A" : "B")}, " +
                      $"cameraPos={camPos}, boothPanelPos={panelPos}, distance={dist:F1} " +
                      $"(a large distance means this player spawned at the wrong booth).");

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
            bool inVR = !VRTPilotConfig.InstanceExists() || VRTPilotConfig.Instance.researcherConfig.questionnaireInVR;

            if (inVR)
            {
                yield return StartCoroutine(ShowReflection());

                QuestionSet set = practiceMode && practiceQuestionSet != null ? practiceQuestionSet : questionSet;
                yield return StartCoroutine(ShowQuestions(set.postScenarioCommon, 0));

                // Practice is a short rehearsal — skip the paired-only block.
                if (!practiceMode && _isPaired)
                    yield return StartCoroutine(ShowQuestions(set.postScenarioPairedOnly, set.postScenarioCommon.Length));
            }
            else
            {
                yield return StartCoroutine(ShowPaperQuestionnairePrompt());
            }

            if (!practiceMode)
                _record.Save(outputFilename);

            yield return StartCoroutine(ShowTransitionAndSignal());
        }

        IEnumerator ShowPaperQuestionnairePrompt()
        {
            if (transitionPanel != null) transitionPanel.SetActive(true);
            if (transitionText != null)
                transitionText.text = "You will now fill in a questionnaire (on paper).\n" +
                                      "When you are done and return here, please press the Continue button.";
            if (startButton != null)
            {
                SetButtonLabel(startButton, "Continue");
                startButton.gameObject.SetActive(true);
                bool clicked = false;
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(() => clicked = true);
                yield return new WaitUntil(() => clicked);
                startButton.gameObject.SetActive(false);
            }
            // Leave transitionPanel active — ShowTransitionAndSignal will reuse it immediately.
        }

        void SetButtonLabel(Button btn, string label)
        {
            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = label;
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
                    if (!practiceMode)
                    {
                        FindFirstObjectByType<RecordUserVoice>()?.AddMarker($"reflection_done_{_completedScenario}");
                        _record.MarkReflectionEnded();
                    }
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

            if (practiceMode)
                return "This is a practice run of the questionnaire you will fill in after each round.\n\n" +
                       "Try thinking out loud — say whatever comes to mind — then press Done.";

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
                if (!practiceMode)
                    _record.AddResponse(i + indexOffset, questions[i].text, answer);
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

        IEnumerator ShowTransitionAndSignal()
        {
            if (transitionPanel != null) transitionPanel.SetActive(true);

            bool hasMore = TrolleyGameState.Instance?.HasMoreScenarios() ?? false;

            if (_isPaired)
            {
                if (transitionText != null)
                    transitionText.text = practiceMode
                        ? "The study will begin automatically\nwhen your partner is also ready."
                        : hasMore
                            ? "The next scenario will begin automatically\nwhen your partner is also done."
                            : "You have completed all scenarios.\nPlease wait for your partner.";
                if (startButton != null) startButton.gameObject.SetActive(false);
                readyTrigger.Trigger();
                // Scene loads when barrier fires proceedTrigger → ExecuteSceneLoad on all clients
            }
            else
            {
                if (transitionText != null)
                    transitionText.text = practiceMode
                        ? "The study is about to begin.\n\nPlease prepare yourself."
                        : hasMore
                            ? "The next scenario is about to begin.\n\nPlease prepare yourself."
                            : "You have completed all scenarios.\nThank you for your participation.";
                if (startButton != null)
                {
                    SetButtonLabel(startButton, "Continue");
                    startButton.gameObject.SetActive(true);
                    bool clicked = false;
                    startButton.onClick.RemoveAllListeners();
                    startButton.onClick.AddListener(() => { clicked = true; readyTrigger.Trigger(); });
                    yield return new WaitUntil(() => clicked);
                    startButton.gameObject.SetActive(false);
                }
                else
                {
                    readyTrigger.Trigger();
                }
            }
        }

        void ExecuteSceneLoad()
        {
            string next;
            if (practiceMode)
            {
                next = TrolleyGameState.Instance?.NextScene() ?? "VRTLoginManager";
            }
            else
            {
                next = TrolleyGameState.Instance != null && TrolleyGameState.Instance.HasMoreScenarios()
                    ? TrolleyGameState.Instance.NextScenarioScene()
                    : TrolleyGameState.Instance?.endScene ?? "VRTLoginManager";
            }
            PilotController.Instance.LoadNewScene(next);
        }

    }
}
