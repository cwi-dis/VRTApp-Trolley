using UnityEngine;
using UnityEngine.UI;
using VRT.Pilots.Common;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Avatar customisation for one participant station.
    /// Body type swaps the visible preview model and the VR2Gather SelfPlayerPrefab.
    /// Skin tone and hair colour swap sharedMaterial on the assigned renderers in real time.
    /// Assign skinToneMaterials[6] and hairColorMaterials[6] in the Inspector.
    /// Assign bodyRenderers and hairRenderers after inspecting the FBX hierarchy.
    /// </summary>
    public class AvatarSelector : MonoBehaviour
    {
        [Header("Body Type Buttons")]
        [SerializeField] Button masculineButton;
        [SerializeField] Button feminineButton;

        [Header("VR2Gather Prefabs (body type swap on confirm)")]
        [SerializeField] GameObject masculineSelfPrefab;
        [SerializeField] GameObject feminineSelfPrefab;
        [SerializeField] SessionPlayersManager playersManager;

        [Header("Preview Avatar (3D models facing participant)")]
        [SerializeField] GameObject masculinePreview;
        [SerializeField] GameObject femininePreview;

        [Header("Preview Renderers — assign after inspecting FBX hierarchy")]
        [Tooltip("Body mesh renderers from both preview models")]
        [SerializeField] Renderer[] bodyRenderers;
        [Tooltip("Hair mesh renderers from both preview models")]
        [SerializeField] Renderer[] hairRenderers;

        [Header("Skin Tone Materials (index 0–5, lightest to darkest)")]
        [SerializeField] Material[] skinToneMaterials;

        [Header("Hair Colour Materials (index 0–5)")]
        [SerializeField] Material[] hairColorMaterials;

        [Header("Skin Tone Swatch Buttons (6 — left to right)")]
        [SerializeField] Button[] skinToneButtons;

        [Header("Hair Colour Swatch Buttons (6 — left to right)")]
        [SerializeField] Button[] hairColorButtons;

        static readonly Color Selected      = new Color(0.1f, 0.6f, 0.1f);
        static readonly Color Unselected    = new Color(0.2f, 0.2f, 0.5f);
        const float SwatchDimFactor = 0.4f;

        Color[] _skinToneBaseColors;
        Color[] _hairColorBaseColors;

        void Start()
        {
            _skinToneBaseColors  = CaptureColors(skinToneButtons);
            _hairColorBaseColors = CaptureColors(hairColorButtons);

            if (masculineButton != null)
                masculineButton.onClick.AddListener(() => SelectBodyType(TrolleyGameState.AvatarBodyType.Masculine));
            if (feminineButton != null)
                feminineButton.onClick.AddListener(() => SelectBodyType(TrolleyGameState.AvatarBodyType.Feminine));

            if (skinToneButtons != null)
                for (int i = 0; i < skinToneButtons.Length; i++)
                {
                    if (skinToneButtons[i] == null) continue;
                    int captured = i;
                    skinToneButtons[i].onClick.AddListener(() => SelectSkinTone(captured));
                }

            if (hairColorButtons != null)
                for (int i = 0; i < hairColorButtons.Length; i++)
                {
                    if (hairColorButtons[i] == null) continue;
                    int captured = i;
                    hairColorButtons[i].onClick.AddListener(() => SelectHairColor(captured));
                }

            SelectBodyType(TrolleyGameState.AvatarBodyType.Masculine);
        }

        public void SelectBodyType(TrolleyGameState.AvatarBodyType bodyType)
        {
            if (TrolleyGameState.Instance != null)
                TrolleyGameState.Instance.avatarBodyType = bodyType;

            bool isMasc = bodyType == TrolleyGameState.AvatarBodyType.Masculine;
            if (masculinePreview != null) masculinePreview.SetActive(isMasc);
            if (femininePreview  != null) femininePreview.SetActive(!isMasc);

            if (playersManager != null)
                playersManager.SelfPlayerPrefab = isMasc ? masculineSelfPrefab : feminineSelfPrefab;

            SetHighlight(masculineButton, isMasc);
            SetHighlight(feminineButton,  !isMasc);
        }

        public void SelectSkinTone(int index)
        {
            Debug.Log($"[AvatarSelector] SelectSkinTone({index}) — bodyRenderers:{bodyRenderers?.Length} skinToneMaterials:{skinToneMaterials?.Length}");
            if (TrolleyGameState.Instance != null)
                TrolleyGameState.Instance.skinToneIndex = index;
            HighlightSwatchGroup(skinToneButtons, _skinToneBaseColors, index);
            SwapMaterial(bodyRenderers, skinToneMaterials, index);
        }

        public void SelectHairColor(int index)
        {
            if (TrolleyGameState.Instance != null)
                TrolleyGameState.Instance.hairColorIndex = index;
            HighlightSwatchGroup(hairColorButtons, _hairColorBaseColors, index);
            SwapMaterial(hairRenderers, hairColorMaterials, index);
        }

        [ContextMenu("Debug: Log Wiring")]
        void DebugLogWiring()
        {
            Debug.Log($"[AvatarSelector] skinToneButtons: {skinToneButtons?.Length}, hairColorButtons: {hairColorButtons?.Length}");
            Debug.Log($"[AvatarSelector] skinToneMaterials: {skinToneMaterials?.Length}, hairColorMaterials: {hairColorMaterials?.Length}");
            Debug.Log($"[AvatarSelector] bodyRenderers: {bodyRenderers?.Length}, hairRenderers: {hairRenderers?.Length}");
            if (bodyRenderers != null)
                foreach (var r in bodyRenderers)
                    Debug.Log($"  bodyRenderer: {(r != null ? r.gameObject.name : "NULL")} sharedMaterial={r?.sharedMaterial?.name}");
            if (skinToneMaterials != null)
                for (int i = 0; i < skinToneMaterials.Length; i++)
                    Debug.Log($"  skinToneMaterials[{i}]: {(skinToneMaterials[i] != null ? skinToneMaterials[i].name : "NULL")}");
        }

        static void SwapMaterial(Renderer[] renderers, Material[] materials, int index)
        {
            if (renderers == null || materials == null || index >= materials.Length) return;
            var mat = materials[index];
            foreach (var r in renderers)
            {
                if (r == null || mat == null) continue;
                r.sharedMaterial = mat;
            }
        }

        static void HighlightSwatchGroup(Button[] buttons, Color[] baseColors, int selectedIndex)
        {
            if (buttons == null || baseColors == null) return;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;
                var img = buttons[i].GetComponent<Image>();
                if (img == null) continue;
                Color base_ = i < baseColors.Length ? baseColors[i] : Color.white;
                img.color = i == selectedIndex ? base_ : base_ * SwatchDimFactor;
            }
        }

        static void SetHighlight(Button btn, bool selected)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = selected ? Selected : Unselected;
        }

        static Color[] CaptureColors(Button[] buttons)
        {
            if (buttons == null) return new Color[0];
            var colors = new Color[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                var img = buttons[i] != null ? buttons[i].GetComponent<Image>() : null;
                colors[i] = img != null ? img.color : Color.white;
            }
            return colors;
        }
    }
}
