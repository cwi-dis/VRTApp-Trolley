using UnityEngine;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Attach to the Remy avatar root in each scenario scene.
    /// Reads TrolleyGameState at Start() and applies skin/hair tints via MaterialPropertyBlock.
    /// Set isRemotePlayer = false on the local player's Remy, true on the remote player's Remy.
    /// Colors must match the values set in AvatarSelector (wired by TrolleyAvatarSetupSceneSetup).
    /// </summary>
    public class AvatarApplier : MonoBehaviour
    {
        [SerializeField] bool isRemotePlayer;

        [Header("Skin Tone Colors (0=lightest, 5=darkest) — must match AvatarSelector")]
        [SerializeField] Color[] skinToneColors;

        [Header("Hair Colors (0=black … 5=grey) — must match AvatarSelector")]
        [SerializeField] Color[] hairColors;

        static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
        static readonly int ColorID     = Shader.PropertyToID("_Color");

        void Start() => Apply();

        public void Apply()
        {
            var state = TrolleyGameState.Instance;
            if (state == null) return;

            int skinIdx = isRemotePlayer ? state.remoteSkinToneIndex  : state.skinToneIndex;
            int hairIdx = isRemotePlayer ? state.remoteHairColorIndex : state.hairColorIndex;

            TintChild("Body", skinToneColors, skinIdx);
            TintChild("Hair", hairColors,     hairIdx);
        }

        void TintChild(string childName, Color[] colors, int index)
        {
            if (colors == null || index >= colors.Length) return;
            SkinnedMeshRenderer smr = null;
            foreach (var r in GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (r.gameObject.name == childName) { smr = r; break; }
            if (smr == null) return;

            var mpb = new MaterialPropertyBlock();
            smr.GetPropertyBlock(mpb);
            int propID = smr.sharedMaterial != null && smr.sharedMaterial.HasProperty(BaseColorID)
                ? BaseColorID : ColorID;
            mpb.SetColor(propID, colors[index]);
            smr.SetPropertyBlock(mpb);
        }
    }
}
