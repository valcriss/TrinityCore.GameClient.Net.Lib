namespace TrinityCore.GameClient.Net.Lib.Network.Auth.Models
{
    public class AuthServerInfo
    {
        #region Public Properties

        public string Hostname { get; set; }
        public int Port { get; set; }

        #endregion Public Properties

        #region Public Constructors

        public AuthServerInfo(string hostname) : this(hostname, 3724)
        {
        }

        public AuthServerInfo(string hostname, int port)
        {
            Hostname = hostname;
            Port = port;
        }

        #endregion Public Constructors
    }
}