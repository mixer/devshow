using Newtonsoft.Json;

namespace MixerChatBot.Chat.Messages
{
    [JsonObject]
    public class ChatUserJoinEvent : BaseEvent
    {
        public const string EventType = "UserJoin";

        [JsonProperty]
        public ChatUserData data;
    }
}