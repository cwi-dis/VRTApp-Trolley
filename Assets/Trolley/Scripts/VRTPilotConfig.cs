using System;
using UnityEngine;
using VRT.Core;

[Serializable]
public class TrolleyResearcherConfig
{
    [Tooltip("'Solo' or 'Paired'")]
    public string condition = "";
    [Tooltip("'Stranger', 'Close', or '' (not applicable/not set)")]
    public string relationshipType = "";
    [Tooltip("Ordered scene names for the three scenarios")]
    public string[] scenarioOrder = null;
    [Tooltip("Label for logging, e.g. 'B→D→S'")]
    public string scenarioOrderLabel = "";

    public bool HasConfig =>
        (condition == "Solo" || condition == "Paired") &&
        scenarioOrder != null && scenarioOrder.Length > 0;

    public bool IsPaired => condition == "Paired";
}

[Serializable]
public class VRTPilotConfig : MonoBehaviour
{
    [Tooltip("Where to load the configuration from")]
    public string configFilename = "pilotconfig.json";
    [Tooltip("introspection: configuration was loaded from the config file")]
    public bool wasLoaded = false;

    [Header("Researcher Configuration")]
    public TrolleyResearcherConfig researcherConfig = new TrolleyResearcherConfig();

    /// <summary>True when all required researcher fields were loaded from the config file.</summary>
    public bool HasResearcherConfig => wasLoaded && researcherConfig.HasConfig;

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

#if UNITY_EDITOR
    [ContextMenu("Save as pilotconfig.json")]
    private void SaveAsPilotConfigJson()
    {
        string file = VRTConfig.ConfigFilename("pilotconfig.json", force: true);
        System.IO.File.WriteAllText(file, JsonUtility.ToJson(this, true));
        Debug.Log($"VRTPilotConfig: Saved to {file}");
    }
#endif

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
