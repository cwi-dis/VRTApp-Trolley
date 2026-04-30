using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Lever for the bystander scenario. Grab and pull past the angle threshold to trigger.
    /// Attach to the lever root; assign leverPivot to the rotating child transform.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class TrolleyLever : TrolleyInteractable
    {
        [Tooltip("Degrees from rest rotation required to trigger the decision.")]
        [SerializeField] float pullAngleThreshold = 40f;
        [Tooltip("The transform that physically rotates when the lever is pulled.")]
        [SerializeField] Transform leverPivot;
        [Tooltip("Optional visual highlight shown when the lever is active.")]
        [SerializeField] GameObject highlightObject;

        XRGrabInteractable _grab;
        Quaternion _restRotation;
        bool _isGrabbed;

        void Awake()
        {
            _grab = GetComponent<XRGrabInteractable>();
            _grab.selectEntered.AddListener(_ => _isGrabbed = true);
            _grab.selectExited.AddListener(_ => _isGrabbed = false);
            if (leverPivot != null)
                _restRotation = leverPivot.localRotation;
        }

        protected override void OnActiveChanged(bool active)
        {
            _grab.enabled = active;
            if (highlightObject != null) highlightObject.SetActive(active);
        }

        void Update()
        {
            if (!_isGrabbed || leverPivot == null) return;
            float angle = Quaternion.Angle(leverPivot.localRotation, _restRotation);
            if (angle >= pullAngleThreshold)
                TriggerDecision();
        }
    }
}
