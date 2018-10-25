using Newtonsoft.Json;

namespace MixerChatBot.Http.Contracts
{
    [JsonObject]
    public class SocialInfo
    {
        [JsonProperty]
        public string twitter { get; set; }

        [JsonProperty]
        public string facebook { get; set; }

        [JsonProperty]
        public string youtube { get; set; }

        [JsonProperty]
        public string player { get; set; }

        [JsonProperty]
        public string discord { get; set; }

        [JsonProperty]
        public string[] verified { get; set; }
    }
}
