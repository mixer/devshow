using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DevShowTwitterFollow.Models
{
    [DataContract]
    class UserFollowedEvent
    {
        [DataMember(Name = "user")]
        public MixerUser User { get; set; }

        [DataMember(Name = "following")]
        public bool Following { get; set; }
    }
}
