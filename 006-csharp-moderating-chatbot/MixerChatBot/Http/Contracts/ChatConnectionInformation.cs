using Newtonsoft.Json;

namespace MixerChatBot.Http.Contracts
{
    [JsonObject]
    public class ChatConnectionInformation
    {
        [JsonProperty]
        public string[] roles { get; set; }

        [JsonProperty]
        public string authkey { get; set; }

        [JsonProperty]
        public string[] permissions { get; set; }

        [JsonProperty]
        public string[] endpoints { get; set; }
    }
}
