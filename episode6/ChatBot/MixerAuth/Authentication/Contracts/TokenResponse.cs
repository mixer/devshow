using Newtonsoft.Json;

namespace MixerChatBot.Authentication.Contracts
{
    [JsonObject]
    public class TokenResponse
    {
        [JsonProperty]
        public string access_token { get; set; }

        [JsonProperty]
        public string token_type { get; set; }

        [JsonProperty]
        public uint expires_in { get; set; }

        [JsonProperty]
        public string refresh_token { get; set; }
    }
}
