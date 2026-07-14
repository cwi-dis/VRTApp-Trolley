using UnityEngine;
using UnityEngine.Events;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Marks a GameObject as a gaze target. Requires a collider on the GazeTarget physics layer.
    /// Logs stats records when the local player starts and stops looking at this object.
    /// Also logs the world position at start and whenever it changes (for distance computations).
    /// Wire OnGazeEnter / OnGazeExit for additional behaviour (e.g. NetworkTrigger).
    ///
    /// Also unconditionally logs a closing gazing=0 (and final position) on OnDisable, so a gaze
    /// span never survives this object being disabled or destroyed (e.g. its scene unloading).
    /// Without this, GazeDetector never gets a chance to notice the target disappeared, so a gaze
    /// still open at that moment looks (in the stats log) like it lasts until the next time an
    /// object with the same name happens to be gazed at -- however much later that is. See
    /// VRTApp-Trolley#94 / TrolleyExperiment's session-solo-02 notes.
    /// </summary>
    public class GazeTarget : MonoBehaviour
    {
        [SerializeField, Tooltip("Fired when the local player starts gazing at this object.")]
        UnityEvent m_OnGazeEnter;

        [SerializeField, Tooltip("Fired when the local player stops gazing at this object.")]
        UnityEvent m_OnGazeExit;

        [SerializeField, Tooltip("How often to check and log position changes, in milliseconds. 0 disables periodic logging.")]
        float m_PositionLogIntervalMs = 200f;

        Vector3 m_LastLoggedPosition;
        float m_TimeSinceLastLog;

        void Start()
        {
            m_LastLoggedPosition = transform.position;
            m_TimeSinceLastLog = 0f;
#if VRT_WITH_STATS
            Statistics.Output("GazeTarget", $"gazing=0, target={gameObject.name}");
            LogPosition();
#endif
        }

        void OnDisable()
        {
#if VRT_WITH_STATS
            Statistics.Output("GazeTarget", $"gazing=0, target={gameObject.name}");
            LogPosition();
#endif
        }

        void Update()
        {
            if (m_PositionLogIntervalMs <= 0f) return;
            m_TimeSinceLastLog += Time.deltaTime * 1000f;
            if (m_TimeSinceLastLog < m_PositionLogIntervalMs) return;
            m_TimeSinceLastLog = 0f;
            Vector3 pos = transform.position;
            if (pos == m_LastLoggedPosition) return;
            m_LastLoggedPosition = pos;
#if VRT_WITH_STATS
            LogPosition();
#endif
        }

#if VRT_WITH_STATS
        void LogPosition()
        {
            Vector3 pos = transform.position;
            Statistics.Output("GazeTarget", $"pos_x={pos.x:F2}, pos_y={pos.y:F2}, pos_z={pos.z:F2}, target={gameObject.name}");
        }
#endif

        public void NotifyGazeEnter()
        {
#if VRT_WITH_STATS
            Statistics.Output("GazeTarget", $"gazing=1, target={gameObject.name}");
#endif
            m_OnGazeEnter?.Invoke();
        }

        public void NotifyGazeExit()
        {
#if VRT_WITH_STATS
            Statistics.Output("GazeTarget", $"gazing=0, target={gameObject.name}");
#endif
            m_OnGazeExit?.Invoke();
        }
    }
}
