using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Singleton that writes decision and questionnaire data to CSV.
    /// Call StartSession() from ResearcherSetupController when Begin Study is pressed.
    /// If exportEnabled is false, data is only logged to the console.
    /// </summary>
    public class DataLogger : MonoBehaviour
    {
        public static DataLogger Instance { get; private set; }

        bool _exportEnabled = false;
        bool _sessionStarted = false;
        string _sessionID;
        string _decisionPath;
        string _questionnairePath;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetExportEnabled(bool enabled) => _exportEnabled = enabled;

        public void StartSession()
        {
            _sessionID = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _sessionStarted = true;
            if (!_exportEnabled) return;

            string dir = Application.persistentDataPath;
            _decisionPath      = Path.Combine(dir, $"decisions_{_sessionID}.csv");
            _questionnairePath = Path.Combine(dir, $"questionnaire_{_sessionID}.csv");

            WriteHeader(_decisionPath,
                "timestamp,sessionID,playerIndex,bodyType,condition,relationshipType," +
                "scenarioOrder,avatarConfig,scenario,decision,responseTimeMs," +
                "narrationEndTimestamp,windowStartTimestamp,windowEndTimestamp,buttonPresses");

            WriteHeader(_questionnairePath,
                "timestamp,sessionID,playerIndex,bodyType,condition,relationshipType," +
                "scenarioOrder,avatarConfig,scenario,questionIndex,questionText,answer");

            Debug.Log($"DataLogger: export ON — writing to {dir}");
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
            if (_exportEnabled && _sessionStarted)
                AppendLine(_decisionPath, line);
        }

        public void LogQuestionnaireAnswer(string scenario, int questionIndex,
                                           string questionText, string answer)
        {
            string line =
                $"{Now()},{_sessionID},{Meta()},{scenario}," +
                $"{questionIndex},{CSV(questionText)},{CSV(answer)}";

            Debug.Log($"[Questionnaire] {line}");
            if (_exportEnabled && _sessionStarted)
                AppendLine(_questionnairePath, line);
        }

        public void LogReflection(string scenario, string decision, string audioFilename)
        {
            string line =
                $"{Now()},{_sessionID},{Meta()},{scenario},{decision},{CSV(audioFilename)}";
            Debug.Log($"[Reflection] {line}");
            if (_exportEnabled && _sessionStarted)
            {
                string path = Path.Combine(Application.persistentDataPath, $"reflections_{_sessionID}.csv");
                if (!File.Exists(path))
                    WriteHeader(path,
                        "timestamp,sessionID,playerIndex,bodyType,condition,relationshipType," +
                        "scenarioOrder,avatarConfig,scenario,decision,audioFile");
                AppendLine(path, line);
            }
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
        string CSV(string s) => $"\"{s?.Replace("\"", "\"\"") ?? ""}\"";
    }
}
