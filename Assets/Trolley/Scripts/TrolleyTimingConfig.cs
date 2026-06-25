using UnityEngine;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Single source of truth for the scenario timing, shared by every scene.
    ///
    /// The decision window is the master knob. Train speed and the worker-hide / impact delay are NOT
    /// stored here — each scene keeps its own hand-tuned value (the value that looked right at
    /// <see cref="referenceWindow"/>) and scales it by the ratios below. That keeps the choreography in
    /// sync no matter the window: a longer window proportionally slows the train and pushes the impact
    /// later, so the cab never overruns the still-visible workers.
    ///
    /// Lives in a Resources folder and is loaded by name (<see cref="Load"/>), so no scene wiring is
    /// needed. Create/select it via  Trolley > Create or Select Timing Config.
    ///
    /// Pilot use: change <see cref="decisionWindow"/> only (8 / 10 / 12 …) between conditions; everything
    /// else follows. After the pilot, leave the winning value in place.
    /// </summary>
    [CreateAssetMenu(fileName = "TrolleyTimingConfig", menuName = "Trolley/Timing Config")]
    public class TrolleyTimingConfig : ScriptableObject
    {
        [Header("Master knob")]
        [Tooltip("Decision window in seconds. The independent variable. Everything else derives from this.")]
        public float decisionWindow = 8f;

        [Header("Scaling reference")]
        [Tooltip("The window the per-scene speeds / delays were tuned at. Speeds scale by " +
                 "referenceWindow / decisionWindow. Leave at 8 unless you re-tune the scenes.")]
        public float referenceWindow = 8f;

        [Tooltip("Global speed multiplier on top of the window scaling (1 = unchanged, 0.8 = 20% slower " +
                 "everywhere). Slows/speeds the train without touching per-scene values or the window.")]
        [Range(0.25f, 2f)]
        public float speedScale = 1f;

        /// <summary>The window actually in effect: the researcher's value from the pilot config
        /// (pilotconfig.json → VRTPilotConfig.researcherConfig.decisionWindow) if set (>0), otherwise this
        /// asset's baked default. Sourcing it from the config file is what lets the window change on-device
        /// without a rebuild.</summary>
        public float EffectiveWindow
        {
            get
            {
                if (VRTPilotConfig.InstanceExists())
                {
                    float w = VRTPilotConfig.Instance.researcherConfig.decisionWindow;
                    if (w > 0f) return w;
                }
                return decisionWindow;
            }
        }

        /// <summary>Scales a speed tuned at referenceWindow to the effective window (and global speedScale).</summary>
        public float SpeedFactor => (referenceWindow / Mathf.Max(EffectiveWindow, 0.01f)) * speedScale;

        /// <summary>Scales a "time to cover a fixed distance" value (e.g. hitDelay) — the inverse of speed.</summary>
        public float TimeFactor => (EffectiveWindow / Mathf.Max(referenceWindow, 0.01f)) / Mathf.Max(speedScale, 0.01f);

        /// <summary>Loads the shared config from Resources. Returns null if none exists (callers fall back
        /// to their own serialized values, so nothing breaks).</summary>
        public static TrolleyTimingConfig Load() => Resources.Load<TrolleyTimingConfig>("TrolleyTimingConfig");
    }
}
