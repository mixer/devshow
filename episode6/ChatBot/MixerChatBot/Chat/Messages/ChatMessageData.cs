using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MixerChatBot.Chat.Messages
{
    [JsonObject]
    public class ChatMessageData
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ChatMessageType type { get; set; }

        [JsonProperty]
        public string data { get; set; }

        [JsonProperty]
        public string text { get; set; }
    }
}
