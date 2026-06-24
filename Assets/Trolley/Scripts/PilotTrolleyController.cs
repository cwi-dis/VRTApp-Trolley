using Cwipc;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRT.Pilots.Common;

namespace VRT.Pilots.Trolley
{
    public class PilotTrolleyController : PilotController
    {
        void OnEnable()
        {
#if VRT_WITH_STATS
            Statistics.Output(Name(), $"scene={SceneManager.GetActiveScene().name}, started=1"); 
#endif
        }

        void OnDisable()
        {
            #if VRT_WITH_STATS
            Statistics.Output(Name(), $"scene={SceneManager.GetActiveScene().name}, stopped=1");
            #endif
        }
    }
}