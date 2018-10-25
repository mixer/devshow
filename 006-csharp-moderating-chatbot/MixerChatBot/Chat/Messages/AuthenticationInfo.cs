using Newtonsoft.Json;

namespace MixerChatBot.Chat.Messages
{
    [JsonObject]
    public class AuthenticationInfo
    {
        [JsonProperty]
        public bool authenticated { get; set; }

        [JsonProperty]
        public string[] roles { get; set; }
    }
}
