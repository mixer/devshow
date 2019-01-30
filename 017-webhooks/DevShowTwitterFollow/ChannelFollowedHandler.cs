using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DevShowTwitterFollow.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Tweetinvi;

namespace DevShowTwitterFollow
{
    public static class Function1
    {
        [FunctionName("ChannelFollowedHandler")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "channel/{channelId}/followed")]HttpRequestMessage req,
            TraceWriter log,
            string channelId
        )
        {
            log.Info("Channel followed handler: " + channelId);
            //log.Info("Data: " + await req.Content.ReadAsStringAsync());

            var ev = await req.Content.ReadAsAsync<WebhookEvent<UserFollowedEvent>>();
            if (string.IsNullOrEmpty(ev?.Payload?.User?.social?.Twitter))
            {
                log.Info(string.Format("User {0} did not have twitter",
                    ev?.Payload?.User?.username));
                return req.CreateResponse(HttpStatusCode.OK);
            }

            var twitter = ev.Payload.User.social.Twitter
                .Replace("https://twitter.com/", "")
                .Replace("https://www.twitter.com/", "")
                .Replace("@", "");

            log.Info(string.Format("User {0} is {1}, and their twitter is {2}",
                ev.Payload.User.username,
                ev.Payload.Following ? "following" : "not following",
                twitter));

            // Auth with twitter
            Auth.SetUserCredentials(
                Variables.TwitterConsumerKey,
                Variables.TwitterConsumerSecret,
                Variables.TwitterAccessToken,
                Variables.TwitterAccessTokenSecret);

            // Get my user
            var me = await Tweetinvi.UserAsync.GetAuthenticatedUser();
            var them = await Tweetinvi.UserAsync.GetUserFromScreenName(twitter);
            var relationship = await Tweetinvi.FriendshipAsync.
                GetRelationshipDetailsBetween(me.Id, them.Id);

            if (relationship.Following)
            {
                log.Info("Already following: " + twitter);
                if (!ev.Payload.Following)
                {
                    // Unfollow
                    if (await me.UnFollowUserAsync(them.Id))
                    {
                        log.Info("Unfollowed user: " + twitter);
                    }
                    else
                    {
                        log.Info("Unfollow failed for user: " + twitter);
                        return req.CreateResponse(HttpStatusCode.InternalServerError);
                    }
                }
            }
            else
            {
                log.Info("Not already following: " + twitter);
                if (ev.Payload.Following)
                {
                    // Follow
                    if (await me.FollowUserAsync(them.Id))
                    {
                        log.Info("followed user: " + twitter);
                    }
                    else
                    {
                        log.Info("follow failed for user: " + twitter);
                        return req.CreateResponse(HttpStatusCode.InternalServerError);
                    }
                }
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}
