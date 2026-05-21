using VRT.OrchestratorComm;

namespace VRT.Pilots.Trolley
{
    // App-specific message type IDs (200-299 reserved for Trolley).
    // VR2Gather's MessageTypeID enum is in a read-only package; cast ints instead.
    internal static class TrolleyMsgID
    {
        public const int TimerStart  = 200;
        public const int Action      = 201;
        public const int QuestDone   = 202;
        public const int AvatarReady = 203;
    }

    public class TrolleyTimerStartMessage : BaseMessage { }

    public class TrolleyActionMessage : BaseMessage
    {
        public string triggeredByPlayerId;
        public long   unixMs;
    }

    public class TrolleyQuestionnaireDoneMessage : BaseMessage { }

    public class TrolleyAvatarReadyMessage : BaseMessage
    {
        public int bodyType;       // TrolleyGameState.AvatarBodyType cast to int
        public int skinToneIndex;
        public int hairColorIndex;
    }
}
