using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrinityCore.GameClient.Net.Lib.Network.Auth.Models
{
    public class AuthServerInfo
    {
        public string Hostname { get; set; }
        public int Port { get; set; }   

        public AuthServerInfo(string hostname) : this(hostname, 3724) { }

        public AuthServerInfo(string hostname, int port)
        {
            Hostname = hostname;
            Port = port;
        }


    }
}
