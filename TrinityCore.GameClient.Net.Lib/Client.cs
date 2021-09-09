using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Auth;
using TrinityCore.GameClient.Net.Lib.Network.Entities;
using TrinityCore.GameClient.Net.Lib.World;
using TrinityCore.GameClient.Net.Lib.World.Entities;

namespace TrinityCore.GameClient.Net.Lib
{
    public class Client
    {
        private AuthClient AuthClient { get; set; }
        private WorldClient WorldClient { get; set; }

        public Client(string host, int port, string login, string password)
        {
            AuthClient = new AuthClient(host, port, login, password);
        }

        public async Task<bool> Authenticate()
        {
            return await AuthClient.Authenticate();
        }

        public async Task<List<WorldServerInfo>> GetWorlds()
        {
            return await AuthClient.GetWorlds();
        }

        public async Task<bool> Connect(WorldServerInfo server)
        {
            WorldClient = new WorldClient(server, AuthClient.Username, AuthClient.SessionKey);
            return await WorldClient.Start();
        }

        public async Task<List<Character>> GetCharacters()
        {
            return await WorldClient.GetCharacters();
        }

        public async Task<bool> EnterWorld(Character selectedCharacter)
        {
            return await WorldClient.LoginCharacter(selectedCharacter);
        }
    }
}
