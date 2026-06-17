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
    ///   • Intro narration describes the four CCTV monitors; each monitor's green rim blinks in
    ///     turn as it is named (timings in monitorHighlightTimes), then a short pause.
    ///   • "Press the button to divert" → button B blinks, we wait for the real press (the side
    ///     monitor highlights). "Now change it back" → button A blinks, we wait for the A press.
    ///
    /// ROUND 2 — sorting drill:
    ///   • A short sequence of trains approaches one at a time, each coloured RED or BLUE:
    ///       RED  → do nothing (runs straight)   ·   BLUE → press the button (diverts right).
    ///   • No timer: the decision commits when the train passes the switch (divertThreshold).
    ///   • Top-right counter tracks correct decisions; a ding/buzz plays each round.
    ///   • After the drill, the practice questionnaire scene loads.
    ///
    /// Reuses the Bystander rail spline (0 = straight, 1 = branch), train mesh, the A/B button +
    /// monitor rims (via TrolleyToggleDecision), all wired by TrolleyTutorialSetup. This script
    /// only ever touches the tutorial scene — it does not modify any shared controller.
    /// </summary>
    public class TutorialTrainDrill : MonoBehaviour
    {
        [Header("Track")]
        [Tooltip("Bystander rail: spline 0 = straight (left/inaction), spline 1 = branch (right/action).")]
        [SerializeField] SplineContainer rail;
        [SerializeField] Transform train;
        [SerializeField] float trainSpeed = 6f;
        [SerializeField] float modelForwardYaw = 180f;

        [Header("Input")]
        [Tooltip("Reused A/B toggle. The drill reads IsAction (pressed = divert right) and resets it each round.")]
        [SerializeField] TrolleyToggleDecision toggle;

        [Header("Round 1 — monitor rims (blink in turn during the intro)")]
        [Tooltip("Order matches the narration: approaching view, switch point, current/main track, diverting track.")]
        [SerializeField] GameObject rimApproach;   // top-left  — Monitor_WestView
        [SerializeField] GameObject rimSwitch;     // top-right — Monitor_SwitchPoint
        [SerializeField] GameObject rimMain;       // bottom-left  — Monitor_Track1East (= toggle RimA)
        [SerializeField] GameObject rimSide;       // bottom-right — Monitor_Track2East (= toggle RimB)
        [Tooltip("Seconds into the intro clip at which each monitor (in the order above) starts blinking. " +
                 "Tune to your recording.")]
        [SerializeField] float[] monitorHighlightTimes = { 1f, 5f, 9f, 13f };
        [Tooltip("Quiet pause after the intro narration before the button practice begins.")]
        [SerializeField] float introPauseAfter = 3f;

        [Header("Round 1 — control buttons (blink as a prompt)")]
        [SerializeField] GameObject buttonA;       // OBJ_NetworkButton_A (left — main track)
        [SerializeField] GameObject buttonB;       // OBJ_NetworkButton_B (right — divert)
        [SerializeField] float blinkInterval = 0.4f;
        [Tooltip("Pulse colour for the prompted button. Bright/contrasting so it shows on both the grey " +
                 "and green button states (button A starts green-selected).")]
        [SerializeField] Color blinkColor = new Color(1f, 0.9f, 0.2f);

        [Header("Narration (separate clip per step; optional — falls back to short pauses)")]
        [SerializeField] AudioSource narrationSource;
        [SerializeField] AudioClip introClip;      // control room + four monitors
        [SerializeField] AudioClip pressClip;      // "let's try pressing — press to divert"
        [SerializeField] AudioClip backClip;       // "great, now change it back"
        [SerializeField] AudioClip sortClip;       // "now let's sort the trains…"

        [Header("Round 2 — sorting drill")]
        [Tooltip("Fraction along the straight track where the switch is. Input stays open until the " +
                 "train passes this point, then the decision commits — no timer, purely spatial.")]
        [Range(0.1f, 0.95f)]
        [SerializeField] float divertThreshold = 0.5f;
        [Tooltip("Gap between trains, in seconds: the train finishes its run, then this pause before the next.")]
        [SerializeField] float interRoundDelay = 10f;
        [SerializeField] Color redColor  = new Color(0.85f, 0.15f, 0.15f);
        [SerializeField] Color blueColor = new Color(0.15f, 0.30f, 0.90f);

        [Header("Feedback")]
        [SerializeField] TextMeshProUGUI scoreText;
        [SerializeField] AudioSource sfxSource;
        [SerializeField] AudioClip correctClip;
        [SerializeField] AudioClip wrongClip;

        [Header("After the drill")]
        [Tooltip("Practice questionnaire scene to load when the drill ends, so participants rehearse the " +
                 "questionnaire before it counts. Empty = skip straight to the first real scenario.")]
        [SerializeField] string practiceQuestionnaireScene = "TrolleyPracticeQuestionnaire";

        // Fixed, predetermined order — identical for every participant (this is practice, not data).
        // true = BLUE (press, divert right) · false = RED (do nothing, runs straight).
        // RED, BLUE, BLUE, RED, BLUE.
        static readonly bool[] Sequence = { false, true, true, false, true };

        Spline _current;
        float _t;
        int _correct;
        int _total;

        void Start()
        {
            _total = Sequence.Length;
            UpdateScore();
            if (scoreText != null) scoreText.gameObject.SetActive(false); // counter is for Round 2 only

            if (rail == null || train == null || toggle == null)
            {
                Debug.LogError("[TutorialTrainDrill] rail / train / toggle not wired — tutorial cannot run.");
                return;
            }
            toggle.SetInteractionEnabled(false);
            SetActiveSafe(rimApproach, false);
            SetActiveSafe(rimSwitch, false);
            StartCoroutine(RunTutorial());
        }

        IEnumerator RunTutorial()
        {
            // ── Round 1 — button familiarisation ──────────────────────────────
            yield return StartCoroutine(RunIntro());
            yield return StartCoroutine(RunButtonPractice());

            // ── Round 2 — sorting drill ───────────────────────────────────────
            yield return StartCoroutine(PlayAndWait(sortClip));
            if (scoreText != null) scoreText.gameObject.SetActive(true);

            foreach (bool isBlue in Sequence)
                yield return StartCoroutine(RunRound(isBlue));

            if (scoreText != null)
                scoreText.text = $"Practice complete!\n{_correct} / {_total} correct";
            yield return new WaitForSeconds(2f);
            LoadAfterDrill();
        }

        // ── Round 1 ───────────────────────────────────────────────────────────

        IEnumerator RunIntro()
        {
            var cueRims    = new[] { rimApproach, rimSwitch, rimMain, rimSide };
            var cueButtons = new[] { (GameObject)null, null, buttonA, buttonB }; // A names the main track, B the divert
            foreach (var r in cueRims) SetActiveSafe(r, false);

            if (narrationSource != null && introClip != null)
            {
                narrationSource.clip = introClip;
                narrationSource.loop = false;
                narrationSource.Play();
            }

            float start = Time.time;
            for (int i = 0; i < cueRims.Length; i++)
            {
                float cueStart = start + CueTime(i);
                yield return new WaitUntil(() => Time.time >= cueStart);

                // This cue blinks until the next cue is due (last one: until the narration ends).
                int idx = i;
                yield return StartCoroutine(BlinkCue(cueRims[i], cueButtons[i], () =>
                    idx + 1 < cueRims.Length
                        ? Time.time >= start + CueTime(idx + 1)
                        : !(narrationSource != null && narrationSource.isPlaying)));

                SetActiveSafe(cueRims[i], false);
            }

            if (narrationSource != null && narrationSource.isPlaying)
                yield return new WaitUntil(() => !narrationSource.isPlaying);

            yield return new WaitForSeconds(introPauseAfter);
        }

        float CueTime(int i) => (i < monitorHighlightTimes.Length) ? monitorHighlightTimes[i] : i * 4f;

        IEnumerator RunButtonPractice()
        {
            // Default state: main track selected (RimA on, RimB off), input enabled.
            toggle.ApplyRemoteState(false);
            toggle.SetInteractionEnabled(true);

            // "Let's try pressing the button. Press the button to divert the train."
            yield return StartCoroutine(PlayAndWait(pressClip));
            yield return StartCoroutine(BlinkCue(null, buttonB, () => toggle.IsAction));
            toggle.ApplyRemoteState(true);   // re-assert the toggle's own colours/rims after the blink

            // "Great, now let's change the track back to the original position."
            yield return StartCoroutine(PlayAndWait(backClip));
            yield return StartCoroutine(BlinkCue(null, buttonA, () => !toggle.IsAction));
            toggle.ApplyRemoteState(false);

            toggle.SetInteractionEnabled(false);
        }

        // Blinks a monitor rim and/or pulses a button until stop() returns true.
        IEnumerator BlinkCue(GameObject rim, GameObject button, Func<bool> stop)
        {
            Renderer btn = button != null ? FindButtonRenderer(button) : null;
            Color baseCol = btn != null ? GetColor(btn) : default;

            bool on = false;
            while (!stop())
            {
                on = !on;
                SetActiveSafe(rim, on);
                if (btn != null) SetColor(btn, on ? blinkColor : baseCol);

                float w = 0f;
                while (w < blinkInterval && !stop()) { w += Time.deltaTime; yield return null; }
            }
            SetActiveSafe(rim, false);
            if (btn != null) SetColor(btn, baseCol);
        }

        IEnumerator PlayAndWait(AudioClip clip)
        {
            if (narrationSource == null || clip == null) { yield return new WaitForSeconds(0.5f); yield break; }
            narrationSource.clip = clip;
            narrationSource.loop = false;
            narrationSource.Play();
            yield return new WaitUntil(() => !narrationSource.isPlaying);
        }

        // ── Round 2 ───────────────────────────────────────────────────────────

        IEnumerator RunRound(bool isBlue)
        {
            ColorTrain(isBlue ? blueColor : redColor);

            // Reset to the start of the straight spline.
            _current = rail.Splines[0];
            _t = 0f;
            train.position = EvaluateWorld(_current, 0f);
            OrientToTrack();

            toggle.ApplyRemoteState(false);     // back to "not diverted"
            toggle.SetInteractionEnabled(true);

            // No timer: input stays open while the train approaches the switch. Pressing diverts;
            // once the train passes divertThreshold the decision commits and input locks.
            bool diverted = false;
            bool inputLocked = false;
            while (_t < 1f)
            {
                if (!inputLocked)
                {
                    if (toggle.IsAction)
                    {
                        Divert();
                        diverted = true;
                        inputLocked = true;
                        toggle.SetInteractionEnabled(false);
                    }
                    else if (_t >= divertThreshold)
                    {
                        // Train passed the switch — too late to divert, decision is "do nothing".
                        inputLocked = true;
                        toggle.SetInteractionEnabled(false);
                    }
                }
                MoveStep();
                yield return null;
            }
            toggle.SetInteractionEnabled(false);

            bool correct = (isBlue && diverted) || (!isBlue && !diverted);
            if (correct) _correct++;
            UpdateScore();
            PlaySfx(correct ? correctClip : wrongClip);

            yield return new WaitForSeconds(interRoundDelay);
        }

        // ── Train movement (self-contained spline follow) ──────────────────────

        void MoveStep()
        {
            if (_current == null) return;
            float len = _current.GetLength();
            if (len < 0.01f) return;

            _t += (trainSpeed / len) * Time.deltaTime;
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

        static Color GetColor(Renderer rend)
        {
            var mat = rend.material;
            if (mat.HasProperty("_BaseColor")) return mat.GetColor("_BaseColor");
            if (mat.HasProperty("_Color"))     return mat.GetColor("_Color");
            return Color.white;
        }

        static void SetColor(Renderer rend, Color col)
        {
            var mat = rend.material;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);
        }

        void LoadAfterDrill()
        {
            // Prefer the practice questionnaire so participants rehearse it; if not set, go straight
            // to the first real scenario.
            string next = !string.IsNullOrEmpty(practiceQuestionnaireScene)
                ? practiceQuestionnaireScene
                : TrolleyGameState.Instance?.NextScenarioScene();
            if (string.IsNullOrEmpty(next))
            {
                Debug.LogWarning("[TutorialTrainDrill] Tutorial finished but no next scene set (standalone test?).");
                return;
            }
            PilotController.Instance.LoadNewScene(next);
        }
    }
}
