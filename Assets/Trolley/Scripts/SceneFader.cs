using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Scene-local fader: fades the screen to/from black mid-scene (e.g. to hide a hidden
    /// world reset, or to mask the moment of hitting rocks/workers near the end of a scene).
    /// Lives in whichever scene needs it (added via Trolley > Add Scene Fader) and is destroyed
    /// with that scene like any other GameObject — it does NOT persist across scene loads and
    /// does not fade at scene start (that's handled by VR2Gather's own CameraFader). Code that
    /// wants to fade should check SceneFader.Instance for null and skip the effect if no fader
    /// is present.
    /// Uses a World Space canvas so it works in both editor and XR headset.
    /// </summary>
    public class SceneFader : MonoBehaviour
    {
        public static SceneFader Instance { get; private set; }

        [Tooltip("When true, no canvas is created and no visual effect is shown; only the timing and callbacks are active.")]
        [SerializeField] bool invisible = false;
        [SerializeField] float fadeDuration = 0.8f;
        [SerializeField] float canvasDistance = 0.3f;

        /// <summary>Seconds a FadeToBlack/FadeFromBlack takes — lets callers time a fade to land exactly on a moment.</summary>
        public float FadeDuration => fadeDuration;

        Transform _fadeCanvas;
        Image _overlay;
        bool _fading;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            if (!invisible) CreateOverlay();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (_overlay == null || (_overlay.color.a <= 0f && !_fading)) return;
            var cam = Camera.main;
            if (cam == null) return;
            _fadeCanvas.position = cam.transform.position + cam.transform.forward * canvasDistance;
            _fadeCanvas.rotation = cam.transform.rotation;
        }

        void CreateOverlay()
        {
            var canvasGO = new GameObject("FadeCanvas");
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 999;
            var rt = canvasGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1000f, 1000f);
            canvasGO.transform.localScale = Vector3.one * 0.002f;   // 2m x 2m — covers full FOV at 0.3m
            _fadeCanvas = canvasGO.transform;

            var imgGO = new GameObject("FadeImage");
            imgGO.transform.SetParent(canvasGO.transform, false);
            var imgRect = imgGO.AddComponent<RectTransform>();
            imgRect.anchorMin = Vector2.zero;
            imgRect.anchorMax = Vector2.one;
            imgRect.offsetMin = imgRect.offsetMax = Vector2.zero;
            _overlay = imgGO.AddComponent<Image>();
            _overlay.color = new Color(0f, 0f, 0f, 0f);
            _overlay.raycastTarget = false;
        }

        public void FadeToBlack(Action onComplete)
        {
            StartCoroutine(DoFade(0f, 1f, onComplete));
        }

        /// <summary>Fade from black back to clear. Use after a hidden in-scene reset (e.g. the driver
        /// tutorial snapping the world back between practice reps).</summary>
        public void FadeFromBlack(Action onComplete)
        {
            StartCoroutine(DoFade(1f, 0f, onComplete));
        }

        IEnumerator DoFade(float from, float to, Action onComplete)
        {
            _fading = true;
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                if (_overlay != null)
                {
                    Color c = _overlay.color;
                    c.a = Mathf.Lerp(from, to, t / fadeDuration);
                    _overlay.color = c;
                }
                yield return null;
            }
            if (_overlay != null)
            {
                Color c = _overlay.color;
                c.a = to;
                _overlay.color = c;
            }
            _fading = false;
            onComplete?.Invoke();
        }
    }
}
