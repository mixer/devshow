using Newtonsoft.Json;

namespace MixerChatBot.Chat.Messages
{
    [JsonObject]
    public class AuthenticationReply : BaseReply
    {
        [JsonProperty]
        public AuthenticationInfo data { get; set; }

        [JsonProperty]
        public ErrorInfo error { get; set; }
    }
}
