using UnityEngine;
using UnityEngine.UI;
using VRT.Pilots.Common;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Simple gender-based avatar selection panel shown at the start of the tutorial.
    /// Researcher sets this for each participant before the session begins.
    /// Swaps the SelfPlayerPrefab on SessionPlayersManager to the appropriate avatar.
    /// </summary>
    public class AvatarSelector : MonoBehaviour
    {
        [SerializeField] GameObject selectionPanel;
        [SerializeField] Button maleButton;
        [SerializeField] Button femaleButton;

        [Header("Avatar Prefabs (assign Mixamo-based prefabs)")]
        [SerializeField] GameObject maleSelfPrefab;
        [SerializeField] GameObject femaleSelfPrefab;

        [SerializeField] SessionPlayersManager playersManager;

        void Start()
        {
            maleButton.onClick.AddListener(() => Select(TrolleyGameState.Gender.Male));
            femaleButton.onClick.AddListener(() => Select(TrolleyGameState.Gender.Female));
            selectionPanel.SetActive(true);
        }

        void Select(TrolleyGameState.Gender gender)
        {
            if (TrolleyGameState.Instance != null)
                TrolleyGameState.Instance.localGender = gender;

            if (playersManager != null)
                playersManager.SelfPlayerPrefab =
                    gender == TrolleyGameState.Gender.Male ? maleSelfPrefab : femaleSelfPrefab;

            selectionPanel.SetActive(false);
        }
    }
}
