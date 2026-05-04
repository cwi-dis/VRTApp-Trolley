using System.Collections;
using UnityEngine;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Moves the train in three phases:
    ///   1. Approach  — shared path leading to the fork (starts when narration ends)
    ///   2. Wait      — holds at fork until a decision is made
    ///   3. Branch    — follows action or inaction path based on the decision
    ///
    /// If approachPath is empty the train starts at the fork immediately.
    /// </summary>
    public class TrainController : MonoBehaviour
    {
        [Header("Train")]
        [SerializeField] Transform train;
        [SerializeField] float trainSpeed = 6f;
        [Tooltip("Seconds the approach path takes. Auto-calculates speed from path length. Set to 0 to use trainSpeed directly.")]
        [SerializeField] float approachDuration = 38f;

        [Header("Approach path (shared — leads to fork)")]
        [SerializeField] Transform[] approachPath;

        [Header("Action path (divert track — after fork)")]
        [SerializeField] Transform[] actionPath;

        [Header("Inaction path (default track — after fork)")]
        [SerializeField] Transform[] inactionPath;

        [Header("Optional: wall collision at end of action path")]
        [SerializeField] bool hasWallCollision;
        [SerializeField] GameObject wallCollisionEffect;
        [SerializeField] AudioSource collisionAudio;

        [Header("Workers")]
        [Tooltip("Workers on the action (divert) track — endangered when action is taken")]
        [SerializeField] Animator[] actionTrackWorkers;
        [Tooltip("Workers on the inaction (default) track — endangered when no action")]
        [SerializeField] Animator[] inactionTrackWorkers;

        static readonly int DangerHash = Animator.StringToHash("Danger");
        static readonly int SafeHash   = Animator.StringToHash("Safe");

        Transform[] _decidedPath;
        bool        _hitWall;
        bool        _decisionMade;

        // Called by TrolleyController when narration ends (same moment timer starts).
        public void StartApproach()
        {
            StartCoroutine(RunTrain());
        }

        // Called immediately when a decision is made — train switches path at fork.
        public void ExecuteAction()
        {
            TriggerWorkers(inactionTrackWorkers, safe: true);
            TriggerWorkers(actionTrackWorkers, safe: !hasWallCollision);
            _decidedPath  = actionPath;
            _hitWall      = hasWallCollision;
            _decisionMade = true;
        }

        public void ExecuteInaction()
        {
            TriggerWorkers(inactionTrackWorkers, safe: false);
            TriggerWorkers(actionTrackWorkers, safe: true);
            _decidedPath  = inactionPath;
            _hitWall      = false;
            _decisionMade = true;
        }

        // ── Internal ──────────────────────────────────────────────────────────

        IEnumerator RunTrain()
        {
            // Phase 1: approach — auto-calculate speed if approachDuration is set
            if (approachPath != null && approachPath.Length > 0)
            {
                if (approachDuration > 0 && train != null)
                {
                    float len = Vector3.Distance(train.position, approachPath[0].position);
                    for (int i = 1; i < approachPath.Length; i++)
                        len += Vector3.Distance(approachPath[i - 1].position, approachPath[i].position);
                    if (len > 0) trainSpeed = len / approachDuration;
                }
                yield return StartCoroutine(FollowPath(approachPath, hitWall: false));
            }

            // Phase 2: wait at fork for decision
            yield return new WaitUntil(() => _decisionMade);

            // Phase 3: branch
            if (_decidedPath != null && _decidedPath.Length > 0)
                yield return StartCoroutine(FollowPath(_decidedPath, hitWall: _hitWall));
        }

        IEnumerator FollowPath(Transform[] path, bool hitWall)
        {
            if (train == null) yield break;

            foreach (var waypoint in path)
            {
                Vector3 target = waypoint.position;
                while (Vector3.Distance(train.position, target) > 0.05f)
                {
                    Vector3 dir = (target - train.position).normalized;
                    if (dir != Vector3.zero) train.rotation = Quaternion.LookRotation(dir);
                    train.position = Vector3.MoveTowards(
                        train.position, target, trainSpeed * Time.deltaTime);
                    yield return null;
                }
            }

            if (hitWall)
            {
                if (wallCollisionEffect != null) wallCollisionEffect.SetActive(true);
                if (collisionAudio != null) collisionAudio.Play();
            }
        }

        void TriggerWorkers(Animator[] workers, bool safe)
        {
            if (workers == null) return;
            foreach (var w in workers)
            {
                if (w == null) continue;
                w.SetTrigger(safe ? SafeHash : DangerHash);
            }
        }
    }
}
