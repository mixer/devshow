using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MixerChatBot.Authentication.Contracts;
using MixerChatBot.Http.Contracts;
using MixerChatBot.Rest;
using Newtonsoft.Json;

namespace MixerChatBot.Http
{
    /// <summary>
    /// A client for Mixer's http REST APIs
    /// </summary>
    class HttpClient : MixerRestBase
    {
        private static readonly string chatConnectInfoUri = "https://mixer.com/api/v1/chats/{0}";
        private static readonly string currentUserInfoUri = "https://mixer.com/api/v1/users/current";

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="auth">Optional auth token.</param>
        public HttpClient(TokenResponse auth)
            : base(auth)
        {
        }

        /// <summary>
        /// Prepares the user to join a chat channel. It returns the channel's chatroom settings, available chat servers, and an authentication key that the user (if authenticated) should use to authenticate with the chat servers over websockets.
        /// </summary>
        /// <see cref="https://dev.mixer.com/rest/index.html#chats__channelId__get"/>
        /// <param name="authToken">OAuth token info.</param>
        /// <param name="channelId">The id of the channel to join chat for.</param>
        /// <returns>Chat connection info and auth key.</returns>
        public async Task<ChatConnectionInformation> RequestChatAuthKeyAsync(uint channelId)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, string.Format(CultureInfo.InvariantCulture, chatConnectInfoUri, channelId)))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(this.AuthenticationScheme, this.AccessToken);
                using (var response = await this.SendAsync(request))
                {
                    return await this.GetResponseAsync<ChatConnectionInformation>(response);
                }
            }
        }

        /// <summary>
        /// Retrieves the currently authenticated user. Private properties, such as the account's email address, will only be included if authenticated with the user:details permission.
        /// </summary>
        /// <see cref="https://dev.mixer.com/rest/index.html#users_current_get"/>
        /// <returns>The info on the user whose auth token was used.</returns>
        public async Task<UserInfoResponse> GetAuthenticatedUserInfoAsync()
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, currentUserInfoUri))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(this.AuthenticationScheme, this.AccessToken);
                using (var response = await this.SendAsync(request))
                {
                    return await this.GetResponseAsync<UserInfoResponse>(response);
                }
            }
        }
    }
}
