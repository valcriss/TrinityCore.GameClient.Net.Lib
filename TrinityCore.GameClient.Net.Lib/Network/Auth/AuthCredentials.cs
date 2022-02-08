using System.Net;
using System.Numerics;

namespace TrinityCore.GameClient.Net.Lib.Network.Auth
{
    public class AuthCredentials
    {
        #region Internal Properties

        internal IPAddress IPAddress { get; set; }
        internal string Password { get; set; }
        internal BigInteger SessionKey { get; set; }
        internal byte[] SessionProof { get; set; }
        internal string Username { get; set; }

        #endregion Internal Properties

        #region Internal Constructors

        internal AuthCredentials(string username, string password)
        {
            Username = username.ToUpper();
            Password = password;
        }

        #endregion Internal Constructors
    }
}