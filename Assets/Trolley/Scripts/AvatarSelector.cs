using UnityEngine;
using UnityEngine.UI;
using VRT.Pilots.Common;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Avatar customisation panel shown at the start of the tutorial.
    /// Participants select body type, skin tone, hair colour, and height.
    /// Body type swaps the SelfPlayerPrefab; the other dimensions are logged
    /// as covariates. Visual application of skin/hair/height requires
    /// material or blend-shape wiring on the specific avatar prefabs.
    /// </summary>
    public class AvatarSelector : MonoBehaviour
    {
        [SerializeField] GameObject selectionPanel;

        [Header("Body Type (required — swaps prefab)")]
        [SerializeField] Button masculineButton;
        [SerializeField] Button feminineButton;

        [Header("Avatar Prefabs")]
        [SerializeField] GameObject masculineSelfPrefab;
        [SerializeField] GameObject feminineSelfPrefab;

        [Header("Skin Tone (6 swatches — assign buttons left to right, index 0–5)")]
        [SerializeField] Button[] skinToneButtons;   // length 6

        [Header("Hair Colour (6 swatches — assign buttons left to right, index 0–5)")]
        [SerializeField] Button[] hairColorButtons;  // length 6

        [SerializeField] SessionPlayersManager playersManager;

        static readonly Color Selected   = new Color(0.1f, 0.6f, 0.1f);
        static readonly Color Unselected = new Color(0.2f, 0.2f, 0.5f);
        const float SwatchDimFactor = 0.4f;

        Color[] _skinToneBaseColors;
        Color[] _hairColorBaseColors;

        void Start()
        {
            // Capture swatch base colors before any highlight calls overwrite them
            _skinToneBaseColors = CaptureColors(skinToneButtons);
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

            if (selectionPanel != null) selectionPanel.SetActive(true);
        }

        public void SelectBodyType(TrolleyGameState.AvatarBodyType bodyType)
        {
            if (TrolleyGameState.Instance != null)
                TrolleyGameState.Instance.avatarBodyType = bodyType;

            if (playersManager != null)
                playersManager.SelfPlayerPrefab =
                    bodyType == TrolleyGameState.AvatarBodyType.Masculine
                        ? masculineSelfPrefab
                        : feminineSelfPrefab;

            SetHighlight(masculineButton, bodyType == TrolleyGameState.AvatarBodyType.Masculine);
            SetHighlight(feminineButton,  bodyType == TrolleyGameState.AvatarBodyType.Feminine);
        }

        public void SelectSkinTone(int index)
        {
            if (TrolleyGameState.Instance != null)
                TrolleyGameState.Instance.skinToneIndex = index;

            // TODO: apply skin tone material to avatar renderer
            HighlightSwatchGroup(skinToneButtons, _skinToneBaseColors, index);
        }

        public void SelectHairColor(int index)
        {
            if (TrolleyGameState.Instance != null)
                TrolleyGameState.Instance.hairColorIndex = index;

            // TODO: apply hair colour material to avatar renderer
            HighlightSwatchGroup(hairColorButtons, _hairColorBaseColors, index);
        }

        static void HighlightGroup(Button[] buttons, int selectedIndex)
        {
            for (int i = 0; i < buttons.Length; i++)
                SetHighlight(buttons[i], i == selectedIndex);
        }

        // For colour swatches: selected = full swatch colour, unselected = dimmed swatch colour.
        // This preserves the visual identity of each swatch instead of overwriting with a flat colour.
        static void HighlightSwatchGroup(Button[] buttons, Color[] baseColors, int selectedIndex)
        {
            if (buttons == null || baseColors == null) return;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;
                var img = buttons[i].GetComponent<Image>();
                if (img == null) continue;
                Color base_ = i < baseColors.Length ? baseColors[i] : Color.white;
                img.color = (i == selectedIndex) ? base_ : base_ * SwatchDimFactor;
            }
        }

        static void SetHighlight(Button btn, bool selected)
        {
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
