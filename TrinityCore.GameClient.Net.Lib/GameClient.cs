using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Components;
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
        #region Private Properties

        private AuthClient AuthClient { get; set; }
        private AuthServerCredentials AuthServerCredentials { get; set; }
        private AuthServerInfo AuthServerInfo { get; set; }
        private Dictionary<Type, Component> Components { get; set; }
        private WorldClient WorldClient { get; set; }

        #endregion Private Properties

        #region Private Fields

        private static GameClient _instance;
        private bool _disposed;

        #endregion Private Fields

        #region Private Constructors

        private GameClient(string dataPath, AuthServerInfo authServer, AuthServerCredentials credentials)
        {
            Components = new Dictionary<Type, Component>();

            AuthClient = new AuthClient();
            WorldClient = new WorldClient();

            AuthServerInfo = authServer;
            AuthServerCredentials = credentials;
            Components.Add(typeof(EnvironmentComponent), new EnvironmentComponent(WorldClient));
            Components.Add(typeof(FactionsComponent), new FactionsComponent(WorldClient));
            Components.Add(typeof(EntitiesComponent), new EntitiesComponent(WorldClient));
            Components.Add(typeof(PlayerComponent), new PlayerComponent(WorldClient));
            Components.Add(typeof(SocialComponent), new SocialComponent(WorldClient));
            Components.Add(typeof(ZoneComponent), new ZoneComponent(WorldClient));
        }

        #endregion Private Constructors

        #region Public Methods

        public static GameClient Factory(string dataPath, AuthServerInfo authServer, AuthServerCredentials credentials)
        {
            if (_instance != null) return _instance;
            _instance = new GameClient(dataPath, authServer, credentials);
            return _instance;
        }

        public static GameClient Get()
        {
            return _instance;
        }

        public static T Get<T>() where T : Component
        {
            if (_instance == null) return default(T);
            if (_instance.Components.ContainsKey(typeof(T)))
            {
                return (T)_instance.Components[typeof(T)];
            }
            return default(T);
        }

        public async Task<bool> Authenticate()
        {
            return await AuthClient.Authenticate(AuthServerInfo, AuthServerCredentials);
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
            if (result)
            {
                Get<EntitiesComponent>().Close();
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