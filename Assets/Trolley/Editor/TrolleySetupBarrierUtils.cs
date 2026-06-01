using UnityEditor;
using UnityEngine;
using VRT.Pilots.Common;

namespace VRT.Pilots.Trolley.Editor
{
    /// <summary>
    /// Shared helper for all Trolley setup scripts: creates the three GameObjects
    /// that implement a coordinated scene-transition barrier and wires them to the
    /// named fields on a controller's SerializedObject.
    ///
    /// Expected controller fields (all [SerializeField]):
    ///   NetworkTrigger    readyTrigger
    ///   BarrierController transitionBarrier
    ///   NetworkTrigger    proceedTrigger
    /// </summary>
    public static class TrolleySetupBarrierUtils
    {
        public static readonly string[] ObjectNames =
            { "TransitionReadyTrigger", "TransitionBarrier", "TransitionProceedTrigger" };

        public static void AddTransitionBarrier(SerializedObject controllerSO, string menuItem)
        {
            var readyGO = new GameObject("TransitionReadyTrigger");
            readyGO.AddComponent<ManagedBySetupScript>().menuItem = menuItem;
            var ready = readyGO.AddComponent<NetworkTrigger>();

            var barrierGO = new GameObject("TransitionBarrier");
            barrierGO.AddComponent<ManagedBySetupScript>().menuItem = menuItem;
            var barrier = barrierGO.AddComponent<BarrierController>();

            var proceedGO = new GameObject("TransitionProceedTrigger");
            proceedGO.AddComponent<ManagedBySetupScript>().menuItem = menuItem;
            var proceed = proceedGO.AddComponent<NetworkTrigger>();

            controllerSO.FindProperty("readyTrigger").objectReferenceValue      = ready;
            controllerSO.FindProperty("transitionBarrier").objectReferenceValue = barrier;
            controllerSO.FindProperty("proceedTrigger").objectReferenceValue    = proceed;

            EditorUtility.SetDirty(ready);
            EditorUtility.SetDirty(barrier);
            EditorUtility.SetDirty(proceed);
        }
    }
}
