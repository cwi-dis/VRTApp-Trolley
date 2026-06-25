using VRT.OrchestratorComm;

namespace VRT.Pilots.Trolley
{
    // App-specific message type IDs (200-299 reserved for Trolley).
    // VR2Gather's MessageTypeID enum is in a read-only package; cast ints instead.
    internal static class TrolleyMsgID
    {
        public const int TimerStart  = 200;
        // 201 was Action — removed when toggle A/B replaced physical button
        // 202 was QuestDone — replaced by BarrierController + NetworkTrigger
        public const int AvatarReady = 203;
    }

    public class TrolleyTimerStartMessage : BaseMessage { }

    public class TrolleyAvatarUpdateMessage : BaseMessage
    {
        public int bodyType;       // TrolleyAvatarConfig.AvatarBodyType cast to int
        public int skinToneIndex;
        public int hairColorIndex;
        public bool isDone;        // true = Confirm pressed; false = live selection change
    }
}
