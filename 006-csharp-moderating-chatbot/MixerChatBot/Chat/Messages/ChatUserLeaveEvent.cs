using Newtonsoft.Json;

namespace MixerChatBot.Chat.Messages
{
    [JsonObject]
    public class ChatUserLeaveEvent : BaseEvent
    {
        public const string EventType = "UserLeave";

        [JsonProperty]
        public ChatUserData data;
    }
}