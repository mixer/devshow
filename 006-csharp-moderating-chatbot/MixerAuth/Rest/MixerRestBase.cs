using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using MixerChatBot.Authentication.Contracts;
using Newtonsoft.Json;

namespace MixerChatBot.Rest
{
    public class MixerRestBase : HttpClient
    {
        private TokenResponse authInfo;

        public MixerRestBase(TokenResponse auth = null)
            : base(CreateWebRequestHandler(), true)
        {
            authInfo = auth;
        }

        /// <summary>
        /// Gets the type of token
        /// </summary>
        protected string AuthenticationScheme
        {
            get
            {
                return authInfo.token_type;
            }
        }

        /// <summary>
        /// Gets the OAuth access token
        /// </summary>
        protected string AccessToken
        {
            get
            {
                return authInfo.access_token;
            }
        }

        /// <summary>
        /// Helper method to set the handler settings for this httpclient
        /// </summary>
        /// <returns>The HttpMessageHandler</returns>
        private static HttpMessageHandler CreateWebRequestHandler()
        {
            HttpClientHandler requestHandler = new HttpClientHandler
            {
                CookieContainer = new CookieContainer(),
                UseCookies = true,
            };

            return requestHandler;
        }

        /// <summary>
        /// Handles a web response and deserializes the response body.
        /// </summary>
        /// <typeparam name="T">The object type to deserialize into.</typeparam>
        /// <param name="response">The completed HTTP response.</param>
        /// <returns>A deserialized object from the response.</returns>
        protected async Task<T> GetResponseAsync<T>(HttpResponseMessage response)
            where T : class
        {
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new WebException(error);
            }

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();

            var contract = JsonConvert.DeserializeObject<T>(content);

            return contract;
        }
    }
}
