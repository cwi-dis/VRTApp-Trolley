using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Singleton that fades the screen to/from black between scene transitions.
    /// Uses a World Space canvas so it works in both editor and XR headset.
    /// Create via: new GameObject("SceneFader").AddComponent<SceneFader>()
    /// </summary>
    public class SceneFader : MonoBehaviour
    {
        public static SceneFader Instance { get; private set; }

        [SerializeField] float fadeDuration = 0.8f;
        [SerializeField] float canvasDistance = 0.3f;
        [Tooltip("Seconds to hold black before fading in on scene load.")]
        [SerializeField] float introHoldDuration = 2f;

        public event Action OnFadeInComplete;

        Transform _fadeCanvas;
        Image _overlay;
        bool _fading;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateOverlay();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

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

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(IntroFadeIn());
        }

        IEnumerator IntroFadeIn()
        {
            Color c = _overlay.color;
            c.a = 1f;
            _overlay.color = c;
            yield return new WaitForSeconds(introHoldDuration);
            yield return DoFade(1f, 0f, null);
            OnFadeInComplete?.Invoke();
        }

        IEnumerator DoFade(float from, float to, Action onComplete)
        {
            _fading = true;
            float t = 0f;
            Color c = _overlay.color;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                c.a = Mathf.Lerp(from, to, t / fadeDuration);
                _overlay.color = c;
                yield return null;
            }
            c.a = to;
            _overlay.color = c;
            _fading = false;
            onComplete?.Invoke();
        }
    }
}
