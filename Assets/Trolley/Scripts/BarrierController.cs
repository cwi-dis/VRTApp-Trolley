using UnityEngine;
using UnityEngine.Events;
using VRT.Orchestrator;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Counts Trigger() calls arriving via NetworkTrigger and fires OnAllReady on the
    /// session master once all required participants have checked in.
    ///
    /// Wire-up pattern (done in MonoBehaviour.Start via AddListener):
    ///   readyTrigger.OnTrigger   -> barrier.Trigger
    ///   barrier.OnAllReady       -> proceedTrigger.Trigger
    ///   proceedTrigger.OnTrigger -> ExecuteSceneLoad (or whatever action)
    ///
    /// Only the session master counts; non-master calls to Trigger() are silently
    /// ignored because NetworkTrigger already routes all signals through the master.
    /// In solo (no session) mode the single Trigger() call fires OnAllReady immediately.
    ///
    /// Intended for back-port to the VR2Gather package (VRT.Pilots.Common).
    /// </summary>
    public class BarrierController : MonoBehaviour
    {
        [Tooltip("Number of Trigger() calls required before OnAllReady fires. " +
                 "0 = number of users currently in the session.")]
        public int requiredCount = 0;

        [Tooltip("Fired on the session master when all required triggers have arrived.")]
        public UnityEvent OnAllReady;

        int _count;
        bool _released;

        public void Trigger()
        {
            var comm = VRTOrchestratorSingleton.Comm;
            bool hasSession = comm?.SelfUser != null;
            if (hasSession && !comm.UserIsMaster) return;

            if (_released) return;
            _count++;
            int needed = GetNeededCount();
            Debug.Log($"[BarrierController] {name}: {_count}/{needed}");
            if (_count >= needed)
            {
                _released = true;
                OnAllReady.Invoke();
            }
        }

        public void ResetBarrier()
        {
            _count = 0;
            _released = false;
        }

        int GetNeededCount()
        {
            if (requiredCount > 0) return requiredCount;
            int n = VRTOrchestratorSingleton.Comm?.CurrentSession?.sessionUsers?.Length ?? 1;
            return Mathf.Max(n, 1);
        }
    }
}
