using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Participant-facing "Start" button that self-paces a tutorial. The drill calls WaitForPress() once at
    /// the opening: the button becomes visible + pressable while the A/B buttons are live for a free warm-up,
    /// the drill coroutine holds until Start is pressed, then the button hides and the tutorial begins.
    ///
    /// Built/wired by TrolleyTutorialStartSetup. Like TutorialSkipButton it's local and standalone — its
    /// OnTrigger is wired to Press(); selectEntered is a fallback. It never touches the decision input.
    /// </summary>
    public class TutorialGate : MonoBehaviour
    {
        [SerializeField] XRSimpleInteractable interactable;
        bool _pressed;

        void Awake()
        {
            if (interactable == null) interactable = GetComponentInChildren<XRSimpleInteractable>(true);
            SetShown(false); // hidden until a gate opens
        }

        // Fallback to the same selectEntered path the A/B toggle uses; OnTrigger → Press() is the primary
        // wiring (set up by TrolleyTutorialStartSetup).
        void OnEnable()  => interactable?.selectEntered.AddListener(_ => Press());
        void OnDisable() => interactable?.selectEntered.RemoveAllListeners();

        [ContextMenu("Press")] public void Press() => _pressed = true;

        /// <summary>Show the button, hold until it's pressed, then hide it. Safe to call repeatedly.</summary>
        public IEnumerator WaitForPress()
        {
            _pressed = false;
            SetShown(true);
            yield return new WaitUntil(() => _pressed);
            SetShown(false);
        }

        // Keep the GameObject active (so this component stays alive and the press can register) but toggle
        // the visuals + collider + interactable, so the button only appears while a gate is open.
        void SetShown(bool shown)
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = shown;
            foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = shown;
            if (interactable != null) interactable.enabled = shown;
        }
    }
}
