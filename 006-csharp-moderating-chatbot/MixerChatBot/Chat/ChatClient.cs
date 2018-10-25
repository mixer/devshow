using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using MixerChatBot.Http.Contracts;
using MixerChatBot.Chat.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MixerChatBot.Chat
{
    /// <summary>
    /// Class to connect and send & recieve chat messages
    /// </summary>
    public class ChatClient
    {
        private ClientWebSocket chatSocket = new ClientWebSocket();
        private string server = null;
        JsonSerializer serializer = JsonSerializer.Create();
        private long messageId = 0;

        /// <summary>
        /// Connects to a chat channel given chat auth info already set up for that channel.
        /// </summary>
        /// <param name="chatInfo">The chat auth and connection info.</param>
        /// <param name="channelId">The channel to connect to.</param>
        /// <param name="userId">The user that is connecting to chat.</param>
        /// <returns>void</returns>
        public async Task ConnectAsync(ChatConnectionInformation chatInfo, uint channelId, uint userId)
        {
            // we have a list of chat servers, the first one is good enough
            this.server = chatInfo.endpoints[0];

            using (var ts = new CancellationTokenSource())
            {
                // connect the socket and get the welcome message.
                await this.chatSocket.ConnectAsync(new System.Uri(this.server), ts.Token);
                byte[] messageBytes = await RecieveMessageAsync(ts.Token);
                Messages.WelcomeEvent welcome = DeserializeMessage<Messages.WelcomeEvent>(messageBytes);

                await this.Authenticate(chatInfo.authkey, channelId, userId, ts.Token);
            }
        }

        /// <summary>
        /// Get the next chat message from the channel.
        /// </summary>
        /// <returns>The chat message info.</returns>
        public async Task<Messages.BaseEvent> GetNextChatMessageAsync()
        {
            Messages.BaseEvent chatMessageInfo = null;

            // we'll just keep trying till we get a chat message
            using (var ts = new CancellationTokenSource())
            {
                while (chatMessageInfo == null)
                {
                    try
                    {
                        byte[] nextMessage = await this.RecieveMessageAsync(ts.Token);

                        var rawJson = DeserializeMessage<JObject>(nextMessage);

                        chatMessageInfo = rawJson.ToObject<Messages.BaseEvent>();
                        if (string.CompareOrdinal(chatMessageInfo.type, Messages.BaseEvent.Type) != 0)
                        {
                            chatMessageInfo = null;
                        }
                        else if (string.CompareOrdinal(chatMessageInfo.Event, Messages.ChatDeleteMessageEvent.EventType) == 0)
                        {
                            chatMessageInfo = rawJson.ToObject<Messages.ChatDeleteMessageEvent>();
                        }
                        else if (string.CompareOrdinal(chatMessageInfo.Event, Messages.ChatUserJoinEvent.EventType) == 0)
                        {
                            chatMessageInfo = rawJson.ToObject<Messages.ChatUserJoinEvent>();
                        }
                        else if (string.CompareOrdinal(chatMessageInfo.Event, Messages.ChatUserLeaveEvent.EventType) == 0)
                        {
                            chatMessageInfo = rawJson.ToObject<Messages.ChatUserLeaveEvent>();
                        }
                        else if (string.CompareOrdinal(chatMessageInfo.Event, Messages.ChatMessageEvent.EventType) == 0)
                        {
                            chatMessageInfo = rawJson.ToObject<Messages.ChatMessageEvent>();
                        }
                        else
                        {
                            // we didn't match a type we care about, just throw it away
                            chatMessageInfo = null;
                        }
                    }
                    catch (Exception)
                    {
                        // if anything goes wrong, skill the message
                        chatMessageInfo = null;
                    }
                }
            }

            return chatMessageInfo;
        }

        /// <summary>
        /// Send the user athentication info to the chat server.
        /// </summary>
        /// <see cref="https://dev.mixer.com/reference/chat/methods/auth"/>
        /// <param name="authToken">The chat auth token</param>
        /// <param name="channelId">The channel to connect to chat for</param>
        /// <param name="userId">The user the auth token is for</param>
        /// <param name="token">A cancellation token</param>
        /// <returns>void</returns>
        private async Task Authenticate(string authToken, uint channelId, uint userId, CancellationToken token)
        {
            Messages.SendMethodCall methodData = new Messages.SendMethodCall
            {
                method = ChatMethod.Auth,
                arguments = new JArray(channelId, userId, authToken),
                id = Interlocked.Increment(ref this.messageId),
            };

            await this.chatSocket.SendAsync(SerializeToJsonBytes(methodData), WebSocketMessageType.Text, true, token);

            // the response may not be the next message
            Messages.AuthenticationReply reply = null;
            while (reply == null)
            {
                byte[] nextMessage = await this.RecieveMessageAsync(token);
                reply = DeserializeMessage<Messages.AuthenticationReply>(nextMessage);
                if (string.CompareOrdinal(reply.type, Messages.BaseReply.Type) != 0 ||
                    reply.id != methodData.id)
                {
                    // not our message, wait for the next one
                    reply = null;
                }
            }

            if (reply.error != null)
            {
                throw new WebSocketException(reply.error.code, reply.error.message);
            }
        }

        /// <summary>
        /// Send a whisper to another user in the chat.
        /// </summary>
        /// <see cref="https://dev.mixer.com/reference/chat/methods/whisper"/>
        /// <param name="targetUserName">The user to whisper to</param>
        /// <param name="message">The message to send to the user</param>
        /// <returns>void</returns>
        public async Task SendWhisperAsync(string targetUserName, string message)
        {
            await this.SendToChatAsync(ChatMethod.Whisper, new JArray(targetUserName, message));
        }

        /// <summary>
        /// Delete a message out of the channel's chat.
        /// </summary>
        /// <see cref="https://dev.mixer.com/reference/chat/methods/deletemessage"/>
        /// <param name="messageId">The id of the message to delete</param>
        /// <returns>void</returns>
        public async Task SendDeleteMessageAsync(string messageId)
        {
            await this.SendToChatAsync(ChatMethod.DeleteMessage, new JArray(messageId));
        }

        /// <summary>
        /// Send a chat message to the server. The server will reply with data identical to a ChatMessage event.
        /// </summary>
        /// <see cref="https://dev.mixer.com/reference/chat/methods/msg"/>
        /// <param name="message">The message to send to chat</param>
        /// <returns>void</returns>
        public async Task SendMessageAsync(string message)
        {
            await this.SendToChatAsync(ChatMethod.Message, new JArray(message));
        }

        /// <summary>
        /// Call to do the socket send.
        /// </summary>
        /// <param name="method">The method to be sent</param>
        /// <param name="parameters">Parameters to the method</param>
        /// <returns>Async task</returns>
        private async Task SendToChatAsync(string method, JArray parameters)
        {
            using (var ts = new CancellationTokenSource())
            {
                Messages.SendMethodCall methodData = new Messages.SendMethodCall
                {
                    method = method,
                    arguments = parameters,
                    id = Interlocked.Increment(ref this.messageId),
                };

                await this.chatSocket.SendAsync(SerializeToJsonBytes(methodData), WebSocketMessageType.Text, true, ts.Token);

                // at this point you'd want to validate the response, but we're throwing them away
                // in the recieve for simplicity, so we'll skip it
            }
        }


        /// <summary>
        /// Get the next message from the chat server. This is where buffer management happens.
        /// </summary>
        /// <param name="token">A cancellation token.</param>
        /// <returns>The message bytes or null.</returns>
        private async Task<byte[]> RecieveMessageAsync(CancellationToken token)
        {
            // First we'll need a buffer to get the data from the socket into
            const int recvBufferSize = 1024 * 12;
            byte[] buffer = new byte[recvBufferSize];

            // WebSocket uses an ArraySegment to manage that buffer
            ArraySegment<byte> segment = new ArraySegment<byte>(buffer);

            // Recieve will wait for the next message to come in
            WebSocketReceiveResult result = await this.chatSocket.ReceiveAsync(segment, token);

            // Receive may not have gotten all of the message
            int offset = result.Count;
            while (!result.EndOfMessage)
            {
                // we may have run out of space in the buffer
                if (offset >= buffer.Length)
                {
                    Array.Resize(ref buffer, buffer.Length + recvBufferSize);
                }

                // Reset the ArraySegment atr the new offset and get more data
                segment = new ArraySegment<byte>(buffer, offset, buffer.Length - offset);
                result = await this.chatSocket.ReceiveAsync(segment, token);
                offset += result.Count;
            }

            // Maybe there was no data, a cancel or timeout
            if (offset == 0)
            {
                return null;
            }

            // now we need to return the message in the right sized byte array
            byte[] message = new byte[offset];
            Buffer.BlockCopy(buffer, 0, message, 0, offset);
            return message;
        }

        /// <summary>
        /// Serializes an object to bytes that can be sent to the chat server.
        /// </summary>
        /// <typeparam name="T">The type of object to send.</typeparam>
        /// <param name="obj">The data to serialize.</param>
        /// <returns>Date that can be sent to the chat server.</returns>
        public ArraySegment<byte> SerializeToJsonBytes<T>(T obj)
        {
            var memoryStream = new MemoryStream();

            var sw = new StreamWriter(memoryStream);
            using (var jtw = new JsonTextWriter(sw))
            {
                this.serializer.Serialize(jtw, obj);
                sw.Flush();
                memoryStream.Position = 0;
                return new ArraySegment<byte>(memoryStream.ToArray());
            }
        }

        /// <summary>
        /// Deserializes a byte stream from the socket into objects.
        /// </summary>
        /// <typeparam name="T">The expected object type of the message.</typeparam>
        /// <param name="bytes">The message from the socket.</param>
        /// <returns>A deserialized object.</returns>
        public T DeserializeMessage<T>(byte[] bytes)
        {
            var memoryStream = new MemoryStream(bytes);
            memoryStream.Flush();
            memoryStream.Position = 0;
            var sr = new StreamReader(memoryStream);
            using (var jr = new JsonTextReader(sr))
            {
                return this.serializer.Deserialize<T>(jr);
            }
        }
    }
}
