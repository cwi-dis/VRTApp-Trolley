using UnityEngine;
using UnityEngine.UI;
using VRT.Orchestrator;
using VRT.OrchestratorComm;
using VRT.Pilots.Common;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Avatar customisation for one participant station.
    /// Writes body type / skin tone / hair colour to VRTPilotConfig.avatarConfigs[playerIndex]
    /// and calls TrolleyAvatarLoader.Reload() on the local player to update the live avatar.
    /// </summary>
    public class AvatarSelector : MonoBehaviour
    {
        [Header("Player Slot")]
        [Tooltip("Index into VRTPilotConfig.avatarConfigs — 0 = Station A / P1, 1 = Station B / P2")]
        [SerializeField] int playerIndex = 0;

        [Header("Body Type Buttons")]
        [SerializeField] Button masculineButton;
        [SerializeField] Button feminineButton;

        [Header("Skin Tone Swatch Buttons (6 — left to right)")]
        [SerializeField] Button[] skinToneButtons;

        [Header("Hair Colour Swatch Buttons (6 — left to right)")]
        [SerializeField] Button[] hairColorButtons;

        static readonly Color Selected   = new Color(0.1f, 0.6f, 0.1f);
        static readonly Color Unselected = new Color(0.2f, 0.2f, 0.5f);
        const float SwatchDimFactor = 0.4f;

        Color[] _skinToneBaseColors;
        Color[] _hairColorBaseColors;
        bool _initialized;

        TrolleyAvatarLoader _localLoader;

        TrolleyAvatarLoader LocalLoader
        {
            get
            {
                if (_localLoader != null) return _localLoader;
                if (playerIndex != TrolleyGameState.LocalAvatarConfigIndex) return null;
                var self = FindObjectOfType<PlayerControllerSelf>();
                _localLoader = self != null ? self.GetComponent<TrolleyAvatarLoader>() : null;
                return _localLoader;
            }
        }

        void Start()
        {
            _skinToneBaseColors  = CaptureColors(skinToneButtons);
            _hairColorBaseColors = CaptureColors(hairColorButtons);

            if (masculineButton != null)
                masculineButton.onClick.AddListener(() => SelectBodyType(TrolleyAvatarConfig.AvatarBodyType.Masculine));
            if (feminineButton != null)
                feminineButton.onClick.AddListener(() => SelectBodyType(TrolleyAvatarConfig.AvatarBodyType.Feminine));

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

            InitializeFromConfig();
            _initialized = true;
        }

        void InitializeFromConfig()
        {
            var cfg = GetConfig();
            var bodyType = cfg != null && cfg.bodyType == "Feminine"
                ? TrolleyAvatarConfig.AvatarBodyType.Feminine
                : TrolleyAvatarConfig.AvatarBodyType.Masculine;
            SelectBodyType(bodyType);
            SelectSkinTone(cfg?.skinToneIndex ?? 0);
            SelectHairColor(cfg?.hairColorIndex ?? 0);
        }

        TrolleyAvatarConfig GetConfig()
        {
            if (!VRTPilotConfig.InstanceExists()) return null;
            var configs = VRTPilotConfig.Instance.avatarConfigs;
            return (configs != null && playerIndex >= 0 && playerIndex < configs.Length)
                ? configs[playerIndex] : null;
        }

        public void SelectBodyType(TrolleyAvatarConfig.AvatarBodyType bodyType)
        {
            var cfg = GetConfig();
            if (cfg != null) cfg.bodyType = bodyType.ToString();

            bool isMasc = bodyType == TrolleyAvatarConfig.AvatarBodyType.Masculine;
            SetHighlight(masculineButton, isMasc);
            SetHighlight(feminineButton,  !isMasc);
            LocalLoader?.Reload();
            SendUpdate();
        }

        public void SelectSkinTone(int index)
        {
            var cfg = GetConfig();
            if (cfg != null) cfg.skinToneIndex = index;
            HighlightSwatchGroup(skinToneButtons, _skinToneBaseColors, index);
            LocalLoader?.Reload();
            SendUpdate();
        }

        public void SelectHairColor(int index)
        {
            var cfg = GetConfig();
            if (cfg != null) cfg.hairColorIndex = index;
            HighlightSwatchGroup(hairColorButtons, _hairColorBaseColors, index);
            LocalLoader?.Reload();
            SendUpdate();
        }

        void SendUpdate()
        {
            if (!_initialized || VRTOrchestratorSingleton.Comm == null) return;
            var cfg = GetConfig();
            var msg = new TrolleyAvatarUpdateMessage
            {
                bodyType       = cfg?.bodyType == "Feminine" ? 1 : 0,
                skinToneIndex  = cfg?.skinToneIndex ?? 0,
                hairColorIndex = cfg?.hairColorIndex ?? 0,
            };
            if (VRTOrchestratorSingleton.Comm.UserIsMaster)
                VRTOrchestratorSingleton.Comm.SendTypeEventToAll(msg);
            else
                VRTOrchestratorSingleton.Comm.SendTypeEventToMaster(msg);
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
