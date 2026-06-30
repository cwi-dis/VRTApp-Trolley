using System;
using UnityEngine;
using TMPro;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Decision-window countdown. Drives the window timing only: fires OnTimerExpired
    /// when time runs out (inaction outcome) and exposes GetElapsedTime() for reaction-time
    /// logging. The visible HUD readout is hidden by default — the approaching train is the
    /// participant's time-pressure cue (see protocol). Set showHud to re-enable the numeric
    /// readout for debugging or piloting.
    /// </summary>
    public class DecisionTimer : MonoBehaviour
    {
        [Tooltip("Show the numeric countdown HUD to the participant. Off by default — the " +
                 "approaching train is the intended time-pressure cue. Enable only for debugging/piloting.")]
        [SerializeField] bool showHud = false;
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
            // The decision window is the pilot's independent variable — pull it from the shared config so
            // changing one asset retimes every scene. Falls back to the serialized value if no config asset.
            var cfg = TrolleyTimingConfig.Load();
            if (cfg != null) duration = cfg.EffectiveWindow;
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
            if (showHud && timerText != null)
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
            visible = visible && showHud;
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
