using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrinityCore.GameClient.Net.Lib.Network.Auth.Models
{
    public class AuthServerCredentials
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public AuthServerCredentials(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }
}
