using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DevShowTwitterFollow.Models
{
    [DataContract]
    public class MixerSocial
    {
        public List<object> verified { get; set; }

        [DataMember(Name = "twitter")]
        public string Twitter { get; set; }
    }

    public class MixerChannel
    {
        public bool featured { get; set; }
        public int id { get; set; }
        public int userId { get; set; }
        public string token { get; set; }
        public bool online { get; set; }
        public int featureLevel { get; set; }
        public bool partnered { get; set; }
        public int transcodingProfileId { get; set; }
        public bool suspended { get; set; }
        public string name { get; set; }
        public string audience { get; set; }
        public int viewersTotal { get; set; }
        public int viewersCurrent { get; set; }
        public int numFollowers { get; set; }
        public object description { get; set; }
        public int typeId { get; set; }
        public bool interactive { get; set; }
        public object interactiveGameId { get; set; }
        public int ftl { get; set; }
        public bool hasVod { get; set; }
        public string languageId { get; set; }
        public object coverId { get; set; }
        public object thumbnailId { get; set; }
        public object badgeId { get; set; }
        public object bannerUrl { get; set; }
        public object hosteeId { get; set; }
        public bool hasTranscodes { get; set; }
        public bool vodsEnabled { get; set; }
        public object costreamId { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
        public object deletedAt { get; set; }
    }

    public class MixerUser
    {
        public int level { get; set; }
        public MixerSocial social { get; set; }
        public int id { get; set; }
        public string username { get; set; }
        public bool verified { get; set; }
        public int experience { get; set; }
        public int sparks { get; set; }
        public object avatarUrl { get; set; }
        public object bio { get; set; }
        public object primaryTeam { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
        public object deletedAt { get; set; }
        public MixerChannel channel { get; set; }
    }
}
