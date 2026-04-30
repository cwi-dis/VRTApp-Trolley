using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Physical button for the driver scenario. Press (select) to trigger.
    /// Uses XRSimpleInteractable so direct-touch controllers activate it.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class TrolleyButton : TrolleyInteractable
    {
        [Tooltip("Optional visual highlight shown when the button is active.")]
        [SerializeField] GameObject highlightObject;
        [Tooltip("Optional transform that moves down to simulate a button press.")]
        [SerializeField] Transform buttonMesh;
        [SerializeField] float pressDepth = 0.02f;

        XRSimpleInteractable _interactable;
        Vector3 _restPosition;

        void Awake()
        {
            _interactable = GetComponent<XRSimpleInteractable>();
            _interactable.selectEntered.AddListener(_ => OnPressed());
            if (buttonMesh != null)
                _restPosition = buttonMesh.localPosition;
        }

        protected override void OnActiveChanged(bool active)
        {
            _interactable.enabled = active;
            if (highlightObject != null) highlightObject.SetActive(active);
            if (buttonMesh != null) buttonMesh.localPosition = _restPosition;
        }

        void OnPressed()
        {
            if (buttonMesh != null)
                buttonMesh.localPosition = _restPosition - new Vector3(0, pressDepth, 0);
            TriggerDecision();
        }
    }
}
