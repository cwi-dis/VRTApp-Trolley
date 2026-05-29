using System.Collections;
using UnityEngine;

namespace VRT.Pilots.Trolley
{
    public class TrainController : MonoBehaviour
    {
        [Header("Train")]
        [SerializeField] Transform train;
        [Tooltip("Rotate the model to match its visual forward. Set to 180 if the train faces -Z.")]
        [SerializeField] float modelForwardYaw = 180f;

        [Header("Path")]
        [SerializeField] Transform startPoint;
        [SerializeField] Transform endPoint;
        [Tooltip("Where the train redirects on action. Leave empty for scenes with no divert (Bystander).")]
        [SerializeField] Transform actionEndPoint;

        [Header("Timing")]
        [Tooltip("Total travel time from start to end point. Should match the decision window duration.")]
        [SerializeField] float decisionWindowSeconds = 8f;

        [Header("Audio — ambient train sound (loops while train moves)")]
        [SerializeField] AudioSource ambientAudioSource;

        Vector3 _currentTarget;
        float   _speed;

        public void StartApproach()
        {
            if (train != null && startPoint != null)
                train.position = startPoint.position;

            if (ambientAudioSource != null) { ambientAudioSource.loop = true; ambientAudioSource.Play(); }
            StartCoroutine(MoveTrain(decisionWindowSeconds));
        }

        public void ExecuteAction()
        {
            Debug.Log($"[TrainController] ExecuteAction — actionEndPoint={actionEndPoint}");
            if (actionEndPoint == null) return;
            StopAllCoroutines();
            StartCoroutine(MoveToTarget(actionEndPoint.position));
        }

        public void ExecuteInaction() { }

        IEnumerator MoveTrain(float totalDuration)
        {
            if (train == null || startPoint == null || endPoint == null) yield break;

            _speed = Vector3.Distance(startPoint.position, endPoint.position) / totalDuration;

            Vector3 dir = (endPoint.position - startPoint.position).normalized;
            if (dir != Vector3.zero)
                train.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, modelForwardYaw, 0f);

            yield return StartCoroutine(MoveToTarget(endPoint.position));
        }

        IEnumerator MoveToTarget(Vector3 target)
        {
            while (Vector3.Distance(train.position, target) > 0.05f)
            {
                Vector3 moveDir = (target - train.position).normalized;
                if (moveDir != Vector3.zero)
                    train.rotation = Quaternion.Slerp(train.rotation,
                        Quaternion.LookRotation(moveDir) * Quaternion.Euler(0f, modelForwardYaw, 0f),
                        5f * Time.deltaTime);

                train.position = Vector3.MoveTowards(train.position, target, _speed * Time.deltaTime);
                yield return null;
            }

            train.position = target;
            if (ambientAudioSource != null) ambientAudioSource.Stop();
        }
    }
}
