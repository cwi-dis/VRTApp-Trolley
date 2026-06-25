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
        [Tooltip("Seconds after the decision before the hit workers are hidden. Tune to the impact moment.")]
        [SerializeField] float hitDelay = 2f;

        [Header("Impact effect (optional — e.g. Self-harm rocky-mountain crash)")]
        [Tooltip("Activated hitDelay seconds after the chosen outcome. Self-harm: the dust/impact burst " +
                 "when the tram hits the obstacle. Leave null in the Driver scene.")]
        [SerializeField] GameObject actionImpactEffect;
        [Tooltip("Which outcome triggers the impact effect. Driver/Self-harm crash is on the ACTION (divert) outcome.")]
        [SerializeField] bool impactOnAction = true;

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

        public override void StartApproach() => _approaching = true;

        public override void ExecuteAction()
        {
            _diverting = true;
            _turnedSoFar = 0f;
            HideAfterDelay(actionHitWorkers);
            if (impactOnAction) PlayImpactAfterDelay();
        }

        public override void ExecuteInaction()
        {
            // Keep rolling straight through the outcome.
            HideAfterDelay(inactionHitWorkers);
            if (!impactOnAction) PlayImpactAfterDelay();
        }

        void PlayImpactAfterDelay()
        {
            if (actionImpactEffect != null) StartCoroutine(ImpactRoutine());
        }

        IEnumerator ImpactRoutine()
        {
            yield return new WaitForSeconds(HitDelay);
            actionImpactEffect.SetActive(true);
            var ps = actionImpactEffect.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play();
        }

        void HideAfterDelay(GameObject workers)
        {
            if (workers != null) StartCoroutine(HideRoutine(workers));
        }

        IEnumerator HideRoutine(GameObject workers)
        {
            yield return new WaitForSeconds(HitDelay);
            workers.SetActive(false);
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
