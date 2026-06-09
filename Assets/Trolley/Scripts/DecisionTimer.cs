using System;
using UnityEngine;
using TMPro;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// World-space countdown timer. Shown during the decision window.
    /// Fires OnTimerExpired when time runs out (inaction outcome).
    /// </summary>
    public class DecisionTimer : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI timerText;
        [SerializeField] float duration = 8f;
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color urgentColor = Color.red;
        [SerializeField] float urgentThreshold = 2f;
        [SerializeField] float hudDistance = 2f;

        public event Action OnTimerExpired;

        bool _running;
        float _elapsed;

        void Awake()
        {
            SetVisible(false);
        }

        public void StartCountdown()
        {
            if (_running) return;
            _elapsed = 0f;
            _running = true;
            SetVisible(true);
        }

        public void Stop()
        {
            _running = false;
            SetVisible(false);
        }

        /// <summary>Returns elapsed seconds since countdown started.</summary>
        public float GetElapsedTime() => _elapsed;

        void Update()
        {
            if (!_running) return;
            _elapsed += Time.deltaTime;
            float remaining = Mathf.Max(0f, duration - _elapsed);
            if (timerText != null)
            {
                timerText.text = remaining.ToString("F1");
                timerText.color = remaining <= urgentThreshold ? urgentColor : normalColor;
            }
            if (_elapsed >= duration)
            {
                _running = false;
                SetVisible(false);
                OnTimerExpired?.Invoke();
            }
        }

        void SetVisible(bool visible)
        {
            if (timerText != null) timerText.gameObject.SetActive(visible);
            if (visible) PositionInFrontOfCamera();
        }

        void PositionInFrontOfCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            transform.position = cam.transform.position + cam.transform.forward * hudDistance;
            transform.rotation = cam.transform.rotation;
        }
    }
}
