using UnityEngine;
using VRT.Core;
using VRT.Pilots.Common;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Add to P_Self_Player_Trolley and P_Player_Trolley.
    /// At Start(), reads the participant's TrolleyAvatarConfig from VRTPilotConfig,
    /// activates the right altRep via SetRepresentation(), and applies skin/hair tints.
    /// Config index: master (Station A) = 0, non-master (Station B) = 1.
    /// </summary>
    public class TrolleyAvatarLoader : MonoBehaviour
    {
        void Start()
        {
            if (!VRTPilotConfig.InstanceExists())
            {
                Debug.LogWarning("[TrolleyAvatarLoader] VRTPilotConfig not available — skipping.");
                return;
            }

            bool isSelf = GetComponent<PlayerControllerSelf>() != null;
            int configIndex = isSelf
                ? TrolleyGameState.LocalAvatarConfigIndex
                : TrolleyGameState.OtherAvatarConfigIndex;

            var configs = VRTPilotConfig.Instance.avatarConfigs;
            if (configs == null || configIndex >= configs.Length)
            {
                Debug.LogWarning($"[TrolleyAvatarLoader] No avatar config at index {configIndex}.");
                return;
            }
            var cfg = configs[configIndex];

            var repType = cfg.bodyType == "Feminine"
                ? UserRepresentationType.AppDefinedRepresentationTwo
                : UserRepresentationType.AppDefinedRepresentationOne;

            var controller = GetComponent<PlayerControllerBase>();
            if (controller == null)
            {
                Debug.LogError("[TrolleyAvatarLoader] No PlayerControllerBase found.");
                return;
            }
            controller.SetRepresentation(repType);

            var repGO = controller.GetRepresentationGameObject();
            if (repGO == null)
            {
                Debug.LogWarning("[TrolleyAvatarLoader] Representation GO is null after SetRepresentation.");
                return;
            }

            var appearance = repGO.GetComponent<TrolleyAvatarAppearance>();
            if (appearance == null)
            {
                Debug.LogWarning($"[TrolleyAvatarLoader] No TrolleyAvatarAppearance on {repGO.name} — tinting skipped.");
                return;
            }
            appearance.ApplyConfig(cfg);
        }
    }
}
