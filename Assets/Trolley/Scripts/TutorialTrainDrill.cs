using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using TMPro;
using VRT.Pilots.Common;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Standalone practice drill for the Tutorial scene — completely separate from
    /// TrolleyController so it can't affect the real scenario flow.
    ///
    /// A sequence of trains approaches one at a time, each coloured RED or BLUE:
    ///   • RED  train → correct response is to DO NOTHING (it runs straight/left).
    ///   • BLUE train → correct response is to PRESS the button (it diverts right).
    ///
    /// Each correct handling plays a ding and increments the top-right counter; a wrong
    /// one plays a buzz and does not. After all trains, the first real scenario loads.
    ///
    /// Reuses the Bystander rail spline (0 = straight/left, 1 = branch/right), train mesh,
    /// and the A/B button (via TrolleyToggleDecision) for input. Wired by TrolleyTutorialSetup.
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

        [Header("Drill")]
        [SerializeField] int blueTrains = 5;   // press = divert right
        [SerializeField] int redTrains  = 5;   // do nothing = straight/left
        [Tooltip("Seconds the player has to act, while the train approaches, before the outcome locks.")]
        [SerializeField] float decisionWindow = 3f;
        [Tooltip("Pause between trains.")]
        [SerializeField] float interRoundDelay = 1f;

        [Header("Train colours")]
        [SerializeField] Color redColor  = new Color(0.85f, 0.15f, 0.15f);
        [SerializeField] Color blueColor = new Color(0.15f, 0.30f, 0.90f);

        [Header("Feedback")]
        [SerializeField] TextMeshProUGUI scoreText;
        [SerializeField] AudioSource sfxSource;
        [SerializeField] AudioClip correctClip;
        [SerializeField] AudioClip wrongClip;

        [Header("Intro narration (optional)")]
        [SerializeField] NarrationPlayer narrationPlayer;

        Spline _current;
        float _t;
        int _correct;
        int _total;
        bool _narrationDone;

        void Start()
        {
            _total = blueTrains + redTrains;
            UpdateScore();

            if (rail == null || train == null || toggle == null)
            {
                Debug.LogError("[TutorialTrainDrill] rail / train / toggle not wired — drill cannot run.");
                return;
            }
            toggle.SetInteractionEnabled(false);
            StartCoroutine(RunDrill());
        }

        IEnumerator RunDrill()
        {
            // Intro narration explains the red/blue rule, then the drill starts.
            if (narrationPlayer != null)
            {
                narrationPlayer.OnNarrationComplete += () => _narrationDone = true;
                narrationPlayer.Play();
                yield return new WaitUntil(() => _narrationDone);
            }
            else
            {
                yield return new WaitForSeconds(1f);
            }

            foreach (bool isBlue in BuildSequence())
                yield return StartCoroutine(RunRound(isBlue));

            if (scoreText != null)
                scoreText.text = $"Practice complete!\n{_correct} / {_total} correct";
            yield return new WaitForSeconds(2f);
            LoadFirstScenario();
        }

        List<bool> BuildSequence()
        {
            var seq = new List<bool>(_total);
            for (int i = 0; i < blueTrains; i++) seq.Add(true);
            for (int i = 0; i < redTrains; i++)  seq.Add(false);
            // Fisher–Yates shuffle
            for (int i = seq.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (seq[i], seq[j]) = (seq[j], seq[i]);
            }
            return seq;
        }

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

            bool diverted = false;
            float elapsed = 0f;
            while (elapsed < decisionWindow)
            {
                MoveStep();
                if (!diverted && toggle.IsAction)
                {
                    Divert();
                    diverted = true;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            toggle.SetInteractionEnabled(false);

            bool correct = (isBlue && diverted) || (!isBlue && !diverted);
            if (correct) _correct++;
            UpdateScore();
            PlaySfx(correct ? correctClip : wrongClip);

            // Let the train finish running off its chosen track.
            while (_t < 1f)
            {
                MoveStep();
                yield return null;
            }
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

        // ── Feedback ────────────────────────────────────────────────────────────

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
            if (scoreText != null) scoreText.text = $"Correct: {_correct} / {_total}";
        }

        void PlaySfx(AudioClip clip)
        {
            if (sfxSource != null && clip != null) sfxSource.PlayOneShot(clip);
        }

        void LoadFirstScenario()
        {
            string next = TrolleyGameState.Instance?.NextScenarioScene();
            if (string.IsNullOrEmpty(next))
            {
                Debug.LogWarning("[TutorialTrainDrill] Drill finished but no scenario order set (standalone test?).");
                return;
            }
            PilotController.Instance.LoadNewScene(next);
        }
    }
}
