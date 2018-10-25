using Newtonsoft.Json;

namespace MixerChatBot.Chat.Messages
{
    [JsonObject]
    public class ConnectionInfo
    {
        [JsonProperty]
        public string server { get; set; }
    }
}
