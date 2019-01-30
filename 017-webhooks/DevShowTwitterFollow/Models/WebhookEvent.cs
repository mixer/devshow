using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DevShowTwitterFollow.Models
{
    [DataContract]
    class WebhookEvent<T>
    {
        [DataMember(Name = "event")]
        public string Event { get; set; }

        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "payload")]
        public T Payload { get; set; }

        [DataMember(Name = "sentAt")]
        public DateTime SentAt { get; set; }
    }
}
