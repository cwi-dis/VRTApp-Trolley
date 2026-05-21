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
    /// Controls the avatar customisation scene.
    /// Solo: Station A confirm → immediate scene load.
    /// Paired: master uses Station A, non-master uses Station B.
    /// Both must confirm before either proceeds (network sync via TrolleyAvatarReadyMessage).
    /// Body/skin/hair wiring is handled internally by each AvatarSelector.
    /// </summary>
    public class AvatarSetupController : MonoBehaviour
    {
        [Header("Station A — Solo player / Paired P1 (master)")]
        [SerializeField] GameObject stationARoot;
        [SerializeField] AvatarSelector selectorA;
        [SerializeField] Button confirmButtonA;
        [SerializeField] TextMeshProUGUI statusTextA;

        [Header("Station B — Paired P2 (non-master) only")]
        [SerializeField] GameObject stationBRoot;
        [SerializeField] AvatarSelector selectorB;
        [SerializeField] Button confirmButtonB;
        [SerializeField] TextMeshProUGUI statusTextB;

        bool _isPaired;
        bool _localConfirmed;
        bool _remoteConfirmed;

        void Awake()
        {
            if (VRTOrchestratorSingleton.Comm != null)
                VRTOrchestratorSingleton.Comm.RegisterEventType(
                    (MessageTypeID)TrolleyMsgID.AvatarReady, typeof(TrolleyAvatarReadyMessage));
        }

        void OnEnable()
        {
            VRTOrchestratorSingleton.Comm?.Subscribe<TrolleyAvatarReadyMessage>(OnAvatarReady);
        }

        void OnDisable()
        {
            VRTOrchestratorSingleton.Comm?.Unsubscribe<TrolleyAvatarReadyMessage>(OnAvatarReady);
        }

        void Start()
        {
            _isPaired = TrolleyGameState.Instance?.condition == TrolleyGameState.Condition.Paired;

            if (stationBRoot != null) stationBRoot.SetActive(_isPaired);

            if (_isPaired && VRTOrchestratorSingleton.Comm != null)
            {
                bool isMaster = VRTOrchestratorSingleton.Comm.UserIsMaster;
                // Each player can only confirm their own station
                if (confirmButtonA != null) confirmButtonA.interactable = isMaster;
                if (confirmButtonB != null) confirmButtonB.interactable = !isMaster;
            }

            if (confirmButtonA != null) confirmButtonA.onClick.AddListener(OnLocalConfirm);
            if (confirmButtonB != null) confirmButtonB.onClick.AddListener(OnLocalConfirm);

            SetStatus(statusTextA, "Customise your avatar, then press Confirm.");
            if (_isPaired) SetStatus(statusTextB, "Customise your avatar, then press Confirm.");
        }

        void OnLocalConfirm()
        {
            _localConfirmed = true;

            // Disable both confirm buttons so it cannot be pressed again
            if (confirmButtonA != null) confirmButtonA.interactable = false;
            if (confirmButtonB != null) confirmButtonB.interactable = false;

            if (_isPaired && VRTOrchestratorSingleton.Comm != null)
            {
                SetStatus(statusTextA, "Waiting for partner…");
                SetStatus(statusTextB, "Waiting for partner…");

                var state = TrolleyGameState.Instance;
                var msg = new TrolleyAvatarReadyMessage
                {
                    bodyType       = (int)(state?.avatarBodyType ?? TrolleyGameState.AvatarBodyType.Masculine),
                    skinToneIndex  = state?.skinToneIndex ?? 0,
                    hairColorIndex = state?.hairColorIndex ?? 0,
                };

                if (VRTOrchestratorSingleton.Comm.UserIsMaster)
                    VRTOrchestratorSingleton.Comm.SendTypeEventToAll(msg);
                else
                    VRTOrchestratorSingleton.Comm.SendTypeEventToMaster(msg);

                StartCoroutine(WaitForPartner());
            }
            else
            {
                LoadNext();
            }
        }

        IEnumerator WaitForPartner()
        {
            yield return new WaitUntil(() => _remoteConfirmed);
            LoadNext();
        }

        void LoadNext()
        {
            string next = TrolleyGameState.Instance?.NextScenarioScene();
            if (string.IsNullOrEmpty(next))
            {
                Debug.LogError("AvatarSetupController: no next scene in TrolleyGameState.");
                return;
            }
            if (SceneFader.Instance == null)
                new GameObject("SceneFader").AddComponent<SceneFader>();
            SceneFader.Instance.FadeToBlack(() => SceneManager.LoadScene(next));
        }

        void OnAvatarReady(TrolleyAvatarReadyMessage msg)
        {
            if (VRTOrchestratorSingleton.Comm == null) return;
            if (VRTOrchestratorSingleton.Comm.UserIsMaster)
                VRTOrchestratorSingleton.Comm.SendTypeEventToAll(msg, true);
            if (msg.SenderId == VRTOrchestratorSingleton.Comm.SelfUser?.userId) return;

            var state = TrolleyGameState.Instance;
            if (state != null)
            {
                state.remoteAvatarBodyType  = (TrolleyGameState.AvatarBodyType)msg.bodyType;
                state.remoteSkinToneIndex   = msg.skinToneIndex;
                state.remoteHairColorIndex  = msg.hairColorIndex;
            }

            _remoteConfirmed = true;
        }

        static void SetStatus(TextMeshProUGUI label, string text)
        {
            if (label != null) label.text = text;
        }
    }
}
