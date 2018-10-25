using Newtonsoft.Json;

namespace MixerChatBot.Chat.Messages
{
    [JsonObject]
    public class BaseEvent : BaseMessage
    {
        public const string Type = "event";

        [JsonProperty(PropertyName = "event")]
        public string Event { get; set; }
    }
}
