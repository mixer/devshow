using Newtonsoft.Json;

namespace MixerChatBot.Authentication.Contracts
{
    [JsonObject]
    public class TokenRequest
    {
        [JsonProperty]
        public string code { get; set; }

        [JsonProperty]
        public string client_id { get; set; }

        [JsonProperty]
        public string client_secret { get; set; }

        [JsonProperty]
        public string grant_type { get; set; }
    }
}
