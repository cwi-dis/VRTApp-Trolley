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
        [Header("Audio — narration (plays once, no loop)")]
        [SerializeField] AudioSource audioSource;
        [Tooltip("Narration clips played in order.")]
        [SerializeField] AudioClip[] clips;
        [Tooltip("Seconds to wait when no clips are assigned (placeholder mode).")]
        [SerializeField] float placeholderDuration = 4f;

        public event Action OnNarrationComplete;

        public float TotalDuration
        {
            get
            {
                if (clips == null || clips.Length == 0 || clips[0] == null)
                    return placeholderDuration;
                float t = 0f;
                foreach (var c in clips)
                    if (c != null) t += c.length + 0.2f;
                return t;
            }
        }

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
                audioSource.loop = false;
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
