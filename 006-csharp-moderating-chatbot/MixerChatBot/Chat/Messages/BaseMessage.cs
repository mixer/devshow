using Newtonsoft.Json;

namespace MixerChatBot.Chat.Messages
{
    [JsonObject]
    public class BaseMessage
    {
        [JsonProperty(PropertyName = "type")]
        public string type { get; set; }
    }
}
