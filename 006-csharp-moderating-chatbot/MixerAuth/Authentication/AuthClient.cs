using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MixerChatBot.Rest;
using Newtonsoft.Json;

namespace MixerChatBot.Authentication
{
    /// <summary>
    /// Class to do the simplest OAuth short code login for a console app.
    /// </summary>
    public class AuthClient : MixerRestBase
    {
        private static readonly string shortCodeUri = "https://mixer.com/api/v1/oauth/shortcode";
        private static readonly string authCodeUri = "https://mixer.com/api/v1/oauth/shortcode/check/{0}";
        private static readonly string authTokenUri = "https://mixer.com/api/v1/oauth/token";
        private static readonly string authCodeType = "authorization_code";
        private string clientId;
        private string clientSecret;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <see cref="https://mixer.com/lab/oauth"/>
        /// <param name="clientId">The OAuth client id.</param>
        /// <param name="clientSecret">The OAuth client secret, only required if the OAuth client in use has one.</param>
        public AuthClient(string clientId, string clientSecret)
            : base(null)
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;
        }

        /// <summary>
        /// Runs the full OAuth login flow for a console app.
        /// </summary>
        /// <param name="scopes">List of permissions separated by whitespace.</param>
        /// <returns>The access and refresh tokens.</returns>
        public async Task<Contracts.TokenResponse> RunOauthCodeFlowForConsoleAppAsync(string scopes)
        {
            Contracts.ShortCodeResponse shortCode = await this.RequestShortcodeAsync(scopes);

            Console.WriteLine("Go to http://mixer.com/go and enter code " + shortCode.code);

            Contracts.CodeResponse authCode = await this.RequestAuthCodeAsync(shortCode.handle);
            while (authCode == null)
            {
                await Task.Delay(500);
                authCode = await this.RequestAuthCodeAsync(shortCode.handle);
            }

            return await this.ExchangeCodeForTokenAsync(authCode.code);
        }

        /// <summary>
        /// Initializes the "shortcode" auth flow. The endpoint takes a client_id, client_secret, and scope similarly to the initial authorization request. It returns a short, six-digit code to be displayed to the user to link their account in addition to a longer "handle" that can be used for polling its status in POST /oauth/code/{handle}.
        /// </summary>
        /// <see cref="https://dev.mixer.com/rest/index.html#oauth_shortcode_post"/>
        /// <param name="scopes">List of permissions separated by whitespace.</param>
        /// <returns>Code for the user and handle for the app.</returns>
        public async Task<Contracts.ShortCodeResponse> RequestShortcodeAsync(string scopes)
        {
            var body = new Contracts.ShortCodeRequest
            {
                client_id = this.clientId,
                client_secret = this.clientSecret,
                scope = scopes,
            };

            using (var request = new HttpRequestMessage(HttpMethod.Post, shortCodeUri))
            {
                request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
                using (var response = await this.SendAsync(request))
                {
                    return await this.GetResponseAsync<Contracts.ShortCodeResponse>(response);
                }
            }
        }

        /// <summary>
        /// Checks the status of a previously issued code and, after it's been activated, returns an OAuth authorization code to be passed to GET /oauth/token to exchange it for an access and refresh token. Once again, the client ID and secret (if any) are required.
        /// </summary>
        /// <see cref="https://dev.mixer.com/rest/index.html#oauth_shortcode_check__handle__get"/>
        /// <param name="handle">The code handle as returned in POST /oauth/code</param>
        /// <returns>Null if the code is still valid but the user hasn't granted it yet, or OAuth code to exchange for access and refresh tokens.</returns>
        public async Task<Contracts.CodeResponse> RequestAuthCodeAsync(string handle)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, string.Format(CultureInfo.InvariantCulture, authCodeUri, handle)))
            {
                using (var response = await this.SendAsync(request))
                {
                    return await this.GetResponseAsync<Contracts.CodeResponse>(response);
                }
            }
        }

        /// <summary>
        /// Retrieves an OAuth token.
        /// </summary>
        /// <see cref="https://dev.mixer.com/rest/index.html#oauth_token_post"/>
        /// <param name="code">The authorization code received from the /authorize endpoint.</param>
        /// <returns>The access and refresh tokens.</returns>
        public async Task<Contracts.TokenResponse> ExchangeCodeForTokenAsync(string code)
        {
            var body = new Contracts.TokenRequest
            {
                code = code,
                client_id = this.clientId,
                client_secret = this.clientSecret,
                grant_type = authCodeType,
            };

            using (var request = new HttpRequestMessage(HttpMethod.Post, authTokenUri))
            {
                request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
                using (var response = await this.SendAsync(request))
                {
                    return await this.GetResponseAsync<Contracts.TokenResponse>(response);
                }
            }
        }
    }
}
