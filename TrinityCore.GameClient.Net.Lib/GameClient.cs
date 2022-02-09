using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Components.Environment;
using TrinityCore.GameClient.Net.Lib.Network.Auth;
using TrinityCore.GameClient.Net.Lib.Network.Auth.Models;
using TrinityCore.GameClient.Net.Lib.Network.World;
using TrinityCore.GameClient.Net.Lib.Network.World.Models;

namespace TrinityCore.GameClient.Net.Lib
{
    public class GameClient : IDisposable
    {
        public EnvironmentComponent Environment { get; set; }


        #region Private Properties

        private AuthClient AuthClient { get; set; }
        private string Hostname { get; set; }
        private string Password { get; set; }
        private int Port { get; set; }
        private string Username { get; set; }
        private WorldClient WorldClient { get; set; }

        #endregion Private Properties

        #region Private Fields

        private bool _disposed;

        #endregion Private Fields

        #region Public Constructors

        public GameClient(string username, string password, string hostname, int port = 3724)
        {
            Username = username;
            Password = password;
            Hostname = hostname;
            Port = port;

            AuthClient = new AuthClient();
            WorldClient = new WorldClient();

            // Components
            Environment = new EnvironmentComponent(WorldClient);
        }

        #endregion Public Constructors

        #region Public Methods

        public async Task<bool> Authenticate()
        {
            return await AuthClient.Authenticate(Hostname, Port, Username, Password);
        }

        public async Task<bool> ConnectToRealm(WorldServerInfo realm)
        {
            return await WorldClient.Authenticate(realm, AuthClient.Credentials);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task<bool> EnterRealm(Character character)
        {
            return await WorldClient.LoginCharacter(character);
        }

        public async Task<List<Character>> GetCharacters()
        {
            return await WorldClient.GetCharacters();
        }

        public async Task<List<WorldServerInfo>> GetRealms()
        {
            return await AuthClient.GetRealms();
        }

        public async Task<bool> LogOut()
        {
            return await WorldClient.LogOut();
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }

        #endregion Protected Methods
    }
}