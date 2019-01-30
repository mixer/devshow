using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevShowTwitterFollow
{
    public static class Variables
    {
        public static string TwitterConsumerKey
        {
            get
            {
                return Environment.GetEnvironmentVariable("TwitterConsumerKey", EnvironmentVariableTarget.Process);
            }
        }

        public static string TwitterConsumerSecret
        {
            get
            {
                return Environment.GetEnvironmentVariable("TwitterConsumerSecret", EnvironmentVariableTarget.Process);
            }
        }

        public static string TwitterAccessToken
        {
            get
            {
                return Environment.GetEnvironmentVariable("TwitterAccessToken", EnvironmentVariableTarget.Process);
            }
        }

        public static string TwitterAccessTokenSecret
        {
            get
            {
                return Environment.GetEnvironmentVariable("TwitterAccessTokenSecret", EnvironmentVariableTarget.Process);
            }
        }
    }
}
