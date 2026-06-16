using System;
using UnityEngine;
using VRT.Core;

[Serializable]
public class TrolleyResearcherConfig
{
    public enum Condition { Solo, Paired }
    public enum RelationshipType { NotApplicable, Stranger, Close }

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

    public void Validate(string context)
    {
        if (!string.IsNullOrEmpty(condition) && condition != "Solo" && condition != "Paired")
            Debug.LogWarning($"{context}: invalid condition '{condition}' — expected 'Solo' or 'Paired'");
        if (!string.IsNullOrEmpty(relationshipType) && relationshipType != "Stranger" && relationshipType != "Close")
            Debug.LogWarning($"{context}: invalid relationshipType '{relationshipType}' — expected 'Stranger', 'Close', or ''");
        if (!HasConfig)
            Debug.LogWarning($"{context}: researcher config incomplete — condition='{condition}' scenarioOrder={scenarioOrder?.Length ?? 0} entries");
    }
}

[Serializable]
public class TrolleyAvatarConfig
{
    public enum AvatarBodyType { Masculine, Feminine }

    [Tooltip("'Masculine' or 'Feminine'")]
    public string bodyType = "";
    [Tooltip("Skin tone swatch index, 0-5")]
    public int skinToneIndex = 0;
    [Tooltip("Hair colour swatch index, 0-5")]
    public int hairColorIndex = 0;

    public string ToLogString() => $"body:{bodyType},skin:{skinToneIndex},hair:{hairColorIndex}";
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

    [Header("Avatar Configuration (index 0 = player 1 / master / Station A, index 1 = player 2 / non-master / Station B)")]
    public TrolleyAvatarConfig[] avatarConfigs = new TrolleyAvatarConfig[2] { new TrolleyAvatarConfig(), new TrolleyAvatarConfig() };

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
        // Stopgap for issue #55: VRTConfig.ConfigFilename() only learns the run folder
        // (from the -vrt-config command-line arg) as a side effect of being asked for
        // "config.json". If VRTPilotConfig.Awake() races ahead of VRTConfig.Awake(), that
        // side effect hasn't happened yet and pilotconfig.json resolves against the wrong
        // folder. Calling ConfigFilename() with its default ("config.json") here forces that
        // side effect regardless of Awake() order; it's a no-op if VRTConfig already did it.
        // The real fix belongs in the nl.cwi.dis.vr2gather package (VRTConfig.cs).
        VRTConfig.ConfigFilename();
        var filename = VRTConfig.ConfigFilename(configFilename);
        if (System.IO.File.Exists(filename))
        {
            JsonUtility.FromJsonOverwrite(System.IO.File.ReadAllText(filename), this);
            wasLoaded = true;
            researcherConfig.Validate($"VRTPilotConfig ({filename})");
        }
        else
        {
            Debug.LogWarning($"VRTPilotConfig: file not found: {filename}");
        }
        DontDestroyOnLoad(gameObject);
    }
}
