namespace TrinityCore.GameClient.Net.Lib.Network.Auth.Models
{
    public class AuthServerCredentials
    {
        #region Public Properties

        public string Password { get; set; }
        public string Username { get; set; }

        #endregion Public Properties

        #region Public Constructors

        public AuthServerCredentials(string username, string password)
        {
            Username = username;
            Password = password;
        }

        #endregion Public Constructors
    }
}