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

        [Header("Timing")]
        [Tooltip("Added to the narration clip length to get total travel time.")]
        [SerializeField] float decisionWindowSeconds = 8f;

        [Header("Audio — ambient train sound (loops while train moves)")]
        [SerializeField] AudioSource ambientAudioSource;

        public void StartApproach(float narrationDuration)
        {
            if (train != null && startPoint != null)
                train.position = startPoint.position;

            if (ambientAudioSource != null) { ambientAudioSource.loop = true; ambientAudioSource.Play(); }
            StartCoroutine(MoveTrain(narrationDuration + decisionWindowSeconds));
        }

        public void ExecuteAction()   { }
        public void ExecuteInaction() { }

        IEnumerator MoveTrain(float totalDuration)
        {
            if (train == null || startPoint == null || endPoint == null) yield break;

            float speed = Vector3.Distance(startPoint.position, endPoint.position) / totalDuration;
            Vector3 target = endPoint.position;
            Vector3 dir = (target - startPoint.position).normalized;

            if (dir != Vector3.zero)
                train.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, modelForwardYaw, 0f);

            while (Vector3.Distance(train.position, target) > 0.05f)
            {
                train.position = Vector3.MoveTowards(train.position, target, speed * Time.deltaTime);
                yield return null;
            }

            train.position = target;
            if (ambientAudioSource != null) ambientAudioSource.Stop();
        }
    }
}
