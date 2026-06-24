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
