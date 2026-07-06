using System.Collections;
using UnityEngine;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Driver scene: the player sits in a stationary cab and the whole environment
    /// (this GameObject — TrackEnvironment) moves instead.
    ///
    ///  • Approach  → slide straight toward the player (no rotation, so nothing tilts).
    ///  • Action    → yaw the environment about the player's seat (DivertMarker) at the
    ///                rate of travelling along the branch arc (turn rate = speed / radius),
    ///                up to the arc's total angle. Pure Y rotation — no pitch/roll tilt.
    ///  • Inaction  → keep rolling straight through the outcome.
    ///
    /// Whichever outcome occurs, the workers the tram runs into are hidden shortly after
    /// the decision so they don't clip into the cab.
    ///
    /// Tying the turn rate to speed/radius (rather than a fixed duration) makes the tilt
    /// match actually travelling the curved rail, instead of snapping to the final angle.
    /// </summary>
    public class DriverTrainController : TrainControllerBase
    {
        [Header("Approach")]
        [Tooltip("World-space direction the environment slides during the approach. " +
                 "Default -Z moves it toward a player seated near the origin.")]
        [SerializeField] Vector3 approachDirection = Vector3.back;
        [SerializeField] float approachSpeed = 9.5f;

        [Header("Divert (action)")]
        [Tooltip("Pivot the environment rotates about during the action divert — the player's seat " +
                 "(DivertMarker). Defaults to world origin if unset.")]
        [SerializeField] Transform divertPivot;
        [Tooltip("Total turn of the branch arc. 90° for a quarter-circle switch. " +
                 "Sign sets the turn direction — flip if it turns the wrong way.")]
        [SerializeField] float branchTurnAngle = -90f;
        [Tooltip("Radius of the branch arc (world units). Turn rate = approachSpeed / radius, so the " +
                 "tilt matches travelling along the arc. Smaller = sharper/faster turn.")]
        [SerializeField] float branchRadius = 79.3f;

        [Header("Hit workers (hidden on impact)")]
        [Tooltip("Workers on the action/branch track — hidden after an ACTION divert so they don't clip into the cab.")]
        [SerializeField] GameObject actionHitWorkers;
        [Tooltip("Workers on the straight track — hidden after an INACTION outcome.")]
        [SerializeField] GameObject inactionHitWorkers;
        [Tooltip("A hit worker group is hidden the instant it comes within this many world units of the " +
                 "player's seat (divertPivot) — i.e. the moment the cab actually reaches it. This is " +
                 "distance-based on purpose: it fires at the true impact for BOTH the straight and the " +
                 "divert outcome and at any decision window, so no per-outcome time tuning is needed. " +
                 "Distance from the workers' visual centre to the seat at which they vanish: LOWER it if " +
                 "workers vanish too early (before reaching the cab), RAISE it if a worker clips in first.")]
        [SerializeField] float hideRadius = 5f;

        [Header("Impact effect / fade timing")]
        [Tooltip("Seconds after the decision at which the scene fade goes black and the impact effect (if any) " +
                 "fires. NOTE: this no longer controls when the hit workers are hidden — that is distance-based " +
                 "via hideRadius. It only times the fade-to-black and the optional impact burst below.")]
        [SerializeField] float hitDelay = 2f;
        [Tooltip("Activated hitDelay seconds after the chosen outcome. Self-harm: the dust/impact burst " +
                 "when the tram hits the obstacle. Leave null in the Driver scene.")]
        [SerializeField] GameObject actionImpactEffect;
        [Tooltip("Which outcome triggers the impact effect. Driver/Self-harm crash is on the ACTION (divert) outcome.")]
        [SerializeField] bool impactOnAction = true;
        [Tooltip("If a SceneFader is present: fade back in after the impact swap. Leave off to stay black " +
                 "through the rest of the outcome window and into the scene transition.")]
        [SerializeField] bool fadeBackInAfterImpact = false;

        bool _approaching;
        bool _diverting;
        float _turnedSoFar;   // degrees accumulated during the divert

        TrolleyTimingConfig _cfg;

        // Speed/delay tuned at the reference window, scaled to the active decision window so a longer
        // window slows the approach (and pushes the impact later) — keeps the cab from overrunning the
        // still-visible workers. Falls back to the raw serialized values when no config asset exists.
        float ApproachSpeed => approachSpeed * (_cfg != null ? _cfg.SpeedFactor : 1f);
        float HitDelay      => hitDelay      * (_cfg != null ? _cfg.TimeFactor  : 1f);

        void Awake() => _cfg = TrolleyTimingConfig.Load();

        public override void DoStartApproach() => _approaching = true;

        public override void ExecuteAction()
        {
            _diverting = true;
            _turnedSoFar = 0f;
            StartCoroutine(HideWorkersRoutine(actionHitWorkers));
            StartCoroutine(ImpactRoutine(impactOnAction));
        }

        public override void ExecuteInaction()
        {
            // Keep rolling straight through the outcome.
            StartCoroutine(HideWorkersRoutine(inactionHitWorkers));
            StartCoroutine(ImpactRoutine(!impactOnAction));
        }

        // Hide the workers the tram runs into the instant the cab actually reaches them, so they never clip
        // through it. Distance-based (not a timer): the worker groups are children of the moving environment
        // and close on the stationary seat (divertPivot) as it slides/diverts, so we just watch their world
        // distance to the seat and hide them when it drops below hideRadius. This fires at the true impact for
        // BOTH the straight and the divert outcome, and at any decision window, with no per-outcome tuning.
        // Deliberately INDEPENDENT of the scene fade, which fades the whole scene on its own schedule below.
        IEnumerator HideWorkersRoutine(GameObject workers)
        {
            if (workers == null) yield break;
            // Measure from the workers' visual centre (combined renderer bounds), NOT the group's pivot —
            // the pivot can sit well ahead of the actual meshes, which would hide them seconds too early.
            var renderers = workers.GetComponentsInChildren<Renderer>(true);
            while (true)
            {
                Vector3 seat = divertPivot != null ? divertPivot.position : Vector3.zero;
                if (Vector3.Distance(WorkersCentre(workers, renderers), seat) <= hideRadius) break;
                yield return null;
            }
            workers.SetActive(false);
        }

        // World-space centre of the worker meshes (falls back to the group transform if it has no renderers).
        static Vector3 WorkersCentre(GameObject workers, Renderer[] renderers)
        {
            if (renderers == null || renderers.Length == 0) return workers.transform.position;
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b.center;
        }

        // Fade-to-black starts as soon as the decision locks in (HitDelay - FadeDuration seconds before
        // impact), so the screen is fully black by the moment of the straight-outcome impact. The impact
        // effect (if any) triggers under that black. By default the screen then stays black through the
        // rest of the outcome window and into the scene transition, which VR2Gather's own CameraFader fades
        // out of — set fadeBackInAfterImpact to fade back in instead. No-ops the fade itself if no
        // SceneFader is present in the scene. Worker-hiding is handled separately by HideWorkersRoutine.
        IEnumerator ImpactRoutine(bool playImpactEffect)
        {
            var fader = SceneFader.Instance;

            float preDelay = fader != null ? Mathf.Max(0f, HitDelay - fader.FadeDuration) : HitDelay;
            yield return new WaitForSeconds(preDelay);

            if (fader != null)
            {
                bool done = false;
                fader.FadeToBlack(() => done = true);
                yield return new WaitUntil(() => done);
            }

            if (playImpactEffect && actionImpactEffect != null)
            {
                actionImpactEffect.SetActive(true);
                var ps = actionImpactEffect.GetComponent<ParticleSystem>();
                if (ps != null) ps.Play();
            }

            if (fader != null && fadeBackInAfterImpact)
            {
                bool done = false;
                fader.FadeFromBlack(() => done = true);
                yield return new WaitUntil(() => done);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;

            if (_approaching)
                transform.Translate(approachDirection.normalized * (ApproachSpeed * dt), Space.World);

            if (_diverting)
            {
                // Heading changes by (distance / radius) radians as we travel the arc,
                // so the tilt rate stays locked to the forward speed and rail curvature.
                float distance = ApproachSpeed * dt;
                float stepDeg = (distance / Mathf.Max(branchRadius, 0.01f)) * Mathf.Rad2Deg;

                float remaining = Mathf.Abs(branchTurnAngle) - _turnedSoFar;
                if (stepDeg > remaining) stepDeg = remaining;
                _turnedSoFar += stepDeg;

                Vector3 pivot = divertPivot != null ? divertPivot.position : Vector3.zero;
                transform.RotateAround(pivot, Vector3.up, stepDeg * Mathf.Sign(branchTurnAngle));

                if (_turnedSoFar >= Mathf.Abs(branchTurnAngle))
                    _diverting = false; // full branch angle reached; keep rolling forward
            }
        }
    }
}
