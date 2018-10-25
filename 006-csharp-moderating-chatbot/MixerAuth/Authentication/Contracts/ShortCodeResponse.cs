using Newtonsoft.Json;

namespace MixerChatBot.Authentication.Contracts
{
    [JsonObject]
    public class ShortCodeResponse
    {
        [JsonProperty]
        public string handle { get; set; }

        [JsonProperty]
        public string code { get; set; }

        [JsonProperty]
        public uint expires_in { get; set; }
    }
}
