using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using TMPro;
using VRT.Pilots.Common;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Standalone two-round practice for the Tutorial scene — completely separate from
    /// TrolleyController so it can't affect the real scenario flow.
    ///
    /// ROUND 1 — button familiarisation (guided):
    ///   • Intro narration: a preamble, then one short clip per CCTV monitor — each monitor's green
    ///     rim blinks for exactly as long as its clip plays (sync is automatic, no timestamp tuning),
    ///     then a short pause.
    ///   • "Press the right button to divert" → wait for the real B press; "now press the left button
    ///     to send it back" → wait for the A press. Buttons are NOT blinked — they behave like the real
    ///     scene (colour changes on click, the matching monitor rim glows green).
    ///
    /// ROUND 2 — sorting drill (3 rounds, like the driver tutorial):
    ///   • Before each round a fresh train resets to the approach point and an "approaching" line plays;
    ///     each train is coloured RED or BLUE — RED → do nothing (runs straight) · BLUE → press (diverts).
    ///   • No timer: the decision commits when the train passes the switch (divertThreshold). The train
    ///     moves at a constant WORLD speed so the divert reads as a clean turn (not a slow sideways crawl).
    ///   • Top-right counter tracks correct decisions; a ding/buzz plays each round.
    ///   • After the drill, the next scene in the session flow loads.
    ///
    /// Reuses the Bystander rail spline (0 = straight, 1 = branch), train mesh, the A/B button +
    /// monitor rims (via TrolleyToggleDecision), all wired by TrolleyTutorialBystanderSetup. This script
    /// only ever touches the tutorial scene — it does not modify any shared controller.
    /// </summary>
    public class TutorialBystanderDrill : MonoBehaviour
    {
        [Header("Track")]
        [Tooltip("Bystander rail: spline 0 = straight (left/inaction), spline 1 = branch (right/action).")]
        [SerializeField] SplineContainer rail;
        [SerializeField] Transform train;
        [SerializeField] float modelForwardYaw = 0f;   // matches the real Bystander TrainController

        [Header("Input")]
        [Tooltip("Reused A/B toggle. The drill reads IsAction (pressed = divert right) and resets it each round.")]
        [SerializeField] TrolleyToggleDecision toggle;

        [Header("Pacing")]
        [Tooltip("Optional Start button. If set, the A/B buttons go live for a free warm-up and the tutorial " +
                 "waits for a Start press before beginning, instead of a fixed startDelay. Null-safe.")]
        [SerializeField] TutorialGate gate;

        [Header("Dev / testing (Editor only — never affects a build)")]
        [Tooltip("RESEARCHER ONLY — jumps straight to the sorting rounds, skipping the intro narration and " +
                 "button practice, so you can test the divert without sitting through everything. Ignored in a " +
                 "player build, so it can't accidentally affect a real session.")]
        [SerializeField] bool devSkipToSorting = false;

        [Header("Sync Barriers")]
        [Tooltip("PFB_Barrier instance: gates tutorial start (before instructions). Leave unset to skip sync.")]
        [SerializeField] NetworkTrigger tutorialBarrierReady;
        [SerializeField] NetworkTrigger tutorialBarrierProceed;
        [Tooltip("PFB_Barrier instance (resetWhenDone=true): gates each round start. Leave unset to skip sync.")]
        [SerializeField] NetworkTrigger roundBarrierReady;
        [SerializeField] NetworkTrigger roundBarrierProceed;

        [Header("Round 1 — monitor rims (blink in turn during the intro)")]
        [Tooltip("Order matches the narration: approaching view, switch point, current/main track, diverting track.")]
        [SerializeField] GameObject rimApproach;   // top-left  — Monitor_WestView
        [SerializeField] GameObject rimSwitch;     // top-right — Monitor_SwitchPoint
        [SerializeField] GameObject rimMain;       // bottom-left  — Monitor_Track1East (= toggle RimA)
        [SerializeField] GameObject rimSide;       // bottom-right — Monitor_Track2East (= toggle RimB)
        [Tooltip("If a monitor's intro clip is missing, blink its rim for this many seconds instead, so " +
                 "the tutorial still demonstrates each monitor. Ignored when the clip is present — then " +
                 "the rim blinks for exactly the clip's length.")]
        [SerializeField] float fallbackCueSeconds = 4f;
        [Tooltip("Delay after the scene loads before the first narration plays, so participants can settle " +
                 "into the headset before anything happens.")]
        [SerializeField] float startDelay = 5f;
        [Tooltip("Silent pause inserted after every narration clip, so the recordings don't run together.")]
        [SerializeField] float betweenClipsPause = 2f;

        [Header("Round 1 — control buttons (the named button pulses during its monitor clip)")]
        [Tooltip("Kept so the setup script's name-based wiring stays valid. Buttons are pulsed only during " +
                 "the main/side monitor clips; in the practice round they use their own real-scene feedback.")]
        [SerializeField] GameObject buttonA;       // OBJ_NetworkButton_A (left — main track)
        [SerializeField] GameObject buttonB;       // OBJ_NetworkButton_B (right — divert)
        [Tooltip("Blink cadence for the monitor rims (and the paired button) during the intro.")]
        [SerializeField] float blinkInterval = 0.4f;
        [Tooltip("Highlight colour a button pulses to while its monitor is introduced (the main/side clips). " +
                 "Green, to match the toggle's selected state.")]
        [SerializeField] Color blinkColor = new Color(0.2f, 0.75f, 0.2f);
        [Tooltip("Unselected button colour shown while neither button is chosen (the whole intro). " +
                 "Match the toggle's default grey.")]
        [SerializeField] Color unselectedColor = new Color(0.35f, 0.35f, 0.35f);

        [Header("Narration — Round 1 intro (one clip per step; rim sync is automatic)")]
        [SerializeField] AudioSource narrationSource;
        [Tooltip("Tutorial narration plays at this fraction of the source volume (0.7 = 70%). Applied in Start; " +
                 "leaves the SFX source untouched.")]
        [Range(0f, 1f)]
        [SerializeField] float narrationVolume = 0.5f;
        [Tooltip("Preamble 1 — 'control room … two buttons'. No rim blinks.")]
        [SerializeField] AudioClip introClip;         // narration_tutorial_bystander_intro
        [Tooltip("Preamble 2 — 'four CCTV monitors …'. No rim blinks.")]
        [SerializeField] AudioClip monitorsClip;      // narration_tutorial_bystander_monitors
        [Tooltip("Per-monitor clips — each blinks its rim while it plays. Order: approach, switch, main, side.")]
        [SerializeField] AudioClip introApproachClip; // ..._monitor_approach → rimApproach (top-left)
        [SerializeField] AudioClip introSwitchClip;   // ..._monitor_switch   → rimSwitch  (top-right)
        [SerializeField] AudioClip introMainClip;     // ..._monitor_main     → rimMain    (bottom-left)
        [SerializeField] AudioClip introSideClip;     // ..._monitor_side     → rimSide    (bottom-right)

        [Header("Narration — Round 1 button practice (no blink; toggle behaves like the real scene)")]
        [SerializeField] AudioClip pressClip;         // ..._button_main: 'press the right button to divert'
        [SerializeField] AudioClip backClip;          // ..._button_side: 'press the left button to send it back'
        [SerializeField] AudioClip confirmClip;       // ..._button_confirm: 'selected button + rim glow green'

        [Header("Narration — Round 2")]
        [SerializeField] AudioClip sortClip;          // ..._sortingtrain: 'now let's sort the trains…'
        [Tooltip("Played before EACH sorting round — 'the next train is approaching' — as a fresh train " +
                 "appears at the start point (mirrors the driver tutorial's per-round cue). Null-safe.")]
        [SerializeField] AudioClip approachingClip;   // ..._approaching

        [Header("Round 2 — sorting drill")]
        [Tooltip("Where on the straight spline each train starts (0 = far end, higher = closer to the switch). " +
                 "Defaults baked to the tuned scene values so a rebuild doesn't reset them.")]
        [Range(0f, 0.9f)]
        [SerializeField] float roundStartT = 0.35f;
        [Tooltip("Where each train's straight run ends.")]
        [Range(0.1f, 1f)]
        [SerializeField] float roundEndT = 0.6f;
        [Tooltip("Unused — the train now follows the constant trainSpeed below (kept only so old scene " +
                 "wiring doesn't error).")]
        [SerializeField] float roundDuration = 12f;
        [Tooltip("Constant world speed (units/sec) matching the real Bystander TrainController (scene value 40), " +
                 "scaled by the decision-window factor. This is what makes the tutorial train's pace identical " +
                 "to the real scene — replaces the old roundDuration-derived speed that ran too fast.")]
        [SerializeField] float trainSpeed = 40f;
        [Tooltip("Fraction along the straight track where the switch is (between Start and End). Input stays " +
                 "open until the train passes this point, then the decision commits and the sound plays. " +
                 "Set it just before the visual fork.")]
        [Range(0.1f, 0.95f)]
        [SerializeField] float divertThreshold = 0.5f;
        [Tooltip("Gap between trains, in seconds: the train finishes its run, then this pause before the next.")]
        [SerializeField] float interRoundDelay = 1.5f;
        [SerializeField] Color redColor  = new Color(0.85f, 0.15f, 0.15f);
        [SerializeField] Color blueColor = new Color(0.15f, 0.30f, 0.90f);

        [Header("Feedback")]
        [SerializeField] TextMeshProUGUI scoreText;
        [SerializeField] AudioSource sfxSource;
        [SerializeField] AudioClip correctClip;
        [SerializeField] AudioClip wrongClip;

        [Header("After the drill")]
        [Tooltip("Closing line after the sorting rounds, before the next scene loads. " +
                 "e.g. 'This is the end of the tutorials. Next, the real scenarios will begin.'")]
        [SerializeField] AudioClip closingClip;

        // Fixed, predetermined order — identical for every participant (this is practice, not data).
        // true = BLUE (press, divert right) · false = RED (do nothing, runs straight).
        // Three rounds, mirroring the driver tutorial's divert/stay/divert: BLUE, RED, BLUE.
        static readonly bool[] Sequence = { true, false, true };

        Spline _current;
        float _t;
        float _startT;       // straight-spline t of the tram's placed position — matches the real scene start
        float _forkT;        // straight-spline t where the branch splits off — divert commits here (no jump)
        bool _inSorting;     // true once the sorting rounds have begun — staged skip uses this
        float _lastSkipTime = -1f;
        float _worldSpeed;   // units/sec — held constant across straight + branch so the divert doesn't crawl
        int _correct;
        int _total;

        TrolleyTimingConfig _cfg;

        void Start()
        {
            _cfg = TrolleyTimingConfig.Load();
            _total = Sequence.Length;
            UpdateScore();
            if (scoreText != null) scoreText.gameObject.SetActive(false); // counter is for Round 2 only

            if (rail == null || train == null || toggle == null)
            {
                Debug.LogError("[TutorialBystanderDrill] rail / train / toggle not wired — tutorial cannot run.");
                return;
            }
            // Match the real Bystander scene: start each train from the tram's placed position projected
            // onto the straight spline (exactly like TrainController.DoStartApproach), not a fixed fraction.
            float3 localStart = rail.transform.InverseTransformPoint(train.position);
            SplineUtility.GetNearestPoint(rail.Splines[0], localStart, out _, out _startT);
            // Find where the branch actually splits from the straight, so the divert commits AT the fork
            // (seamless turn) instead of at a fixed fraction that snaps the train sideways.
            _forkT = ComputeForkT();
            if (narrationSource != null) narrationSource.volume = narrationVolume;
            toggle.SetInteractionEnabled(false);
            SetActiveSafe(rimApproach, false);
            SetActiveSafe(rimSwitch, false);
            StartCoroutine(RunTutorial());
        }

        IEnumerator RunTutorial()
        {
            // Let every Start() run first (the toggle lights its default rim on Start), then force a
            // neutral look — both buttons unselected, no rim lit — and hold before the first narration
            // so participants can settle into the headset.
            yield return null;
            SetButtonsNeutral();
            // Warm-up + self-paced start: the real A/B buttons go live so the participant can freely try
            // them (no consequence), and a Start button appears; pressing Start ends the warm-up and begins
            // the tutorial. Falls back to a fixed settle delay if no Start button is wired.
            if (gate != null)
            {
                toggle.SetInteractionEnabled(true);   // free practice on the A/B buttons
                yield return StartCoroutine(gate.WaitForPress());
                toggle.SetInteractionEnabled(false);
                SetButtonsNeutral();
            }
            else yield return new WaitForSeconds(startDelay);

            yield return StartCoroutine(ReadyToStartTutorial());
#if VRT_WITH_STATS
            Cwipc.Statistics.Output("TrolleyTutorialBystanderDrill", "event=tutorial_start");
#endif

            // Researcher dev shortcut (Editor only): jump straight to the sorting rounds to test the divert.
            bool skipToSorting = false;
#if UNITY_EDITOR
            skipToSorting = devSkipToSorting;
#endif
            if (!skipToSorting)
            {
                // ── Round 1 — button familiarisation ──────────────────────────
                yield return StartCoroutine(RunIntro());
                yield return StartCoroutine(RunButtonPractice());

                // ── Round 2 — sorting drill intro ─────────────────────────────
                yield return StartCoroutine(PlayAndWait(sortClip));
            }

            yield return StartCoroutine(RunSortingRounds());
        }

        // The 3 sorting rounds + closing line, then the next scene. Extracted so the researcher skip button
        // (and devSkipToSorting) can jump straight here from the intro.
        IEnumerator RunSortingRounds()
        {
            _inSorting = true;
            // Three rounds, like the driver tutorial. Before each, a fresh train resets to the approach
            // point and a short "the next train is approaching" line plays as it appears — then it rolls.
            for (int i = 0; i < Sequence.Length; i++)
            {
                bool isBlue = Sequence[i];
                ResetTrainToStart(isBlue);
                yield return StartCoroutine(PlayAndWait(approachingClip));
                yield return StartCoroutine(RunRound(isBlue, i + 1));
            }

            // Closing line, e.g. "This is the end of the tutorials. Next, the real scenarios will begin."
            yield return StartCoroutine(PlayAndWait(closingClip));
            LoadAfterDrill();
        }

        // Researcher skip button (staged): first press jumps to the sorting rounds, second press loads the
        // next scene. De-bounced so one physical press (which may fire OnTrigger + selectEntered) counts once.
        [ContextMenu("Skip (staged: sorting → next scene)")]
        public void Skip()
        {
            if (Time.unscaledTime - _lastSkipTime < 0.4f) return;
            _lastSkipTime = Time.unscaledTime;

            if (_inSorting) LoadAfterDrill();
            else SkipToSorting();
        }

        void SkipToSorting()
        {
            StopAllCoroutines();
            if (narrationSource != null) narrationSource.Stop();
            SetButtonsNeutral();
            SetActiveSafe(rimApproach, false);
            SetActiveSafe(rimSwitch, false);
            if (toggle != null) toggle.SetInteractionEnabled(false);
            StartCoroutine(RunSortingRounds());
        }

        // ── Round 1 ───────────────────────────────────────────────────────────

        IEnumerator RunIntro()
        {
            var cueRims  = new[] { rimApproach, rimSwitch, rimMain, rimSide };
            var cueClips = new[] { introApproachClip, introSwitchClip, introMainClip, introSideClip };
            foreach (var r in cueRims) SetActiveSafe(r, false);

            // Preamble 1 — 'control room … two buttons'. No blink.
            yield return StartCoroutine(PlayAndWait(introClip));

            // Preamble 2 — 'four CCTV monitors …'. Blink ALL FOUR rims together to introduce them.
            yield return StartCoroutine(PlayClipWhileBlinking(monitorsClip, cueRims));

            // One clip per monitor: that monitor's rim blinks for exactly as long as its clip plays —
            // sync is automatic, no timestamp tuning, and it survives re-recording. The main/side clips
            // also pulse their control button (A/B) so the button↔track link is clear.
            var cueButtons = new[] { (GameObject)null, null, buttonA, buttonB };
            for (int i = 0; i < cueRims.Length; i++)
                yield return StartCoroutine(PlayClipWhileBlinking(cueClips[i], new[] { cueRims[i] }, cueButtons[i]));
        }

        // Plays a clip while the given rims (and, optionally, a button) blink together; if the clip is
        // missing, blinks for a fixed beat so each monitor is still demonstrated. Resets to neutral at end.
        IEnumerator PlayClipWhileBlinking(AudioClip clip, GameObject[] rims, GameObject button = null)
        {
            bool hasClip = clip != null && narrationSource != null;
            if (hasClip)
            {
                narrationSource.clip = clip;
                narrationSource.loop = false;
                narrationSource.Play();
            }
            float until = Time.time + fallbackCueSeconds;
            Func<bool> stop = () => hasClip ? !narrationSource.isPlaying : Time.time >= until;

            Renderer btn = button != null ? FindButtonRenderer(button) : null;

            bool on = false;
            while (!stop())
            {
                on = !on;
                foreach (var r in rims) SetActiveSafe(r, on);
                if (btn != null) SetColor(btn, on ? blinkColor : unselectedColor);
                float w = 0f;
                while (w < blinkInterval && !stop()) { w += Time.deltaTime; yield return null; }
            }
            foreach (var r in rims) SetActiveSafe(r, false);
            if (btn != null) SetColor(btn, unselectedColor);

            yield return new WaitForSeconds(betweenClipsPause);
        }

        IEnumerator RunButtonPractice()
        {
            // No blinking here: the buttons behave exactly like the real scene — clicking changes the
            // button colour and lights the matching monitor rim. The narration says what to press.
            toggle.ApplyRemoteState(false);     // start on the main track
            toggle.SetInteractionEnabled(true);

            // "Press the button on the right to divert the train." → wait for the real B press.
            yield return StartCoroutine(PlayAndWait(pressClip));
            yield return new WaitUntil(() => toggle.IsAction);

            // "Now press the button on the left to send it back to the main track." → wait for the A press.
            yield return StartCoroutine(PlayAndWait(backClip));
            yield return new WaitUntil(() => !toggle.IsAction);

            // "Perfect — the selected button and its monitor rim glow green."
            yield return StartCoroutine(PlayAndWait(confirmClip));

            toggle.SetInteractionEnabled(false);
        }

        // Neutral look for the whole intro: both buttons unselected, no monitor rim lit. The shared
        // toggle has no "neither selected" state, so we set it here (the toggle isn't interactive yet).
        void SetButtonsNeutral()
        {
            SetActiveSafe(rimMain, false);
            SetActiveSafe(rimSide, false);
            SetButtonColor(buttonA, unselectedColor);
            SetButtonColor(buttonB, unselectedColor);
        }

        void SetButtonColor(GameObject button, Color col)
        {
            if (button == null) return;
            var r = FindButtonRenderer(button);
            if (r != null) SetColor(r, col);
        }

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

        // ── Round 2 ───────────────────────────────────────────────────────────

        IEnumerator RunRound(bool isBlue, int roundNumber = 0)
        {
            // The caller already placed/coloured the train at the approach point (so it "appeared" during
            // the approaching narration); re-affirm here so RunRound is also safe to enter directly.
            ResetTrainToStart(isBlue);
            toggle.SetInteractionEnabled(true);
            yield return StartCoroutine(ReadyToStartRound());
#if VRT_WITH_STATS
            Cwipc.Statistics.Output("TrolleyTutorialBystanderDrill", $"event=round_start, round={roundNumber}");
#endif

            // The participant may press any time to ARM a divert, but the train only actually switches
            // tracks AT the diverting point (like a real rail switch) — and the correct/wrong sound plays
            // there too, once the train is visibly on the right track. Then it runs the chosen track out:
            // the branch all the way (so it shows on the divert monitor), the straight track to roundEndT.
            bool diverted = false;
            bool resolved = false;
            while (true)
            {
                // The participant can press (or change their mind) until the train reaches the fork; the
                // toggle's state AT the fork is the decision. The train switches there and the sound plays.
                if (!resolved && _t >= _forkT)
                {
                    if (toggle.IsAction) { Divert(); diverted = true; } // switch onto the branch at the fork
                    toggle.SetInteractionEnabled(false);

                    bool correct = (isBlue && diverted) || (!isBlue && !diverted);
                    if (correct) _correct++;
                    UpdateScore();
                    PlaySfx(correct ? correctClip : wrongClip);
                    resolved = true;
                }

                float endT = diverted ? 1f : roundEndT;
                if (_t >= endT) break;

                MoveStep();
                yield return null;
            }
            toggle.SetInteractionEnabled(false);

            yield return new WaitForSeconds(interRoundDelay);
        }

        protected virtual IEnumerator ReadyToStartTutorial()
        {
            yield return StartCoroutine(BarrierController.WaitFor(tutorialBarrierReady, tutorialBarrierProceed));
        }

        protected virtual IEnumerator ReadyToStartRound()
        {
            yield return StartCoroutine(BarrierController.WaitFor(roundBarrierReady, roundBarrierProceed));
        }

        // Colour the train for this round and place it at the approach point on the straight spline, ready
        // to roll, with the toggle back on the main track. Also calibrates the constant world speed used by
        // MoveStep. Leaves interaction disabled (RunRound enables it once the round actually starts).
        void ResetTrainToStart(bool isBlue)
        {
            ColorTrain(isBlue ? blueColor : redColor);

            _current = rail.Splines[0];
            _t = _startT;   // the tram's placed position on the straight spline (matches the real scene)
            train.position = EvaluateWorld(_current, _t);
            OrientToTrack();

            // Constant WORLD speed matching the real Bystander scene: TrainController moves at trainSpeed ×
            // the decision-window factor, so we use the same value here. (The old roundDuration-derived
            // speed ran faster than the real train and made it overshoot the fork instead of turning cleanly.)
            _worldSpeed = trainSpeed;
            if (_cfg != null) _worldSpeed *= _cfg.SpeedFactor;

            toggle.ApplyRemoteState(false);     // back to "not diverted"
        }

        // ── Train movement (self-contained spline follow) ──────────────────────

        void MoveStep()
        {
            if (_current == null) return;

            // Advance at a constant WORLD speed (units/sec), normalising by the current spline's length —
            // exactly like the real TrainController. A fixed t-rate made the train almost stop at the fork
            // and slide sideways onto the branch over several seconds; this keeps the speed steady through
            // the divert so it reads as a clean turn at the switch.
            float len = Mathf.Max(0.01f, _current.GetLength());
            _t += (_worldSpeed / len) * Time.deltaTime;
            if (_t > 1f) _t = 1f;

            train.position = EvaluateWorld(_current, _t);
            OrientToTrack();
        }

        void Divert()
        {
            if (rail.Splines.Count < 2) return;
            var branch = rail.Splines[1];
            float3 localPos = rail.transform.InverseTransformPoint(train.position);
            SplineUtility.GetNearestPoint(branch, localPos, out _, out _t);
            _current = branch;
        }

        // Walk the straight spline; the fork is the LAST point that coincides with the branch spline
        // (in world space). Before the fork the two tracks overlap, so committing the divert there curves
        // smoothly onto the branch instead of snapping sideways. Falls back to divertThreshold if the two
        // splines never coincide (e.g. a fully separate branch track).
        //
        // Do NOT stop at the first gap (#89): the straight track is an AutoSmooth Bézier while the branch's
        // shared run is Linear knots (a chord), so mid-approach they drift ~0.5 m apart before re-converging
        // at the exact shared fork knot. Breaking at that drift detected a phantom fork ~200 m early — the
        // beep played and input locked ~5 s before the train visibly reached the switch. Taking the last
        // coincident sample lands on the real fork; past it the branch veers off (~14°) and never returns.
        float ComputeForkT()
        {
            if (rail.Splines.Count < 2) return divertThreshold;
            var s0 = rail.Splines[0];
            var s1 = rail.Splines[1];
            const int steps = 300;
            const float epsSqr = 0.25f;   // 0.5 m tolerance for "still on the shared track"
            float fork = -1f;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector3 world0 = EvaluateWorld(s0, t);
                float3 query = rail.transform.InverseTransformPoint(world0);
                SplineUtility.GetNearestPoint(s1, query, out float3 nearLocal, out _);
                Vector3 nearWorld = rail.transform.TransformPoint(nearLocal);
                if ((world0 - nearWorld).sqrMagnitude < epsSqr) fork = t;
            }
            return fork >= 0f ? fork : divertThreshold;
        }

        void OrientToTrack()
        {
            float3 tangent = SplineUtility.EvaluateTangent(_current, _t);
            Vector3 worldTangent = rail.transform.TransformDirection(tangent);
            if (worldTangent.sqrMagnitude > 0.001f)
                train.rotation = Quaternion.LookRotation(worldTangent) * Quaternion.Euler(0f, modelForwardYaw, 0f);
        }

        Vector3 EvaluateWorld(Spline spline, float t)
        {
            float3 local = SplineUtility.EvaluatePosition(spline, t);
            return rail.transform.TransformPoint(local);
        }

        // ── Feedback / helpers ──────────────────────────────────────────────────

        void ColorTrain(Color c)
        {
            foreach (var rend in train.GetComponentsInChildren<Renderer>(true))
            {
                var mat = rend.material; // per-instance
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                if (mat.HasProperty("_Color"))     mat.SetColor("_Color", c);
            }
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

        static Renderer FindButtonRenderer(GameObject root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name.Contains("Button")) { var r = child.GetComponent<Renderer>(); if (r != null) return r; }
            return root.GetComponentInChildren<MeshRenderer>(true);
        }

        static void SetColor(Renderer rend, Color col)
        {
            var mat = rend.material;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);
        }

        void LoadAfterDrill()
        {
            string next = TrolleyGameState.Instance?.NextScene();
            if (string.IsNullOrEmpty(next))
            {
                Debug.LogWarning("[TutorialBystanderDrill] Tutorial finished but TrolleyGameState has no next scene.");
                return;
            }
            PilotController.Instance.LoadNewScene(next);
        }
    }
}
