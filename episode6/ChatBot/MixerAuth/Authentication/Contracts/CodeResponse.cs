using Newtonsoft.Json;

namespace MixerChatBot.Authentication.Contracts
{
    [JsonObject]
    public class CodeResponse
    {
        [JsonProperty]
        public string code { get; set; }
    }
}
