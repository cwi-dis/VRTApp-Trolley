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
    /// Wire OnGazeEnter / OnGazeExit for additional behaviour (e.g. NetworkTrigger).
    /// </summary>
    public class GazeTarget : MonoBehaviour
    {
        [SerializeField, Tooltip("Fired when the local player starts gazing at this object.")]
        UnityEvent m_OnGazeEnter;

        [SerializeField, Tooltip("Fired when the local player stops gazing at this object.")]
        UnityEvent m_OnGazeExit;

        void Start()
        {
#if VRT_WITH_STATS
            Statistics.Output("GazeTarget", $"gazing=0, target={gameObject.name}");
#endif
        }

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
