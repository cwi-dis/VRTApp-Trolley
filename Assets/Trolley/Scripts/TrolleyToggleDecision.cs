using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Two-button toggle for Bystander scene.
    /// Button A = inaction (default, highlighted at start).
    /// Button B = action.
    /// Either can be pressed during the decision window to switch state.
    /// TrolleyController reads IsAction when the timer expires to determine final decision.
    /// </summary>
    public class TrolleyToggleDecision : MonoBehaviour
    {
        [Header("Buttons — assign OBJ_NetworkButton_A and _B")]
        [SerializeField] GameObject buttonA;
        [SerializeField] GameObject buttonB;

        [Header("Colors")]
        [SerializeField] Color colorDefault = new Color(0.35f, 0.35f, 0.35f); // unselected
        [SerializeField] Color colorActive  = new Color(0.2f, 0.75f, 0.2f);   // selected

        [Header("Interactables — drag XRSimpleInteractable from each button object")]
        [SerializeField] XRSimpleInteractable interactableA;
        [SerializeField] XRSimpleInteractable interactableB;

        [Header("Renderers — assign the Button mesh renderer on each")]
        [SerializeField] Renderer rendererA;
        [SerializeField] Renderer rendererB;

        [Header("Monitor Rims — assign rim quad for Track A and Track B monitors")]
        [SerializeField] GameObject rimA;
        [SerializeField] GameObject rimB;

        public bool IsAction { get; private set; } = false; // A (inaction) is selected by default

        public event Action<bool> OnToggled; // fires with new isAction value — use for network broadcast

        void Awake()
        {
            if (buttonA != null)
            {
                if (interactableA == null) interactableA = buttonA.GetComponentInChildren<XRSimpleInteractable>(true);
                if (rendererA == null)     rendererA     = FindButtonRenderer(buttonA);
            }
            if (buttonB != null)
            {
                if (interactableB == null) interactableB = buttonB.GetComponentInChildren<XRSimpleInteractable>(true);
                if (rendererB == null)     rendererB     = FindButtonRenderer(buttonB);
            }
            Debug.Log($"[ToggleDecision] Awake — rendererA={rendererA}, rendererB={rendererB}, interactableA={interactableA}, interactableB={interactableB}");
        }

        static Renderer FindButtonRenderer(GameObject root)
        {
            // First try child named "Button"
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name.Contains("Button")) { var r = child.GetComponent<Renderer>(); if (r != null) return r; }
            // Fallback: first MeshRenderer anywhere in children
            return root.GetComponentInChildren<MeshRenderer>(true);
        }

        void OnEnable()
        {
            interactableA?.selectEntered.AddListener(_ => PressA());
            interactableB?.selectEntered.AddListener(_ => PressB());
        }

        void OnDisable()
        {
            interactableA?.selectEntered.RemoveAllListeners();
            interactableB?.selectEntered.RemoveAllListeners();
        }

        void Start()
        {
            SetInteractionEnabled(false);
            RefreshMaterials();
        }

        public void SetInteractionEnabled(bool enabled)
        {
            if (interactableA != null) interactableA.enabled = enabled;
            if (interactableB != null) interactableB.enabled = enabled;
        }

        // Called by TrolleyController to apply a remote partner's toggle.
        public void ApplyRemoteState(bool isAction)
        {
            IsAction = isAction;
            RefreshMaterials();
        }

        public void PressA()
        {
            if (!IsAction) return; // already A, no change
            IsAction = false;
            RefreshMaterials();
            Debug.Log("[ToggleDecision] Pressed A (Track A) — decision: inaction");
            OnToggled?.Invoke(false);
        }

        public void PressB()
        {
            if (IsAction) return; // already B, no change
            IsAction = true;
            RefreshMaterials();
            Debug.Log("[ToggleDecision] Pressed B (Track B) — decision: action");
            OnToggled?.Invoke(true);
        }

        void RefreshMaterials()
        {
            SetColor(rendererA, IsAction ? colorDefault : colorActive);
            SetColor(rendererB, IsAction ? colorActive  : colorDefault);

            // Green rim on the monitor matching the current decision
            if (rimA != null) rimA.SetActive(!IsAction); // rimA on when A (inaction) selected
            if (rimB != null) rimB.SetActive(IsAction);  // rimB on when B (action) selected
        }

        static void SetColor(Renderer rend, Color col)
        {
            if (rend == null) return;
            var mat = rend.material; // creates per-instance material
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);
        }
    }
}
