using Newtonsoft.Json;

namespace MixerChatBot.Chat.Messages
{
    [JsonObject]
    public class ChatMessageInfo : BaseEvent
    {
        [JsonProperty]
        public string id { get; set; }

        [JsonProperty]
        public ulong user_id { get; set; }

        [JsonProperty]
        public string user_name { get; set; }

        public ChatMessagesData message { get; set; }
    }
}
