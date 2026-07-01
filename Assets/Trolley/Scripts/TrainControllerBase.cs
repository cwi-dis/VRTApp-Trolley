using UnityEngine;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Shared base so TrolleyController can drive either scene's "train":
    ///  • Bystander  → TrainController        (a physical tram following a spline)
    ///  • Driver     → DriverTrainController  (the whole environment sliding past a
    ///                                          stationary player, diverting by yawing
    ///                                          about the player's seat)
    /// </summary>
    public abstract class TrainControllerBase : MonoBehaviour
    {
        /// <summary>Signal that this client is ready to begin the approach. Default: immediately executes DoStartApproach. Override to insert a sync barrier before doing so.</summary>
        public virtual void ReadyToStartApproach()
        {
#if VRT_WITH_STATS
            Cwipc.Statistics.Output("TrolleyTrainController", "event=approach_start");
#endif
            DoStartApproach();
        }

        /// <summary>Begin the run-in toward the decision point.</summary>
        public abstract void DoStartApproach();

        /// <summary>Apply the ACTION outcome — divert onto the branch track.</summary>
        public abstract void ExecuteAction();

        /// <summary>Apply the INACTION outcome — continue straight.</summary>
        public abstract void ExecuteInaction();
    }
}
