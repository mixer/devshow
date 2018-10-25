using System.Collections.Generic;
using Newtonsoft.Json;

namespace MixerChatBot.Chat.Messages
{
    [JsonObject]
    public class ModeratorData
    {
        [JsonProperty]
        public string user_name { get; set; }

        [JsonProperty]
        public uint user_id { get; set; }

        [JsonProperty]
        public List<string> user_roles { get; set; }

        [JsonProperty]
        public uint user_level { get; set; }
    }
}
