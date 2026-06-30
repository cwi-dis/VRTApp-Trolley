using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Singleton that writes decision data to CSV and to the stats: output.
    /// Created in the ResearcherSetup scene; survives scene loads via DontDestroyOnLoad.
    /// Session starts automatically on Awake.
    /// </summary>
    public class DataLogger : MonoBehaviour
    {
        public static DataLogger Instance { get; private set; }

        public string SessionID => _sessionID;

        string _sessionID;
        string _decisionPath;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            StartSession();
        }

        void StartSession()
        {
            _sessionID = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            string dir = Application.persistentDataPath;
            _decisionPath = Path.Combine(dir, $"decisions_{_sessionID}.csv");

            WriteHeader(_decisionPath,
                "timestamp,sessionID,playerIndex,bodyType,condition,relationshipType," +
                "scenarioOrder,avatarConfig,scenario,decision,responseTimeMs," +
                "narrationEndTimestamp,windowStartTimestamp,windowEndTimestamp,buttonPresses");

            Debug.Log($"DataLogger: writing to {_decisionPath}");
#if VRT_WITH_STATS
            Cwipc.Statistics.Output("TrolleyDataLogger",
                $"event=session_start, sessionID={_sessionID}, csvPath={_decisionPath}");
#endif
        }

        public void LogDecision(
            string scenario, string decision, float responseTimeSec,
            DateTime narrationEndTime, DateTime windowStartTime, DateTime windowEndTime,
            List<(string choice, long unixMs)> attempts = null)
        {
            string pressesStr = (attempts != null && attempts.Count > 0)
                ? string.Join("|", attempts.ConvertAll(a => $"{a.choice}@{a.unixMs}"))
                : "none";

            string line =
                $"{Now()},{_sessionID},{Meta()},{scenario}," +
                $"{decision},{Mathf.RoundToInt(responseTimeSec * 1000)}," +
                $"{Stamp(narrationEndTime)},{Stamp(windowStartTime)},{Stamp(windowEndTime)}," +
                $"{CSV(pressesStr)}";

            Debug.Log($"[Decision] {line}");
            AppendLine(_decisionPath, line);

#if VRT_WITH_STATS
            string buttonPressChoice    = attempts != null && attempts.Count > 0
                ? string.Join("|", attempts.ConvertAll(a => a.choice)) : "none";
            string buttonPressTimestamp = attempts != null && attempts.Count > 0
                ? string.Join("|", attempts.ConvertAll(a => a.unixMs.ToString())) : "none";
            Cwipc.Statistics.Output("TrolleyDataLogger",
                $"event=decision, sessionID={_sessionID}, scenario={scenario}, decision={decision}, " +
                $"narrationEndTimestamp={StampISO(narrationEndTime)}, " +
                $"windowStartTimestamp={StampISO(windowStartTime)}, " +
                $"buttonPressChoice={buttonPressChoice}, buttonPressTimestamp={buttonPressTimestamp}");
#endif
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        string Meta()
        {
            var cfg = VRTPilotConfig.InstanceExists() ? VRTPilotConfig.Instance : null;
            if (cfg == null) return ",,,,,,";

            int avatarIndex = TrolleyGameState.LocalAvatarConfigIndex;
            string playerIndex = avatarIndex == 0 ? "1" : "2";

            var rc = cfg.researcherConfig;
            var configs = cfg.avatarConfigs;
            var ac = (configs != null && avatarIndex < configs.Length) ? configs[avatarIndex] : null;

            string avatarBodyType     = ac  != null ? ac.bodyType            : "";
            string condition          = rc  != null ? rc.condition           : "";
            string relationshipType   = rc  != null ? rc.relationshipType    : "";
            string scenarioOrderLabel = rc  != null ? rc.scenarioOrderLabel  : "";
            string avatarConfig       = ac  != null ? ac.ToLogString()       : "";
            return $"{playerIndex},{avatarBodyType},{condition}," +
                   $"{relationshipType},{CSV(scenarioOrderLabel)},{CSV(avatarConfig)}";
        }

        void WriteHeader(string path, string header) =>
            File.WriteAllText(path, header + "\n", Encoding.UTF8);

        void AppendLine(string path, string line) =>
            File.AppendAllText(path, line + "\n", Encoding.UTF8);

        string Now()   => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string Stamp(DateTime dt) => dt.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string StampISO(DateTime dt) => dt.ToString("yyyy-MM-ddTHH:mm:ss.fff");
        string CSV(string s) => $"\"{s?.Replace("\"", "\"\"") ?? ""}\"";
    }
}
