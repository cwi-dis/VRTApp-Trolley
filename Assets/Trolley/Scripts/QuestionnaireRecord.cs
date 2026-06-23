using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif

namespace VRT.Pilots.Trolley
{
    [Serializable]
    public class QuestionnaireEntry
    {
        public int index;
        public string questionText;
        public string answer;
    }

    /// <summary>
    /// Accumulates metadata and per-question answers for one questionnaire run,
    /// then serialises to JSON via <see cref="Save"/>.
    /// Kept separate from QuestionnaireController so the data layer is independent
    /// of whichever UI (in-VR Likert buttons, paper form, etc.) drives it.
    /// </summary>
    [Serializable]
    public class QuestionnaireRecord
    {
        public string sessionID;
        public string startedAt;
        public string scenario;
        public string decision;
        public bool practiceMode;
        public int playerIndex;
        public string bodyType;
        public string condition;
        public string relationshipType;
        public string scenarioOrder;
        public string avatarConfig;
        public string reflectionEndedAt;
        public List<QuestionnaireEntry> responses = new List<QuestionnaireEntry>();

        QuestionnaireRecord() { }

        public static QuestionnaireRecord Create(string scenario, string decision, bool practiceMode, string sessionID)
        {
            var r = new QuestionnaireRecord
            {
                sessionID = !string.IsNullOrEmpty(sessionID) ? sessionID : DateTime.Now.ToString("yyyyMMdd_HHmmss"),
                startedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                scenario  = scenario,
                decision  = decision,
                practiceMode = practiceMode,
            };

            int avatarIndex = TrolleyGameState.LocalAvatarConfigIndex;
            r.playerIndex = avatarIndex == 0 ? 1 : 2;

            var cfg = VRTPilotConfig.InstanceExists() ? VRTPilotConfig.Instance : null;
            if (cfg != null)
            {
                var rc = cfg.researcherConfig;
                var configs = cfg.avatarConfigs;
                var ac = (configs != null && avatarIndex < configs.Length) ? configs[avatarIndex] : null;
                r.bodyType          = ac?.bodyType           ?? "";
                r.condition         = rc?.condition          ?? "";
                r.relationshipType  = rc?.relationshipType   ?? "";
                r.scenarioOrder     = rc?.scenarioOrderLabel ?? "";
                r.avatarConfig      = ac?.ToLogString()      ?? "";
            }
            return r;
        }

        public void MarkReflectionEnded()
        {
            reflectionEndedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        public void AddResponse(int index, string questionText, string answer)
        {
            responses.Add(new QuestionnaireEntry { index = index, questionText = questionText, answer = answer });
        }

        /// <summary>
        /// Expand {var} tokens in <paramref name="template"/> and resolve the path via
        /// VRTConfig.ConfigFilename so the output folder follows the same rule as other
        /// session files.  Supported tokens: {session}, {scenario}, {player}, {time}.
        /// </summary>
        public string Save(string filenameTemplate)
        {
            string filename = filenameTemplate
                .Replace("{session}",  sessionID)
                .Replace("{scenario}", scenario)
                .Replace("{player}",   playerIndex.ToString())
                .Replace("{time}",     DateTime.Now.ToString("yyyyMMdd-HHmm"));

            string path = VRT.Core.VRTConfig.ConfigFilename(filename, force: true, label: "Questionnaire");
            File.WriteAllText(path, JsonUtility.ToJson(this, prettyPrint: true), Encoding.UTF8);
            Debug.Log($"[Questionnaire] saved {responses.Count} responses to {path}");
#if VRT_WITH_STATS
            Statistics.Output("Questionnaire",
                $"questionnaire_file={filename}, scenario={scenario}, player={playerIndex}");
#endif
            return path;
        }
    }
}
