using UnityEngine;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Blackout overlay for Bystander scene CCTV monitors.
    /// TrolleyController calls Blackout() when the decision window closes.
    /// Each monitorOverlay is a black quad parented in front of a monitor quad, initially disabled.
    /// </summary>
    public class CCTVBlackout : MonoBehaviour
    {
        [SerializeField] GameObject[] monitorOverlays;

        public void Blackout()
        {
            foreach (var overlay in monitorOverlays)
                if (overlay != null) overlay.SetActive(true);
        }
    }
}
