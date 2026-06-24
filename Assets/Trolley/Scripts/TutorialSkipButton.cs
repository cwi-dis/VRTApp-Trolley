using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VRT.Pilots.Common;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Researcher-only "skip the rest of this tutorial" button. Lives on a Button_Skip prefab instance
    /// (a copy of the A/B networked button) placed out of the participant's natural reach/view — behind or
    /// to the left of the seat — so only the operator presses it. Built/wired by TrolleySkipButtonSetup,
    /// which repoints the button's OnTrigger event to Skip(). Pressing it loads the next scene in the
    /// session flow immediately, exactly the step a tutorial drill takes when it finishes.
    ///
    /// Skip() is the OnTrigger target. As a fallback it also wires its own XRSimpleInteractable's
    /// selectEntered (harmless no-op if there isn't one); the _skipped guard keeps a single press from
    /// loading two scenes if both fire. It never touches TrolleyToggleDecision, the drills, or the
    /// participant's decision input.
    /// </summary>
    public class TutorialSkipButton : MonoBehaviour
    {
        [Tooltip("The button's own interactable. Auto-found in children if left unset.")]
        [SerializeField] XRSimpleInteractable interactable;

        bool _skipped;   // idempotency — one press never loads two scenes (OnTrigger + selectEntered may both fire)

        void Awake()
        {
            if (interactable == null) interactable = GetComponentInChildren<XRSimpleInteractable>(true);
        }

        // Matches the selectEntered wiring TrolleyToggleDecision uses (lambda + RemoveAllListeners).
        void OnEnable()  => interactable?.selectEntered.AddListener(_ => Skip());
        void OnDisable() => interactable?.selectEntered.RemoveAllListeners();

        // Also exposed so it can be fired from the Inspector context menu or a UnityEvent if needed.
        [ContextMenu("Skip Tutorial")]
        public void Skip()
        {
            if (_skipped) return;
            _skipped = true;

            string next = TrolleyGameState.Instance?.NextScene();
            if (string.IsNullOrEmpty(next))
            {
                Debug.LogWarning("[TutorialSkipButton] No next scene to skip to (TrolleyGameState missing or flow ended).");
                return;
            }
            Debug.Log($"[TutorialSkipButton] Researcher skip → {next}");
            PilotController.Instance.LoadNewScene(next);
        }
    }
}
