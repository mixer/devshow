using System;
using Newtonsoft.Json;

namespace MixerChatBot.Http.Contracts
{
    [JsonObject]
    public class ChannelInfoResponse
    {
        [JsonProperty]
        public bool featured { get; set; }

        [JsonProperty]
        public uint id { get; set; }

        [JsonProperty]
        public int userId { get; set; }

        [JsonProperty]
        public string token { get; set; }

        [JsonProperty]
        public bool online { get; set; }

        [JsonProperty]
        public int? featureLevel { get; set; }

        [JsonProperty]
        public bool partnered { get; set; }

        [JsonProperty]
        public int? transcodingProfileId { get; set; }

        [JsonProperty]
        public bool suspended { get; set; }

        [JsonProperty]
        public string name { get; set; }

        [JsonProperty]
        public string audience { get; set; }

        [JsonProperty]
        public int? viewersTotal { get; set; }

        [JsonProperty]
        public int? viewersCurrent { get; set; }

        [JsonProperty]
        public int? numFollowers { get; set; }

        [JsonProperty]
        public string description { get; set; }

        [JsonProperty]
        public int? typeId { get; set; }

        [JsonProperty]
        public bool interactive { get; set; }

        [JsonProperty]
        public int? interactiveGameId { get; set; }

        [JsonProperty]
        public int? ftl { get; set; }

        [JsonProperty]
        public bool hasVod { get; set; }

        [JsonProperty]
        public string languageId { get; set; }

        [JsonProperty]
        public int? coverId { get; set; }

        [JsonProperty]
        public int? thumbnailId { get; set; }

        [JsonProperty]
        public bool vodsEnabled { get; set; }

        [JsonProperty]
        public System.Guid? costreamId { get; set; }
    }
}
