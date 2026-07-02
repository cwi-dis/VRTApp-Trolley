using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Participant-facing "Start Tutorial" control that self-paces a tutorial. The drill calls
    /// WaitForPress() once at the opening: the control becomes visible while the A/B buttons are live for a
    /// free warm-up, the drill coroutine holds until it's pressed, then it hides and the tutorial begins.
    ///
    /// Preferred form is a world-space UI Button (uiButton + visualRoot), built by TrolleyTutorialStartSetup.
    /// A physical XRSimpleInteractable is still supported as a fallback. Never touches the decision input.
    /// </summary>
    public class TutorialGate : MonoBehaviour
    {
        [Tooltip("World-space UI Start button (preferred). Its onClick is wired to Press() at runtime.")]
        [SerializeField] Button uiButton;
        [Tooltip("Root shown/hidden with the gate — e.g. the Start-button canvas. When set, only this is " +
                 "toggled; otherwise the renderers/colliders on this object are toggled (physical fallback).")]
        [SerializeField] GameObject visualRoot;
        [Tooltip("Physical XR button (legacy fallback). Optional.")]
        [SerializeField] XRSimpleInteractable interactable;

        bool _pressed;

        void Awake()
        {
            if (interactable == null) interactable = GetComponentInChildren<XRSimpleInteractable>(true);
            if (uiButton != null) uiButton.onClick.AddListener(Press);
            SetShown(false); // hidden until a gate opens
        }

        // Fallback to the same selectEntered path the A/B toggle uses; OnTrigger → Press() (physical button)
        // and uiButton.onClick → Press() (UI button) are the primary wirings.
        void OnEnable()  => interactable?.selectEntered.AddListener(_ => Press());
        void OnDisable() => interactable?.selectEntered.RemoveAllListeners();

        [ContextMenu("Press")] public void Press() => _pressed = true;

        /// <summary>Show the control, hold until it's pressed, then hide it. Safe to call repeatedly.</summary>
        public IEnumerator WaitForPress()
        {
            _pressed = false;
            SetShown(true);
            yield return new WaitUntil(() => _pressed);
            SetShown(false);
        }

        // UI form: toggle the canvas root. Physical form: toggle visuals + collider + interactable so the
        // button only appears while a gate is open (this GameObject stays active either way).
        void SetShown(bool shown)
        {
            if (visualRoot != null) { visualRoot.SetActive(shown); return; }
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = shown;
            foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = shown;
            if (interactable != null) interactable.enabled = shown;
        }
    }
}
