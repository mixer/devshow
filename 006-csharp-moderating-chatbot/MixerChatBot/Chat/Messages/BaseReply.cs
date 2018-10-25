using Newtonsoft.Json;

namespace MixerChatBot.Chat.Messages
{
    [JsonObject]
    public class BaseReply : BaseMessage
    {
        public const string Type = "reply";

        [JsonProperty]
        public uint id { get; set; }
    }
}
