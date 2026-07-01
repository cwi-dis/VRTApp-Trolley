using System.Collections;
using UnityEngine;
using TMPro;
using VRT.Pilots.Common;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Standalone first-person practice for the DRIVER Tutorial scene — the counterpart of
    /// TutorialTrainDrill. Completely separate from TrolleyController/DriverTrainController so it can't
    /// affect the real scenario flow.
    ///
    /// The player sits in the cab; the whole environment (TrackEnvironment) slides toward them, exactly
    /// like the real Driver scene. A divert yaws the environment about the player's seat (DivertMarker).
    ///
    /// Buttons are NOT re-taught here — the participant already practised them in the bystander tutorial.
    /// Flow: intro → "watch the window" → the rules → 3 rock-blocker reps → closing.
    ///
    /// Rock-blocker reps:
    ///   • Each round one track ahead is blocked by a rocky mountain barrier; the other is clear. The
    ///     player must end up on the CLEAR track: rocks on the main track → divert; rocks on the side
    ///     track → do nothing.
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
        [Tooltip("World distance the environment slides before the turn begins (the switch). Matches the " +
                 "real Driver scene, where the divert fires at decision-window close = approachSpeed (9.5) × " +
                 "8s window ≈ 76 units. 60 turned before reaching the fork (too early).")]
        [SerializeField] float approachDistance = 76f;
        [Tooltip("Extra distance the environment keeps moving past the switch before the round ends.")]
        [SerializeField] float postForkDistance = 25f;

        [Header("Divert (yaw the environment about the player's seat — mirrors DriverTrainController)")]
        [SerializeField] Transform divertPivot;          // DivertMarker
        [SerializeField] float branchTurnAngle = -90f;
        [SerializeField] float branchRadius = 95f;

        [Header("Input")]
        [Tooltip("Reused A/B toggle. The drill reads IsAction (right = divert) and resets it each round.")]
        [SerializeField] TrolleyToggleDecision toggle;

        [Header("Pacing")]
        [Tooltip("Optional Start button. If set, the A/B buttons go live for a free warm-up and the tutorial " +
                 "waits for a Start press before beginning, instead of a fixed startDelay. Null-safe.")]
        [SerializeField] TutorialGate gate;

        [Header("Rock blockers — one per track; the BLOCKED track is the one to avoid")]
        [Tooltip("Rocky barrier on the MAIN track. Shown when this round's answer is to divert.")]
        [SerializeField] GameObject mainTrackBlocker;
        [Tooltip("Rocky barrier on the SIDE track. Shown when this round's answer is to stay.")]
        [SerializeField] GameObject sideTrackBlocker;

        [Header("Narration — four clips; buttons were already taught in the bystander tutorial")]
        [SerializeField] AudioSource narrationSource;
        [Tooltip("Tutorial narration plays at this fraction of the source volume (0.7 = 70%). Applied in Start; " +
                 "leaves the SFX source untouched.")]
        [Range(0f, 1f)]
        [SerializeField] float narrationVolume = 0.5f;
        [Tooltip("'second tutorial — you're driving now, divert with the two buttons'.")]
        [SerializeField] AudioClip introClip;            // narration_tutorial_driver_intro
        [Tooltip("'watch for obstacles ahead through the front window'.")]
        [SerializeField] AudioClip windowClip;           // narration_tutorial_driver_window
        [Tooltip("The rules: 'one side is blocked with rocks — drive to the other side. Three rounds.'")]
        [SerializeField] AudioClip sortClip;             // narration_tutorial_driver_sortingtrain
        [Tooltip("'this is the end of the second tutorial'.")]
        [SerializeField] AudioClip closingClip;          // narration_tutorial_driver_closing

        [Header("Timing")]
        [SerializeField] float startDelay = 5f;
        [Tooltip("Extra silent pause after each narration clip. The recordings already carry ~2s of their " +
                 "own trailing pause, so keep this small to avoid double gaps.")]
        [SerializeField] float betweenClipsPause = 0f;
        [Tooltip("Distance past the fork before the correct/wrong beep plays — i.e. when the train reaches " +
                 "the rocks and you can see whether you cleared them. Keep < postForkDistance. Smaller = earlier.")]
        [SerializeField] float beepAfterForkDistance = 14f;

        [Header("Feedback")]
        [SerializeField] TextMeshProUGUI scoreText;
        [SerializeField] AudioSource sfxSource;
        [SerializeField] AudioClip correctClip;
        [SerializeField] AudioClip wrongClip;

        [Header("After the drill")]

        // Fixed, predetermined order (practice, not data). true = rocks on the MAIN track (divert) ·
        // false = rocks on the SIDE track (stay). 3 reps: divert, stay, divert — diverting is the skill
        // that needs the most practice.
        static readonly bool[] Sequence = { true, false, true };

        Vector3 _envStartPos;
        Quaternion _envStartRot;
        float _traveled;   // distance the environment has slid since the last reset (≈ displacement from start)
        int _correct;
        int _total;

        TrolleyTimingConfig _cfg;
        // Approach speed scaled to the active decision window, so the tutorial drives at the same pace as
        // the real Driver scene (a longer window → slower drive). Falls back to the raw value with no config.
        float EffectiveApproachSpeed => approachSpeed * (_cfg != null ? _cfg.SpeedFactor : 1f);

        void Start()
        {
            _cfg = TrolleyTimingConfig.Load();
            _total = Sequence.Length;
            UpdateScore();
            if (scoreText != null) scoreText.gameObject.SetActive(false); // counter shown only during the reps
            HideBlockers();

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

            if (narrationSource != null) narrationSource.volume = narrationVolume;
            toggle.SetInteractionEnabled(false);
            StartCoroutine(RunTutorial());
        }

        IEnumerator RunTutorial()
        {
            yield return null;                 // let the toggle's Start() run first
            toggle.SetNeutral();               // both buttons neutral during the intro — nothing pre-selected
            toggle.SetInteractionEnabled(false);
            // Warm-up + self-paced start: A/B buttons go live so the participant can freely try them, a Start
            // button appears, and pressing it begins the tutorial (falls back to startDelay if unwired).
            if (gate != null)
            {
                toggle.SetInteractionEnabled(true);
                yield return StartCoroutine(gate.WaitForPress());
                toggle.SetInteractionEnabled(false);
                toggle.SetNeutral();
            }
            else yield return new WaitForSeconds(startDelay);

            yield return StartCoroutine(ReadyToStartTutorial());
#if VRT_WITH_STATS
            Cwipc.Statistics.Output("TrolleyTutorialDriverDrill", "event=tutorial_start");
#endif

            // ── Intro (buttons were already taught in the bystander tutorial) ──
            yield return StartCoroutine(PlayAndWait(introClip));
            yield return StartCoroutine(PlayAndWait(windowClip));

            // ── Rock-blocker drill — the rules, then 3 reps ───────────────────
            yield return StartCoroutine(PlayAndWait(sortClip));
            if (scoreText != null) scoreText.gameObject.SetActive(true);

            ResetEnvironment();   // start the first approach from the seat's start pose
            for (int i = 0; i < Sequence.Length; i++)
            {
                yield return StartCoroutine(RunRound(Sequence[i], i + 1));
                // Between reps: keep the train rolling while we fade out, snap the world back under
                // black, then fade in — so the train never visibly stops.
                if (i < Sequence.Length - 1)
                    yield return StartCoroutine(TransitionToNextRound());
            }
            HideBlockers();

            if (scoreText != null)
                scoreText.text = $"Practice complete!\n{_correct} / {_total} correct";

            yield return StartCoroutine(PlayAndWait(closingClip));
            LoadAfterDrill();
        }

        // ── Rock-blocker reps ──────────────────────────────────────────────────

        IEnumerator RunRound(bool rocksOnMain, int roundNumber = 0)
        {
            // The environment is at its start pose (reset by the caller). Block one track with rocks:
            // main blocked → the answer is to divert.
            ShowBlocker(rocksOnMain);

            // Reset to the inaction default; SetInteractionEnabled(true) then reveals it (A highlighted)
            // exactly as the real scenes do when their decision window opens.
            toggle.ApplyRemoteState(false);
            toggle.SetInteractionEnabled(true);
            yield return StartCoroutine(ReadyToStartRound());
#if VRT_WITH_STATS
            Cwipc.Statistics.Output("TrolleyTutorialDriverDrill", $"event=round_start, round={roundNumber}");
#endif

            float turned = 0f;
            bool resolved = false;
            bool beeped   = false;
            bool diverted = false;
            bool correct  = false;

            float roundEnd = approachDistance + postForkDistance;
            while (_traveled < roundEnd)
            {
                float dist = DriveStep();

                // Reaching the fork: lock the decision (no switching past the switch) and score it —
                // silently. The beep is held back until the train reaches the rocks (below).
                if (!resolved && _traveled >= approachDistance)
                {
                    diverted = toggle.IsAction;
                    toggle.SetInteractionEnabled(false);
                    correct = (rocksOnMain && diverted) || (!rocksOnMain && !diverted);
                    if (correct) _correct++;
                    UpdateScore();
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

                // Beep only once the train reaches the rocks — the moment you can see whether you cleared them.
                if (resolved && !beeped && _traveled >= approachDistance + beepAfterForkDistance)
                {
                    PlaySfx(correct ? correctClip : wrongClip);
                    beeped = true;
                }

                yield return null;
            }
            // The train keeps moving: the caller's transition fades out, hides the blockers, resets the
            // world under black, and fades back in — all while it's still rolling.
        }

        // Translate the environment one frame's worth along the approach axis and track how far it's slid
        // since the last reset (≈ its displacement from the start pose, so the fork fires at the right place).
        float DriveStep()
        {
            float dist = EffectiveApproachSpeed * Time.deltaTime;
            environment.Translate(approachDirection.normalized * dist, Space.World);
            _traveled += dist;
            return dist;
        }

        /// <summary>Called after the gate/settle and before the first instruction clip. Override to insert a sync barrier. Default: no-op.</summary>
        protected virtual IEnumerator ReadyToStartTutorial() { yield break; }

        /// <summary>Called just before each round's movement loop starts. Override to insert a sync barrier. Default: no-op.</summary>
        protected virtual IEnumerator ReadyToStartRound() { yield break; }

        // Keep the train rolling forward while the screen fades out, snap the world back to its start pose
        // under full black (invisible), then fade in — still rolling. No visible stop, no teleport.
        IEnumerator TransitionToNextRound()
        {
            HideBlockers();
            yield return StartCoroutine(DriveWhileFading(toBlack: true));
            ResetEnvironment();
            yield return StartCoroutine(DriveWhileFading(toBlack: false));
        }

        IEnumerator DriveWhileFading(bool toBlack)
        {
            var fader = SceneFader.Instance;
            if (fader == null)
            {
                // No fader (standalone test): coast briefly so motion stays continuous.
                float t = 0f;
                while (t < 0.5f) { DriveStep(); t += Time.deltaTime; yield return null; }
                yield break;
            }
            bool done = false;
            if (toBlack) fader.FadeToBlack(() => done = true);
            else         fader.FadeFromBlack(() => done = true);
            while (!done) { DriveStep(); yield return null; }
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

        // Block exactly one track. rocksOnMain → the main-track barrier is shown (answer: divert);
        // otherwise the side-track barrier is shown (answer: stay).
        void ShowBlocker(bool rocksOnMain)
        {
            SetActiveSafe(mainTrackBlocker, rocksOnMain);
            SetActiveSafe(sideTrackBlocker, !rocksOnMain);
        }

        void HideBlockers()
        {
            SetActiveSafe(mainTrackBlocker, false);
            SetActiveSafe(sideTrackBlocker, false);
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
            _traveled = 0f;
        }

        void LoadAfterDrill()
        {
            string next = TrolleyGameState.Instance?.NextScene();
            if (string.IsNullOrEmpty(next))
            {
                Debug.LogWarning("[TutorialDriverDrill] Tutorial finished but TrolleyGameState has no next scene.");
                return;
            }
            PilotController.Instance.LoadNewScene(next);
        }
    }
}
