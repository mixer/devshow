using Newtonsoft.Json;

namespace MixerChatBot.Http.Contracts
{
    [JsonObject]
    public class UserInfoResponse
    {
        [JsonProperty]
        public uint id { get; set; }

        [JsonProperty]
        public string username { get; set; }

        [JsonProperty]
        public string email { get; set; }

        [JsonProperty]
        public bool verified { get; set; }

        [JsonProperty]
        public uint level { get; set; }

        [JsonProperty]
        public SocialInfo social { get; set; }

        [JsonProperty]
        public int experience { get; set; }

        [JsonProperty]
        public int sparks { get; set; }

        [JsonProperty]
        public string avatarUrl { get; set; }

        [JsonProperty]
        public string bio { get; set; }

        [JsonProperty]
        public ChannelInfoResponse channel { get; set; }

        [JsonProperty]
        public int? primaryTeam { get; set; }

        [JsonProperty]
        public int? transcodingProfileId { get; set; }

        [JsonProperty]
        public bool hasTranscodes { get; set; }
    }
}
