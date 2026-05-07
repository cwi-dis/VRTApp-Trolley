using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using VRT.Orchestrator;
using VRT.OrchestratorComm;
namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Controls the avatar customisation scene (Phase 1, Step 3).
    /// Finds all UI references by GameObject name — no Inspector wiring needed.
    /// Solo: Confirm immediately loads the first scenario.
    /// Paired: both must confirm before either proceeds.
    /// </summary>
    public class AvatarSetupController : MonoBehaviour
    {
        bool _remoteReady = false;
        bool _isPaired    = false;

        TextMeshProUGUI _statusText;

        void Awake()
        {
            VRTOrchestratorSingleton.Comm.RegisterEventType((MessageTypeID)TrolleyMsgID.AvatarReady, typeof(TrolleyAvatarReadyMessage));
        }

        void OnEnable()
        {
            VRTOrchestratorSingleton.Comm.Subscribe<TrolleyAvatarReadyMessage>(OnAvatarReady);
        }

        void OnDisable()
        {
            VRTOrchestratorSingleton.Comm?.Unsubscribe<TrolleyAvatarReadyMessage>(OnAvatarReady);
        }

        void Start()
        {
            _isPaired = TrolleyGameState.Instance?.condition == TrolleyGameState.Condition.Paired;
            // Find UI by name
            _statusText = FindTMP("StatusText");

            var confirmBtn = FindButton("ConfirmButton");
            if (confirmBtn != null)
                confirmBtn.onClick.AddListener(OnConfirm);

            WireAvatarSelector();
            SetStatus("Customise your avatar, then press Confirm.");
        }

        // ── Avatar selector wiring ─────────────────────────────────────────

        void WireAvatarSelector()
        {
            var selector = FindObjectOfType<AvatarSelector>();
            if (selector == null) return;

            // Body type
            WireBodyButton(selector, "MasculineButton", TrolleyGameState.AvatarBodyType.Masculine);
            WireBodyButton(selector, "FeminineButton",  TrolleyGameState.AvatarBodyType.Feminine);

            // Height
            WireHeightButton(selector, "ShortButton",  TrolleyGameState.AvatarHeight.Short);
            WireHeightButton(selector, "MediumButton", TrolleyGameState.AvatarHeight.Medium);
            WireHeightButton(selector, "TallButton",   TrolleyGameState.AvatarHeight.Tall);

            // Skin tone
            for (int i = 0; i < 6; i++)
            {
                int captured = i;
                var btn = FindButton($"SkinTone_{i}");
                if (btn != null)
                    btn.onClick.AddListener(() => selector.SelectSkinTone(captured));
            }

            // Hair colour
            for (int i = 0; i < 6; i++)
            {
                int captured = i;
                var btn = FindButton($"HairColor_{i}");
                if (btn != null)
                    btn.onClick.AddListener(() => selector.SelectHairColor(captured));
            }
        }

        void WireBodyButton(AvatarSelector selector, string goName, TrolleyGameState.AvatarBodyType type)
        {
            var btn = FindButton(goName);
            if (btn != null) btn.onClick.AddListener(() => selector.SelectBodyType(type));
        }

        void WireHeightButton(AvatarSelector selector, string goName, TrolleyGameState.AvatarHeight height)
        {
            var btn = FindButton(goName);
            if (btn != null) btn.onClick.AddListener(() => selector.SelectHeight(height));
        }

        // ── Confirm flow ───────────────────────────────────────────────────

        void OnConfirm()
        {
            var confirmBtn = FindButton("ConfirmButton");
            if (confirmBtn != null) confirmBtn.interactable = false;

            if (_isPaired && VRTOrchestratorSingleton.Comm != null)
            {
                if (VRTOrchestratorSingleton.Comm.UserIsMaster)
                    VRTOrchestratorSingleton.Comm.SendTypeEventToAll(new TrolleyAvatarReadyMessage());
                else
                    VRTOrchestratorSingleton.Comm.SendTypeEventToMaster(new TrolleyAvatarReadyMessage());
                SetStatus("Waiting for your partner…");
                StartCoroutine(WaitForPartner());
            }
            else
            {
                LoadNext();
            }
        }

        IEnumerator WaitForPartner()
        {
            yield return new WaitUntil(() => _remoteReady);
            LoadNext();
        }

        void LoadNext()
        {
            string next = TrolleyGameState.Instance?.NextScenarioScene();
            if (string.IsNullOrEmpty(next))
            {
                Debug.LogError("AvatarSetupController: no next scene configured.");
                return;
            }
            if (SceneFader.Instance == null)
                new GameObject("SceneFader").AddComponent<SceneFader>();
            SceneFader.Instance.FadeToBlack(() => SceneManager.LoadScene(next));
        }

        // ── Network ────────────────────────────────────────────────────────

        void OnAvatarReady(TrolleyAvatarReadyMessage msg)
        {
            if (VRTOrchestratorSingleton.Comm.UserIsMaster)
                VRTOrchestratorSingleton.Comm.SendTypeEventToAll(msg, true);
            if (msg.SenderId == VRTOrchestratorSingleton.Comm.SelfUser?.userId) return;
            _remoteReady = true;
        }

        // ── Helpers ────────────────────────────────────────────────────────

        void SetStatus(string msg)
        {
            if (_statusText != null) _statusText.text = msg;
        }

        static Button FindButton(string goName)
        {
            var go = GameObject.Find(goName);
            return go != null ? go.GetComponent<Button>() : null;
        }

        static TextMeshProUGUI FindTMP(string goName)
        {
            var go = GameObject.Find(goName);
            return go != null ? go.GetComponent<TextMeshProUGUI>() : null;
        }
    }
}
