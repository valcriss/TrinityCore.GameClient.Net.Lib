using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Components.Entities;
using TrinityCore.GameClient.Net.Lib.Components.Environment;
using TrinityCore.GameClient.Net.Lib.Components.Factions;
using TrinityCore.GameClient.Net.Lib.Components.Player;
using TrinityCore.GameClient.Net.Lib.Components.Social;
using TrinityCore.GameClient.Net.Lib.Components.Zone;
using TrinityCore.GameClient.Net.Lib.Network.Auth;
using TrinityCore.GameClient.Net.Lib.Network.Auth.Models;
using TrinityCore.GameClient.Net.Lib.Network.World;
using TrinityCore.GameClient.Net.Lib.Network.World.Models;

namespace TrinityCore.GameClient.Net.Lib
{
    public class GameClient : IDisposable
    {
        #region Public Properties

        public EnvironmentComponent Environment { get; set; }
        public FactionsComponent Factions { get; set; }
        public PlayerComponent Player { get; set; }
        public SocialComponent Social { get; set; }
        public ZoneComponent Zone { get; set; }
        public EntitiesComponent Entities { get; set; }

        #endregion Public Properties

        #region Private Properties

        private AuthClient AuthClient { get; set; }
        private WorldClient WorldClient { get; set; }

        #endregion Private Properties

        #region Private Fields

        private bool _disposed;

        #endregion Private Fields

        #region Public Constructors

        public GameClient()
        {
            AuthClient = new AuthClient();
            WorldClient = new WorldClient();

            // Components
            Environment = new EnvironmentComponent(WorldClient);
            Factions = new FactionsComponent(WorldClient);
            Player = new PlayerComponent(WorldClient);
            Social = new SocialComponent(WorldClient);
            Zone = new ZoneComponent(WorldClient);
            Entities = new EntitiesComponent(WorldClient, Player);
        }

        #endregion Public Constructors

        #region Public Methods

        public async Task<bool> Authenticate(AuthServerInfo authServer, AuthServerCredentials credentials)
        {
            return await AuthClient.Authenticate(authServer, credentials);
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
            bool result = await WorldClient.LogOut();
            if(result)
            {
                Entities.Close();
            }
            return result;
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