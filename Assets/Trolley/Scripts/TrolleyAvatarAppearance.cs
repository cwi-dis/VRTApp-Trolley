using UnityEngine;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Attach to each avatar prefab root (P_Avatar_Trolley_Male, P_Avatar_Trolley_Female).
    /// Populate skinToneColors and hairColors in the Inspector to match AvatarSelector's swatches.
    /// Called by TrolleyAvatarLoader after the avatar is activated.
    /// </summary>
    public class TrolleyAvatarAppearance : MonoBehaviour
    {
        [Header("Skin Tone Colors (0=lightest, 5=darkest) — must match AvatarSelector")]
        [SerializeField] Color[] skinToneColors;
        [Header("Hair Colors (0=black … 5=grey) — must match AvatarSelector")]
        [SerializeField] Color[] hairColors;

        static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
        static readonly int ColorID     = Shader.PropertyToID("_Color");

        public void ApplyConfig(TrolleyAvatarConfig cfg)
        {
            TintChild("Body", skinToneColors, cfg.skinToneIndex);
            TintChild("Hair", hairColors,     cfg.hairColorIndex);
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
