using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Singleton that writes decision and questionnaire data to CSV.
    /// Call StartSession() from TutorialController when Begin Study is pressed.
    /// If exportEnabled is false, data is only logged to the console.
    /// CSV columns include all session metadata so each row is self-contained.
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

        /// <summary>
        /// Call once when the researcher clicks Begin Study.
        /// Creates CSV files with headers if export is enabled.
        /// </summary>
        public void StartSession()
        {
            _sessionID = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _sessionStarted = true;

            if (!_exportEnabled) return;

            string dir = Application.persistentDataPath;
            _decisionPath      = Path.Combine(dir, $"decisions_{_sessionID}.csv");
            _questionnairePath = Path.Combine(dir, $"questionnaire_{_sessionID}.csv");

            WriteHeader(_decisionPath,
                "timestamp,sessionID,participantNumber,gender,condition,relationshipType," +
                "scenarioOrder,scenario,decision,triggeredByPlayerID,responseTimeMs");

            WriteHeader(_questionnairePath,
                "timestamp,sessionID,participantNumber,gender,condition,relationshipType," +
                "scenarioOrder,scenario,questionIndex,questionText,answer");

            Debug.Log($"DataLogger: export ON — writing to {dir}");
        }

        public void LogDecision(string scenario, string decision,
                                string triggeredBy, float responseTimeSec)
        {
            string line =
                $"{Now()},{_sessionID},{Meta()},{scenario}," +
                $"{decision},{triggeredBy},{Mathf.RoundToInt(responseTimeSec * 1000)}";

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
                    WriteHeader(path, "timestamp,sessionID,participantNumber,gender,condition,relationshipType,scenarioOrder,scenario,decision,audioFile");
                AppendLine(path, line);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        string Meta()
        {
            var gs = TrolleyGameState.Instance;
            if (gs == null) return ",,,,";
            return $"{gs.participantNumber},{gs.localGender},{gs.condition}," +
                   $"{gs.relationshipType},{CSV(gs.scenarioOrderLabel)}";
        }

        void WriteHeader(string path, string header) =>
            File.WriteAllText(path, header + "\n", Encoding.UTF8);

        void AppendLine(string path, string line) =>
            File.AppendAllText(path, line + "\n", Encoding.UTF8);

        string Now() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string CSV(string s) => $"\"{s?.Replace("\"", "\"\"") ?? ""}\"";
    }
}
