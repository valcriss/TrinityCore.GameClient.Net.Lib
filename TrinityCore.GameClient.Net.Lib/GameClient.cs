using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Network.Auth.Models;
using TrinityCore.GameClient.Net.Lib.Network.World.Models;

namespace TrinityCore.GameClient.Net.Lib
{
    class GameClient : IDisposable
    {
        private bool _disposed;
        public GameClient(string username, string password, string hostname, int port = 3724)
        {

        }

        public bool Authenticate()
        {
            throw new System.NotImplementedException();
        }

        public List<WorldServerInfo> GetRealms()
        {
            throw new System.NotImplementedException();
        }

        public bool ConnectToRealm(WorldServerInfo realm)
        {
            throw new System.NotImplementedException();
        }

        public List<Character> GetCharacters()
        {
            throw new System.NotImplementedException();
        }

        public bool EnterRealm(Character character)
        {
            throw new System.NotImplementedException();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {                
                _disposed = true;
            }
        }
    }
}
