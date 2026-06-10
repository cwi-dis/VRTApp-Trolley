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
        /// <summary>Begin the run-in toward the decision point (called when narration ends).</summary>
        public abstract void StartApproach();

        /// <summary>Apply the ACTION outcome — divert onto the branch track.</summary>
        public abstract void ExecuteAction();

        /// <summary>Apply the INACTION outcome — continue straight.</summary>
        public abstract void ExecuteInaction();
    }
}
