using System;
using System.Collections.Generic;
using System.Text;

namespace MixerChatBot.Chat.Messages
{
    public class ChatMethod
    {
        /// <summary>
        /// Authenticate as an active user in a specified channel. Arguments are channelId, userId, key. You can connect anonymously by
        /// supplying just the channnelId as an argument, but if you do this you will not be able to send messages or participate in chat. 
        /// This can be useful for creating chat overlays.
        /// </summary>
        public const string Auth = "auth";

        /// <summary>
        /// Send a chat message to the server. The server will reply with data identical to a ChatMessage event.
        /// </summary>
        public const string Message = "msg";

        /// <summary>
        /// Send a whisper to another user in the chat.
        /// </summary>
        public const string Whisper = "whisper";

        /// <summary>
        /// Time a user out and purge their chat messages. They cannot send messages until the duration is over. The user being timed out must be in the channel.
        /// </summary>
        public const string Timeout = "timeout";

        /// <summary>
        /// Purge a user's messages from that chat without timing them out.
        /// </summary>
        public const string Purge = "purge";

        /// <summary>
        /// Delete a message from chat.
        /// </summary>
        public const string DeleteMessage = "deleteMessage";

        /// <summary>
        /// Request previous messages from this chat from before you joined.
        /// </summary>
        public const string History = "history";

        /// <summary>
        /// Start a giveaway in the channel. After sending this method, the 'HypeBot' user will publicly announce the winner of the giveaway, who 
        /// will be randomly selected.
        /// </summary>
        public const string Giveawaystart = "giveaway:start";

        /// <summary>
        /// A ping method. This should be used in environments that do not support Native WebSocket Pings. An example of this is Chat 
        /// implementations in with a web browser.
        /// </summary>
        public const string Ping = "ping";

        /// <summary>
        /// Enable an enhancement to the ChatMessage event. This will populate the meta property of the event with a hash containing the emoticon
        /// text mapped to the base64 PNG representation.
        /// </summary>
        public const string Attachemotes = "attachEmotes";
    }
}
