using System.Collections;
using System.Collections.Generic;
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
        // One row on a question page: the question text plus its Likert buttons. A page shows several of
        // these at once; unused rows (when a page has fewer questions than rows) are hidden via 'root'.
        [System.Serializable]
        public class QuestionRow
        {
            [Tooltip("Row container — hidden when a page uses fewer than all rows.")]
            public GameObject root;
            public TextMeshProUGUI text;       // question text
            public Button[] buttons;           // up to 7 Likert buttons
            public TextMeshProUGUI[] labels;   // numeric labels on the buttons (optional)
        }

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
        [SerializeField] TextMeshProUGUI reflectionPromptTextA;      // "What happened" — the consequence
        [SerializeField] TextMeshProUGUI reflectionInstructionTextA; // accent box — the question to answer aloud
        [SerializeField] TextMeshProUGUI reflectionTimerTextA;
        [SerializeField] GameObject questionPanelA;
        [SerializeField] QuestionRow[] questionRowsA;
        [SerializeField] Button nextButtonA;
        [SerializeField] RectTransform progressFillA;
        [SerializeField] TextMeshProUGUI scaleMinLabelA;
        [SerializeField] TextMeshProUGUI scaleMaxLabelA;
        [SerializeField] GameObject waitingPanelA;
        [SerializeField] TextMeshProUGUI waitingTextA;
        [SerializeField] GameObject transitionPanelA;
        [SerializeField] TextMeshProUGUI transitionTextA;
        [SerializeField] Button startButtonA;

        [Header("Booth B — Non-master player (paired only)")]
        [SerializeField] GameObject reflectionPanelB;
        [SerializeField] TextMeshProUGUI reflectionPromptTextB;      // "What happened" — the consequence
        [SerializeField] TextMeshProUGUI reflectionInstructionTextB; // accent box — the question to answer aloud
        [SerializeField] TextMeshProUGUI reflectionTimerTextB;
        [SerializeField] GameObject questionPanelB;
        [SerializeField] QuestionRow[] questionRowsB;
        [SerializeField] Button nextButtonB;
        [SerializeField] RectTransform progressFillB;
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

        // Neutral, no red/green gradient: an empty (light) circle vs a filled (dark) selected circle.
        static readonly Color DefaultBtnColor  = new Color(0.78f, 0.78f, 0.78f);
        static readonly Color SelectedBtnColor = new Color(0.20f, 0.20f, 0.20f);

        // Working refs resolved at Start() based on master/non-master role.
        Button reflectionDoneButton;
        GameObject reflectionPanel;
        TextMeshProUGUI reflectionPromptText, reflectionInstructionText, reflectionTimerText;
        GameObject questionPanel;
        QuestionRow[] questionRows;
        Button nextButton;
        RectTransform progressFill;
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
            reflectionInstructionText = useA ? reflectionInstructionTextA : reflectionInstructionTextB;
            reflectionTimerText  = useA ? reflectionTimerTextA   : reflectionTimerTextB;
            questionPanel        = useA ? questionPanelA         : questionPanelB;
            questionRows         = useA ? questionRowsA          : questionRowsB;
            nextButton           = useA ? nextButtonA            : nextButtonB;
            progressFill         = useA ? progressFillA          : progressFillB;
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

            // Reflection (think-aloud) always runs — in VR and paper-form sessions alike.
            yield return StartCoroutine(ShowReflection());

            if (inVR)
            {
                QuestionSet set = practiceMode && practiceQuestionSet != null ? practiceQuestionSet : questionSet;

                // Build the page list up front so the progress bar knows the total (solo = common pages,
                // paired = common + partner pages — e.g. 4 or 5).
                var commonPages = new List<PageSpec>(Paginate(set.postScenarioCommon.Length, set.commonPageSizes));
                var pairedPages = (!practiceMode && _isPaired)
                    ? new List<PageSpec>(Paginate(set.postScenarioPairedOnly.Length, set.pairedPageSizes))
                    : new List<PageSpec>();
                int totalPages = commonPages.Count + pairedPages.Count;
                int pageNum = 0;

                // Common questions, grouped into pages (e.g. 3/3/5/3) shown together on one screen each.
                foreach (var page in commonPages)
                {
                    UpdateProgress(++pageNum, totalPages);
                    yield return StartCoroutine(ShowQuestionPage(set.postScenarioCommon, page.start, page.count, 0));
                }

                // Paired-only block (practice skips it).
                foreach (var page in pairedPages)
                {
                    UpdateProgress(++pageNum, totalPages);
                    yield return StartCoroutine(ShowQuestionPage(set.postScenarioPairedOnly, page.start, page.count,
                                                                 set.postScenarioCommon.Length));
                }
            }
            else if (!practiceMode)
            {
                // Paper form: ask participant to remove HMD and fill in the form, then continue.
                yield return StartCoroutine(ShowPaperQuestionnairePrompt());
            }
            // else: !inVR + practiceMode → reflection is the only exercise; no Likert, no paper prompt.

            if (!practiceMode)
                _record.Save(outputFilename);

            yield return StartCoroutine(ShowTransitionAndSignal());
        }

        IEnumerator ShowPaperQuestionnairePrompt()
        {
            if (transitionPanel != null) transitionPanel.SetActive(true);
            if (transitionText != null)
                transitionText.text = "Please take off the headset and fill in the questionnaire on paper.\n" +
                                      "When you're done, put the headset back on and press Continue.";
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
            // "What happened" — the consequence of their choice.
            reflectionPromptText.text = BuildConsequenceText(_completedScenario, _lastDecision);
            // The highlighted box — what to say aloud; append the partner question in paired sessions.
            if (reflectionInstructionText != null)
            {
                string instruction = "Why did you make that choice?";
                if (_isPaired) instruction += "\nDid the other person affect your decision?";
                reflectionInstructionText.text = instruction;
            }
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

        // The "What happened" line only — the consequence of their choice. The instruction to speak
        // aloud is now static in the reflection panel (see TrolleyQuestionnaireSetup), so it's no
        // longer appended here.
        string BuildConsequenceText(string scenarioID, string decision)
        {
            if (practiceMode)
                return "This is a practice of the reflection you'll do after every round. When you're ready, answer out loud — then press I've answered.";

            if (scenarioID == "selfharm")
            {
                return decision == "action"
                    ? "You steered the train into the rocks. The five workers are safe, but you put yourself in danger."
                    : "You let the train continue, and it hit the five workers.";
            }

            return decision == "action"
                ? "You diverted the train to the side track. The five workers are safe, but one worker was hit."
                : "You let the train continue, and it hit the five workers.";
        }

        struct PageSpec { public int start; public int count; }

        // Split `total` questions into pages by the configured sizes, clamped to the available UI rows and
        // the actual question count — so an exact-sum list (3/3/5/3 = 14), a shorter set (the practice set),
        // or a size that overflows the rows all paginate safely.
        IEnumerable<PageSpec> Paginate(int total, int[] sizes)
        {
            int maxRows = questionRows != null ? questionRows.Length : 0;
            if (maxRows <= 0) maxRows = total;          // no rows wired: fall back to one page
            int start = 0, p = 0;
            while (start < total)
            {
                int count = (sizes != null && p < sizes.Length) ? sizes[p] : (total - start);
                count = Mathf.Min(count, total - start);
                count = Mathf.Min(count, maxRows);      // never exceed the rows the panel actually has
                if (count <= 0) break;
                yield return new PageSpec { start = start, count = count };
                start += count;
                p++;
            }
        }

        // Show one page: questions[start .. start+count], each on its own row, all visible at once. Next
        // enables only when every shown question is answered. Records each response at its global index
        // (baseOffset + start + r) so the JSON numbering is unchanged from the one-per-screen version.
        IEnumerator ShowQuestionPage(QuestionSet.Question[] questions, int start, int count, int baseOffset)
        {
            questionPanel.SetActive(true);

            // The 7 point words are a fixed column header (built by the setup script), so there's nothing
            // per-page to set here — every question shares the same agree/disagree scale.
            var answers = new string[count];
            int rows = questionRows != null ? questionRows.Length : 0;

            for (int r = 0; r < rows; r++)
            {
                var row = questionRows[r];
                bool used = r < count;
                if (row != null && row.root != null) row.root.SetActive(used);
                if (!used || row == null) continue;

                var q = questions[start + r];
                int globalIndex = baseOffset + start + r;
                if (row.text != null) row.text.text = $"Q{globalIndex + 1}. {q.text}";

                int numPoints = q.type == QuestionSet.QuestionType.Likert7 ? 7 : 5;
                SetupRowButtons(row, numPoints);

                int rr = r;
                int np = numPoints;
                for (int b = 0; b < row.buttons.Length; b++)
                {
                    if (b >= np || row.buttons[b] == null) continue;
                    int captured = b;
                    row.buttons[b].onClick.RemoveAllListeners();
                    row.buttons[b].onClick.AddListener(() =>
                    {
                        answers[rr] = (captured + 1).ToString();
                        for (int j = 0; j < np; j++)
                        {
                            var img = row.buttons[j] != null ? row.buttons[j].GetComponent<Image>() : null;
                            if (img != null) img.color = j == captured ? SelectedBtnColor : DefaultBtnColor;
                        }
                        if (nextButton != null) nextButton.interactable = AllAnswered(answers);
                    });
                }
            }

            if (nextButton != null)
            {
                nextButton.interactable = false;
                nextButton.gameObject.SetActive(true);
                bool nextClicked = false;
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(() => nextClicked = true);
                yield return new WaitUntil(() => nextClicked);
                nextButton.gameObject.SetActive(false);
            }
            else
            {
                yield return new WaitUntil(() => AllAnswered(answers));
            }

            questionPanel.SetActive(false);

            if (!practiceMode)
                for (int r = 0; r < count; r++)
                    _record.AddResponse(baseOffset + start + r, questions[start + r].text, answers[r]);
        }

        static bool AllAnswered(string[] answers)
        {
            foreach (var a in answers) if (string.IsNullOrEmpty(a)) return false;
            return true;
        }

        // Fill the bottom progress bar to pageNum / totalPages (page 1 of 4 → 1/4 filled).
        void UpdateProgress(int pageNum, int totalPages)
        {
            if (progressFill == null || totalPages <= 0) return;
            float frac = Mathf.Clamp01((float)pageNum / totalPages);
            progressFill.anchorMin = new Vector2(0f, 0f);
            progressFill.anchorMax = new Vector2(frac, 1f);
            progressFill.offsetMin = progressFill.offsetMax = Vector2.zero;
        }

        // Show `numPoints` circles on a row (reset to the empty colour) and hide the rest. The point words
        // live in the shared column header, so the circles themselves carry no labels.
        void SetupRowButtons(QuestionRow row, int numPoints)
        {
            for (int i = 0; i < row.buttons.Length; i++)
            {
                if (row.buttons[i] == null) continue;
                row.buttons[i].gameObject.SetActive(i < numPoints);
                var img = row.buttons[i].GetComponent<Image>();
                if (img != null) img.color = DefaultBtnColor;
            }
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
