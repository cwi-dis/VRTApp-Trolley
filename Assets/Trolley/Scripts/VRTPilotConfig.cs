using System;
using UnityEngine;
using VRT.Core;

[Serializable]
public class VRTPilotConfig : MonoBehaviour
{
    [Tooltip("Where to load the configuration from")]
    public string configFilename = "pilotconfig.json";
    [Tooltip("introspection: configuration was loaded from the config file")]
    public bool wasLoaded = false;

    [Header("Researcher Configuration")]
    [Tooltip("'Solo' or 'Paired'")]
    public string condition = "";
    [Tooltip("Participant number (1-30). 0 = not set.")]
    public int participantNumber = 0;
    [Tooltip("'Stranger', 'Close', or '' (not applicable/not set)")]
    public string relationshipType = "";
    [Tooltip("Ordered scene names for the three scenarios")]
    public string[] scenarioOrder = null;
    [Tooltip("Label for logging, e.g. 'B→D→S'")]
    public string scenarioOrderLabel = "";

    /// <summary>True when all required researcher fields were loaded from the config file.</summary>
    public bool HasResearcherConfig =>
        wasLoaded &&
        participantNumber > 0 &&
        (condition == "Solo" || condition == "Paired") &&
        scenarioOrder != null && scenarioOrder.Length > 0;

    public bool IsPaired => condition == "Paired";

    static VRTPilotConfig _Instance;
    public static VRTPilotConfig Instance
    {
        get
        {
            if (_Instance == null)
            {
                Debug.LogError("VRTPilotConfig: Instance accessed before allocation. Must be on a Component that is initialized very early.");
            }
            return _Instance;
        }
    }

    public static bool InstanceExists()
    {
        return _Instance != null;
    }

    private void Awake()
    {
        if (_Instance != null)
        {
            Debug.LogWarning($"VRTPilotConfig: Awake() called but there is an Instance already from {_Instance.gameObject}. Keeping the old one.");
            Destroy(gameObject);
            return;
        }
        Initialize();
    }

    void Initialize()
    {
        _Instance = this;
        var filename = VRTConfig.ConfigFilename(configFilename);
        if (System.IO.File.Exists(filename))
        {
            JsonUtility.FromJsonOverwrite(System.IO.File.ReadAllText(filename), this);
            wasLoaded = true;
        }
        else
        {
            Debug.LogWarning($"VRTPilotConfig: file not found: {filename}");
        }
        DontDestroyOnLoad(gameObject);
    }
}
