using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using VRT.Orchestrator;
using VRT.OrchestratorComm;
using VRT.Pilots.Common;

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

        [Header("Scene Transition")]
        [SerializeField] NetworkTrigger readyTrigger;
        [SerializeField] BarrierController transitionBarrier;
        [SerializeField] NetworkTrigger proceedTrigger;

        bool _isPaired;

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
            _isPaired = VRTPilotConfig.InstanceExists() && VRTPilotConfig.Instance.IsPaired;

            readyTrigger.OnTrigger.AddListener(transitionBarrier.Trigger);
            transitionBarrier.OnAllReady.AddListener(proceedTrigger.Trigger);
            proceedTrigger.OnTrigger.AddListener(ExecuteLoad);

            if (stationBRoot != null) stationBRoot.SetActive(_isPaired);

            if (_isPaired && VRTOrchestratorSingleton.Comm != null)
            {
                bool isMaster = VRTOrchestratorSingleton.Comm.UserIsMaster;
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
            }

            readyTrigger.Trigger();
        }

        void ExecuteLoad()
        {
            string next = TrolleyGameState.Instance?.NextScenarioScene();
            if (string.IsNullOrEmpty(next))
            {
                Debug.LogError("AvatarSetupController: no next scene in TrolleyGameState.");
                return;
            }
            PilotController.Instance.LoadNewScene(next);
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
        }

        static void SetStatus(TextMeshProUGUI label, string text)
        {
            if (label != null) label.text = text;
        }
    }
}
