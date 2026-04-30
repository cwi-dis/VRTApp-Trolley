using System.Collections;
using UnityEngine;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Moves the train along one of two waypoint paths depending on the decision.
    /// For the optional scenario, actionPath leads to a wall (hasWallCollision = true).
    /// </summary>
    public class TrainController : MonoBehaviour
    {
        [Header("Train")]
        [SerializeField] Transform train;
        [SerializeField] float trainSpeed = 6f;

        [Header("Action path (divert track)")]
        [SerializeField] Transform[] actionPath;

        [Header("Inaction path (default track)")]
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
        static readonly int SafeHash = Animator.StringToHash("Safe");

        public void ExecuteAction()
        {
            TriggerWorkers(inactionTrackWorkers, safe: true);
            TriggerWorkers(actionTrackWorkers, safe: !hasWallCollision);
            StartCoroutine(MoveTrain(actionPath, hitWall: hasWallCollision));
        }

        public void ExecuteInaction()
        {
            TriggerWorkers(inactionTrackWorkers, safe: false);
            TriggerWorkers(actionTrackWorkers, safe: true);
            StartCoroutine(MoveTrain(inactionPath, hitWall: false));
        }

        IEnumerator MoveTrain(Transform[] path, bool hitWall)
        {
            if (path == null || path.Length == 0 || train == null) yield break;

            foreach (var waypoint in path)
            {
                Vector3 target = waypoint.position;
                while (Vector3.Distance(train.position, target) > 0.05f)
                {
                    Vector3 dir = (target - train.position).normalized;
                    if (dir != Vector3.zero) train.rotation = Quaternion.LookRotation(dir);
                    train.position = Vector3.MoveTowards(train.position, target, trainSpeed * Time.deltaTime);
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
