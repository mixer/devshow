using System.Collections.Generic;
using Newtonsoft.Json;

namespace MixerChatBot.Chat.Messages
{
    [JsonObject]
    public class ChatUserData
    {
        [JsonProperty]
        public string username { get; set; }

        [JsonProperty]
        public List<string> roles { get; set; }

        [JsonProperty]
        public uint id { get; set; }

        [JsonProperty]
        public uint originatingChannel { get; set; }
    }
}
