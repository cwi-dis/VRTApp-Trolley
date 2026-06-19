using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using TMPro;
using VRT.Pilots.Common;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Standalone two-round practice for the DRIVER Tutorial scene — the first-person counterpart of
    /// TutorialTrainDrill. Completely separate from TrolleyController/DriverTrainController so it can't
    /// affect the real scenario flow.
    ///
    /// The player sits in the cab; the whole environment (TrackEnvironment) slides toward them, exactly
    /// like the real Driver scene. A divert yaws the environment about the player's seat (DivertMarker).
    ///
    /// ROUND 1 — button familiarisation (guided):
    ///   • Intro narration (you're the driver, the two buttons, the signal ahead).
    ///   • "Press the right button to divert" → wait for the real B press; "now the left button" → A press.
    ///     Buttons aren't blinked — they use their real-scene feedback (colour on click).
    ///
    /// ROUND 2 — signal drill:
    ///   • A signal light ahead turns RED or BLUE each round: BLUE → press right (divert), RED → do nothing.
    ///   • The tram switches tracks AT the diverting point (after sliding 'approachDistance'); the
    ///     correct/wrong sound plays there, once the move is committed. Fixed order, 3 reps.
    ///   • Between reps the screen fades to black to hide the world snapping back to its start pose
    ///     (a hard teleport reads as a glitch / is uncomfortable in first-person VR).
    ///   • After the drill, the practice questionnaire loads.
    ///
    /// Reuses the Driver rail/cab/movement + the A/B TrolleyToggleDecision, all wired by
    /// TrolleyDriverTutorialSetup. Touches no shared controller.
    /// </summary>
    public class TutorialDriverDrill : MonoBehaviour
    {
        [Header("Movement (environment slides toward the seated player, like the real Driver scene)")]
        [Tooltip("TrackEnvironment — the root that moves while the player stays put.")]
        [SerializeField] Transform environment;
        [SerializeField] Vector3 approachDirection = Vector3.back;
        [SerializeField] float approachSpeed = 9.5f;
        [Tooltip("World distance the environment slides before reaching the diverting point (the switch).")]
        [SerializeField] float approachDistance = 60f;
        [Tooltip("Extra distance the environment keeps moving past the switch before the round ends.")]
        [SerializeField] float postForkDistance = 25f;

        [Header("Divert (yaw the environment about the player's seat — mirrors DriverTrainController)")]
        [SerializeField] Transform divertPivot;          // DivertMarker
        [SerializeField] float branchTurnAngle = -90f;
        [SerializeField] float branchRadius = 95f;

        [Header("Input")]
        [Tooltip("Reused A/B toggle. The drill reads IsAction (right = divert) and resets it each round.")]
        [SerializeField] TrolleyToggleDecision toggle;

        [Header("Signal light ahead (red = stay, blue = divert)")]
        [SerializeField] GameObject signalLight;
        [SerializeField] Color redColor  = new Color(0.85f, 0.15f, 0.15f);
        [SerializeField] Color blueColor = new Color(0.15f, 0.30f, 0.90f);

        [Header("Narration — Round 1 (one clip per step; ~2s pause added after each)")]
        [SerializeField] AudioSource narrationSource;
        [Tooltip("Preamble — 'now you're the driver, in the cab'.")]
        [SerializeField] AudioClip introClip;            // narration_tutorial_driver_intro
        [Tooltip("'two buttons — left keeps to the main track, right diverts'.")]
        [SerializeField] AudioClip buttonsClip;          // narration_tutorial_driver_buttons
        [Tooltip("'watch the signal ahead — blue divert, red stay'. Signal light blinks while this plays.")]
        [SerializeField] AudioClip signalClip;           // narration_tutorial_driver_signal
        [SerializeField] AudioClip pressClip;            // ..._button_main: 'press the right button to divert'
        [SerializeField] AudioClip backClip;             // ..._button_side: 'press the left button to come back'
        [SerializeField] AudioClip confirmClip;          // ..._button_confirm

        [Header("Narration — Round 2 + end")]
        [SerializeField] AudioClip sortClip;             // ..._sortingtrain: 'now let's practise…'
        [Tooltip("Closing line after 5 reps, before the next scene. e.g. 'tutorials done — the study begins'.")]
        [SerializeField] AudioClip closingClip;

        [Header("Timing")]
        [SerializeField] float startDelay = 5f;
        [Tooltip("Silent pause after every narration clip so the recordings don't run together.")]
        [SerializeField] float betweenClipsPause = 2f;
        [SerializeField] float blinkInterval = 0.4f;
        [Tooltip("Gap between reps: the round ends, then this pause before the next.")]
        [SerializeField] float interRoundDelay = 4f;

        [Header("Feedback")]
        [SerializeField] TextMeshProUGUI scoreText;
        [SerializeField] AudioSource sfxSource;
        [SerializeField] AudioClip correctClip;
        [SerializeField] AudioClip wrongClip;

        [Header("After the drill")]
        [Tooltip("Scene to load when the drill ends. Driver tutorial → the practice questionnaire. " +
                 "Empty = skip straight to the first real scenario.")]
        [SerializeField] string nextSceneAfterDrill = "TrolleyPracticeQuestionnaire";

        // Fixed, predetermined order (practice, not data). true = BLUE (divert) · false = RED (stay).
        // 3 reps: BLUE, RED, BLUE — diverting is the skill that needs the most practice.
        static readonly bool[] Sequence = { true, false, true };

        Vector3 _envStartPos;
        Quaternion _envStartRot;
        int _correct;
        int _total;

        void Start()
        {
            _total = Sequence.Length;
            UpdateScore();
            if (scoreText != null) scoreText.gameObject.SetActive(false); // counter is for Round 2 only
            SetActiveSafe(signalLight, false);

            if (environment == null || toggle == null)
            {
                Debug.LogError("[TutorialDriverDrill] environment / toggle not wired — tutorial cannot run.");
                return;
            }
            _envStartPos = environment.localPosition;
            _envStartRot = environment.localRotation;

            // Ensure a fader exists so the between-rep reset can be hidden. The real flow already has
            // one (DontDestroyOnLoad singleton from an earlier scene); this covers standalone testing.
            if (SceneFader.Instance == null)
                new GameObject("SceneFader").AddComponent<SceneFader>();

            toggle.SetInteractionEnabled(false);
            StartCoroutine(RunTutorial());
        }

        IEnumerator RunTutorial()
        {
            yield return null;                 // let the toggle's Start() run first
            toggle.ApplyRemoteState(false);
            toggle.SetInteractionEnabled(false);
            yield return new WaitForSeconds(startDelay);

            // ── Round 1 — intro + button familiarisation ──────────────────────
            yield return StartCoroutine(PlayAndWait(introClip));
            yield return StartCoroutine(PlayAndWait(buttonsClip));
            yield return StartCoroutine(PlayClipWhileBlinkingSignal(signalClip));
            yield return StartCoroutine(RunButtonPractice());

            // ── Round 2 — signal drill ────────────────────────────────────────
            yield return StartCoroutine(PlayAndWait(sortClip));
            if (scoreText != null) scoreText.gameObject.SetActive(true);

            for (int i = 0; i < Sequence.Length; i++)
            {
                if (i == 0)
                {
                    ResetEnvironment();                         // already at start; no fade needed
                }
                else
                {
                    // Hide the snap-back behind a fade so it isn't a jarring teleport in VR.
                    yield return StartCoroutine(FadeOutIfPossible());
                    ResetEnvironment();
                    yield return StartCoroutine(FadeInIfPossible());
                }
                yield return StartCoroutine(RunRound(Sequence[i]));
            }

            if (scoreText != null)
                scoreText.text = $"Practice complete!\n{_correct} / {_total} correct";

            yield return StartCoroutine(PlayAndWait(closingClip));
            LoadAfterDrill();
        }

        // ── Round 1 ───────────────────────────────────────────────────────────

        IEnumerator RunButtonPractice()
        {
            // No blinking: the buttons behave like the real scene (colour on click). Narration guides.
            toggle.ApplyRemoteState(false);     // main track selected to start
            toggle.SetInteractionEnabled(true);

            yield return StartCoroutine(PlayAndWait(pressClip));
            yield return new WaitUntil(() => toggle.IsAction);

            yield return StartCoroutine(PlayAndWait(backClip));
            yield return new WaitUntil(() => !toggle.IsAction);

            yield return StartCoroutine(PlayAndWait(confirmClip));
            toggle.SetInteractionEnabled(false);
        }

        IEnumerator PlayClipWhileBlinkingSignal(AudioClip clip)
        {
            // Show the signal (neutral colour) and blink it while the clip introduces it.
            SetSignal(Color.white, true);
            bool hasClip = clip != null && narrationSource != null;
            if (hasClip) { narrationSource.clip = clip; narrationSource.loop = false; narrationSource.Play(); }
            float until = Time.time + 4f;
            Func<bool> stop = () => hasClip ? !narrationSource.isPlaying : Time.time >= until;

            bool on = false;
            while (!stop())
            {
                on = !on;
                SetActiveSafe(signalLight, on);
                float w = 0f;
                while (w < blinkInterval && !stop()) { w += Time.deltaTime; yield return null; }
            }
            SetActiveSafe(signalLight, false);
            yield return new WaitForSeconds(betweenClipsPause);
        }

        // ── Round 2 ───────────────────────────────────────────────────────────

        IEnumerator RunRound(bool isBlue)
        {
            // The environment was already reset to its start pose by the caller (under a fade between
            // reps). Show the signal for this round.
            SetSignal(isBlue ? blueColor : redColor, true);

            toggle.ApplyRemoteState(false);
            toggle.SetInteractionEnabled(true);

            float traveled = 0f;
            float turned = 0f;
            bool resolved = false;
            bool diverted = false;

            while (true)
            {
                float dist = approachSpeed * Time.deltaTime;
                environment.Translate(approachDirection.normalized * dist, Space.World);
                traveled += dist;

                // Reaching the diverting point: lock the decision (toggle state now), play the sound,
                // and begin the divert turn if the right button is selected.
                if (!resolved && traveled >= approachDistance)
                {
                    diverted = toggle.IsAction;
                    toggle.SetInteractionEnabled(false);
                    bool correct = (isBlue && diverted) || (!isBlue && !diverted);
                    if (correct) _correct++;
                    UpdateScore();
                    PlaySfx(correct ? correctClip : wrongClip);
                    resolved = true;
                }

                // After the fork, yaw the environment about the seat to "turn" onto the branch — same
                // rate as the real DriverTrainController (turn rate = speed / radius).
                if (resolved && diverted && turned < Mathf.Abs(branchTurnAngle))
                {
                    float stepDeg = (dist / Mathf.Max(branchRadius, 0.01f)) * Mathf.Rad2Deg;
                    float remaining = Mathf.Abs(branchTurnAngle) - turned;
                    if (stepDeg > remaining) stepDeg = remaining;
                    turned += stepDeg;
                    Vector3 pivot = divertPivot != null ? divertPivot.position : Vector3.zero;
                    environment.RotateAround(pivot, Vector3.up, stepDeg * Mathf.Sign(branchTurnAngle));
                }

                if (resolved && traveled >= approachDistance + postForkDistance) break;
                yield return null;
            }

            SetActiveSafe(signalLight, false);
            yield return new WaitForSeconds(interRoundDelay);
        }

        // ── Narration / helpers ─────────────────────────────────────────────────

        IEnumerator PlayAndWait(AudioClip clip)
        {
            if (narrationSource != null && clip != null)
            {
                narrationSource.clip = clip;
                narrationSource.loop = false;
                narrationSource.Play();
                yield return new WaitUntil(() => !narrationSource.isPlaying);
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }
            yield return new WaitForSeconds(betweenClipsPause);
        }

        void SetSignal(Color col, bool on)
        {
            SetActiveSafe(signalLight, on);
            if (signalLight == null || !on) return;
            var r = signalLight.GetComponentInChildren<Renderer>();
            if (r == null) return;
            var mat = r.material;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            if (mat.HasProperty("_Color"))     mat.SetColor("_Color", col);
            // Make it glow if the shader supports emission, so it reads as a lit signal.
            if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", col); }
        }

        void UpdateScore()
        {
            if (scoreText != null) scoreText.text = $"Correct decisions: {_correct} / {_total}";
        }

        void PlaySfx(AudioClip clip)
        {
            if (sfxSource != null && clip != null) sfxSource.PlayOneShot(clip);
        }

        static void SetActiveSafe(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active) go.SetActive(active);
        }

        void ResetEnvironment()
        {
            environment.localPosition = _envStartPos;
            environment.localRotation = _envStartRot;
        }

        // Fade the screen to/from black so the between-rep reset is invisible. No-op (instant) if no
        // fader is present, so the drill still runs.
        IEnumerator FadeOutIfPossible()
        {
            var fader = SceneFader.Instance;
            if (fader == null) yield break;
            bool done = false;
            fader.FadeToBlack(() => done = true);
            yield return new WaitUntil(() => done);
        }

        IEnumerator FadeInIfPossible()
        {
            var fader = SceneFader.Instance;
            if (fader == null) yield break;
            bool done = false;
            fader.FadeFromBlack(() => done = true);
            yield return new WaitUntil(() => done);
        }

        void LoadAfterDrill()
        {
            string next = !string.IsNullOrEmpty(nextSceneAfterDrill)
                ? nextSceneAfterDrill
                : TrolleyGameState.Instance?.NextScenarioScene();
            if (string.IsNullOrEmpty(next))
            {
                Debug.LogWarning("[TutorialDriverDrill] Tutorial finished but no next scene set (standalone test?).");
                return;
            }
            PilotController.Instance.LoadNewScene(next);
        }
    }
}
