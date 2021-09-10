using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Auth.Commands;
using TrinityCore.GameClient.Net.Lib.Auth.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.Entities;

namespace TrinityCore.GameClient.Net.Lib.Auth
{
    public class AuthClient : AuthSocket
    {
        public BigInteger SessionKey { get; set; }
        private ManualResetEvent AuthenticateDone { get; }
        private string Password { get; }
        private ManualResetEvent RealmListDone { get; }
        private List<WorldServerInfo> Realms { get; set; }
        private byte[] SessionProof { get; set; }
        private AuthState State { get; set; }
        public string Username { get; }

        public AuthClient(string host,int port, string username, string password) : base(host, port)
        {
            AuthenticateDone = new ManualResetEvent(false);
            RealmListDone = new ManualResetEvent(false);
            Realms = null;
            Username = username.ToUpper();
            Password = password;
            OnGameSocketConnected += AuthClientGameSocketConnected;
            PacketsHandler.RegisterHandler(AuthCommand.LOGON_CHALLENGE, LogonChallenge);
            PacketsHandler.RegisterHandler(AuthCommand.LOGON_PROOF, AuthProof);
            PacketsHandler.RegisterHandler(AuthCommand.REALM_LIST, RealmList);
        }

        public async Task<List<WorldServerInfo>> GetWorlds()
        {
            return await Task.Run(() =>
            {
                Realms = null;
                if (State != AuthState.AUTHENTICATED) return Realms;
                Send(new RealmListRequest());
                RealmListDone.Reset();
                RealmListDone.WaitOne(10000);
                return Realms;
            });
        }

        public async Task<bool> Authenticate()
        {
            return await Task.Run(() =>
            {
                State = AuthState.DISCONNECTED;
                Connect().Wait();
                Realms = null;
                AuthenticateDone.Reset();
                AuthenticateDone.WaitOne(10000);
                return State == AuthState.AUTHENTICATED;
            });
        }

        private void AuthClientGameSocketConnected(GameSocket sender)
        {
            Send(new LogonChallengeRequest(Username, GetIpAddress()));
        }

        private void AuthProof(ReceivablePacket content)
        {
            AuthProofResponse response = new AuthProofResponse(content, SessionProof);
            State = response.IsValid ? AuthState.AUTHENTICATED : AuthState.ERROR;
            AuthenticateDone.Set();
        }

        private void LogonChallenge(ReceivablePacket content)
        {
            LogonChallengeResponse response = new LogonChallengeResponse(content, Username, Password);
            if (response.Error != AuthResult.SUCCESS)
            {
                State = AuthState.ERROR;
                AuthenticateDone.Set();
                return;
            }

            SessionKey = response.Key;
            SessionProof = response.Proof;
            Send(response.AuthProofRequest);
        }

        private void RealmList(ReceivablePacket content)
        {
            RealmListResponse response = new RealmListResponse(content);
            Realms = response.Realms;
            RealmListDone.Set();
        }
    }
}
