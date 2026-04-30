using System;
using System.Collections;
using UnityEngine;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Plays a sequence of AudioClips before the decision timer starts.
    /// Falls back to a timed placeholder when no clips are assigned.
    /// </summary>
    public class NarrationPlayer : MonoBehaviour
    {
        [SerializeField] AudioSource audioSource;
        [Tooltip("Narration clips played in order. Assign scenario-specific audio here.")]
        [SerializeField] AudioClip[] clips;
        [Tooltip("Seconds to wait when no clips are assigned (placeholder mode).")]
        [SerializeField] float placeholderDuration = 4f;

        public event Action OnNarrationComplete;

        public void Play()
        {
            if (clips != null && clips.Length > 0 && clips[0] != null)
                StartCoroutine(PlaySequence());
            else
                StartCoroutine(PlaceholderDelay());
        }

        IEnumerator PlaySequence()
        {
            foreach (var clip in clips)
            {
                if (clip == null) continue;
                audioSource.clip = clip;
                audioSource.Play();
                yield return new WaitForSeconds(clip.length + 0.2f);
            }
            OnNarrationComplete?.Invoke();
        }

        IEnumerator PlaceholderDelay()
        {
            Debug.LogWarning($"NarrationPlayer: no clips assigned — using {placeholderDuration}s placeholder");
            yield return new WaitForSeconds(placeholderDuration);
            OnNarrationComplete?.Invoke();
        }
    }
}
