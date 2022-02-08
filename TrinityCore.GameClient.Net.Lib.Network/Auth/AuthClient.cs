using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Network.Auth.Commands.Incoming;
using TrinityCore.GameClient.Net.Lib.Network.Auth.Commands.Outgoing;
using TrinityCore.GameClient.Net.Lib.Network.Auth.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Auth.Models;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.Security;

namespace TrinityCore.GameClient.Net.Lib.Network.Auth
{
    public class AuthClient : AuthSocket
    {
        #region Public Properties

        public AuthCredentials Credentials { get; set; }

        #endregion Public Properties

        #region Private Properties

        private ManualResetEvent AuthenticateDone { get; }
        private ManualResetEvent RealmListDone { get; }
        private List<WorldServerInfo> Realms { get; set; }
        private AuthState State { get; set; }

        #endregion Private Properties

        #region Private Fields

        private const int AUTHENTIFICATION_TIMEOUT = 5000;
        private const int REALM_LIST_TIMEOUT = 5000;

        #endregion Private Fields

        #region Public Constructors

        public AuthClient(string host, int port, string username, string password) : base(host, port)
        {
            AuthenticateDone = new ManualResetEvent(false);
            RealmListDone = new ManualResetEvent(false);
            Credentials = new AuthCredentials(username, password);
            State = AuthState.DISCONNECTED;
            GameSocketConnected += OnGameSocketConnected;
            GameSocketDisconnected += OnGameSocketDisconnected;
            PacketsHandler.RegisterHandler(AuthCommand.LOGON_CHALLENGE, LogonChallengeResult);
            PacketsHandler.RegisterHandler(AuthCommand.LOGON_PROOF, AuthProofResult);
            PacketsHandler.RegisterHandler(AuthCommand.REALM_LIST, RealmListResult);
        }

        #endregion Public Constructors

        #region Public Methods

        public async Task<bool> Authenticate()
        {
            return await Task.Run(() =>
            {
                State = AuthState.DISCONNECTED;
                if (!Connect()) return false;
                AuthenticateDone.Reset();
                AuthenticateDone.WaitOne(AUTHENTIFICATION_TIMEOUT);
                return State == AuthState.AUTHENTICATED;
            });
        }

        public async Task<List<WorldServerInfo>> GetRealms()
        {
            return await Task.Run(() =>
            {
                Realms = null;
                if (State != AuthState.AUTHENTICATED) return Realms;
                Send(new RealmListRequest());
                RealmListDone.Reset();
                RealmListDone.WaitOne(REALM_LIST_TIMEOUT);
                return Realms;
            });
        }

        #endregion Public Methods

        #region Protected Methods

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    AuthenticateDone.Dispose();
                    RealmListDone.Dispose();
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }

        #endregion Protected Methods

        #region Private Methods

        private void AuthProofResult(ReceivablePacket<AuthCommand> content)
        {
            AuthProofResponse response = new AuthProofResponse(content);
            State = AuthProof.IsProofValid(response.M2, Credentials.SessionProof) ? AuthState.AUTHENTICATED : AuthState.ERROR;
            AuthenticateDone.Set();
        }

        private void LogonChallengeResult(ReceivablePacket<AuthCommand> content)
        {
            LogonChallengeResponse response = new LogonChallengeResponse(content);
            if (response.Error != AuthResult.SUCCESS)
            {
                State = AuthState.ERROR;
                AuthenticateDone.Set();
                return;
            }

            AuthProof proof = AuthProof.ComputeAuthProof(Credentials, response);

            Credentials.SessionKey = proof.Key;
            Credentials.SessionProof = proof.Proof;

            Send(new AuthProofRequest(proof.Y, proof.M1Hash, new byte[20]));
        }

        private void OnGameSocketConnected()
        {
            State = AuthState.CONNECTED;
            Credentials.IPAddress = GetIpAddress();
            Send(new LogonChallengeRequest(Credentials));
        }

        private void OnGameSocketDisconnected()
        {
            State = AuthState.DISCONNECTED;
        }

        private void RealmListResult(ReceivablePacket<AuthCommand> content)
        {
            RealmListResponse response = new RealmListResponse(content);
            Realms = response.Realms;
            RealmListDone.Set();
        }

        #endregion Private Methods
    }
}