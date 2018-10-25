using Newtonsoft.Json;

namespace MixerChatBot.Chat.Messages
{
    [JsonObject]
    public class WelcomeEvent : BaseEvent
    {
        public const string EventType = "WelcomeEvent";

        [JsonProperty]
        public ConnectionInfo data { get; set; }
    }
}
