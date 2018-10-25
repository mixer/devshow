using Newtonsoft.Json;

namespace MixerChatBot.Chat.Messages
{
    [JsonObject]
    public class ChatMessageEvent : BaseEvent
    {
        public const string EventType = "ChatMessage";

        [JsonProperty]
        public ChatMessageInfo data;
    }
}
