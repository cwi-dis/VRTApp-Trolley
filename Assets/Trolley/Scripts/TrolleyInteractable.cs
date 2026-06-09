using System;
using UnityEngine;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Base class for decision buttons. Enforces single-trigger guard
    /// and active/inactive gating. Subclasses call TriggerDecision() when
    /// the physical interaction threshold is met.
    /// </summary>
    public abstract class TrolleyInteractable : MonoBehaviour
    {
        public event Action OnTriggered;

        bool _active;
        bool _triggered;

        /// <summary>Enable or disable this interactable (called by TrolleyController).</summary>
        public void SetActive(bool active)
        {
            _active = active;
            _triggered = false;
            OnActiveChanged(active);
        }

        protected virtual void OnActiveChanged(bool active) { }

        protected void TriggerDecision()
        {
            if (!_active || _triggered) return;
            _triggered = true;
            _active = false;
            OnTriggered?.Invoke();
        }
    }
}
