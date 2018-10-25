using Newtonsoft.Json;

namespace MixerChatBot.Chat.Messages
{
    [JsonObject]
    public class ChatDeleteMessageEvent : BaseEvent
    {
        public const string EventType = "DeleteMessage";

        [JsonProperty]
        public DeleteEventAttributionData data;
    }
}