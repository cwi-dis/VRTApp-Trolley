using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace VRT.Pilots.Trolley
{
    /// <summary>
    /// Singleton that writes decision and questionnaire data to CSV files in
    /// Application.persistentDataPath. Survives scene transitions.
    /// </summary>
    public class DataLogger : MonoBehaviour
    {
        public static DataLogger Instance { get; private set; }

        string _sessionID;
        string _decisionPath;
        string _questionnairePath;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _sessionID = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string dir = Application.persistentDataPath;
            _decisionPath = Path.Combine(dir, $"decisions_{_sessionID}.csv");
            _questionnairePath = Path.Combine(dir, $"questionnaire_{_sessionID}.csv");
            WriteHeader(_decisionPath,
                "timestamp,sessionID,condition,scenario,decision,triggeredByPlayerID,responseTimeMs");
            WriteHeader(_questionnairePath,
                "timestamp,sessionID,condition,scenario,questionIndex,questionText,answer");
            Debug.Log($"DataLogger: writing to {dir}");
        }

        public void LogDecision(string scenario, string decision, string triggeredBy, float responseTimeSec)
        {
            string condition = TrolleyGameState.Instance?.condition.ToString() ?? "unknown";
            AppendLine(_decisionPath,
                $"{Now()},{_sessionID},{condition},{scenario},{decision},{triggeredBy},{Mathf.RoundToInt(responseTimeSec * 1000)}");
        }

        public void LogQuestionnaireAnswer(string scenario, int questionIndex, string questionText, string answer)
        {
            string condition = TrolleyGameState.Instance?.condition.ToString() ?? "unknown";
            AppendLine(_questionnairePath,
                $"{Now()},{_sessionID},{condition},{scenario},{questionIndex},{CSV(questionText)},{CSV(answer)}");
        }

        void WriteHeader(string path, string header) =>
            File.WriteAllText(path, header + "\n", Encoding.UTF8);

        void AppendLine(string path, string line) =>
            File.AppendAllText(path, line + "\n", Encoding.UTF8);

        string Now() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string CSV(string s) => $"\"{s.Replace("\"", "\"\"")}\"";
    }
}
