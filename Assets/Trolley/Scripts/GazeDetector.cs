using UnityEngine;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Casts a ray each frame from the local player's head forward and notifies GazeTarget
    /// components when the player starts or stops looking at them.
    /// Place on the camera or head GameObject of the local player (PlayerControllerSelf hierarchy).
    /// </summary>
    public class GazeDetector : MonoBehaviour
    {
        [SerializeField, Tooltip("Transform to cast from. Defaults to Camera.main if unset.")]
        Transform m_GazeOrigin;

        [SerializeField, Tooltip("Layer mask containing GazeTarget colliders.")]
        LayerMask m_GazeTargetLayer;

        [SerializeField, Tooltip("Maximum gaze detection distance in metres.")]
        float m_MaxDistance = 20f;

        [SerializeField, Tooltip("SphereCast radius in metres. Small value adds forgiveness over a pure raycast.")]
        float m_SphereRadius = 0.05f;

        GazeTarget m_CurrentTarget;

        void Start()
        {
            if (m_GazeOrigin == null)
                m_GazeOrigin = Camera.main?.transform;
            if (m_GazeOrigin == null)
                Debug.LogWarning($"[GazeDetector] No gaze origin set and Camera.main not found.");
        }

        void Update()
        {
            if (m_GazeOrigin == null) return;

            GazeTarget newTarget = null;
            if (Physics.SphereCast(m_GazeOrigin.position, m_SphereRadius, m_GazeOrigin.forward,
                    out RaycastHit hit, m_MaxDistance, m_GazeTargetLayer))
                newTarget = hit.collider.GetComponent<GazeTarget>();

            if (newTarget == m_CurrentTarget) return;

            m_CurrentTarget?.NotifyGazeExit();
            m_CurrentTarget = newTarget;
            m_CurrentTarget?.NotifyGazeEnter();
        }

        void OnDisable()
        {
            m_CurrentTarget?.NotifyGazeExit();
            m_CurrentTarget = null;
        }
    }
}
