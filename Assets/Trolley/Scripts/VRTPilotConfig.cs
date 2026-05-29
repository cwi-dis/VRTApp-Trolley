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
    // Add public fields here, they will be loaded from pilotconfig.json.
    
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
