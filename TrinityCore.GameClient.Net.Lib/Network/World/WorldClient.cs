using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using TrinityCore.GameClient.Net.Lib.Network.Auth;
using TrinityCore.GameClient.Net.Lib.Network.Auth.Models;
using TrinityCore.GameClient.Net.Lib.Network.Security;
using TrinityCore.GameClient.Net.Lib.Network.Tools;
using TrinityCore.GameClient.Net.Lib.Network.World.Commands.Incoming;
using TrinityCore.GameClient.Net.Lib.Network.World.Commands.Outgoing;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;
using TrinityCore.GameClient.Net.Lib.Network.World.Models;

namespace TrinityCore.GameClient.Net.Lib.Network.World
{
    public class WorldClient : WorldSocket
    {
        #region Public Events

        public event CharacterLoggedInEventHandler CharacterLoggedIn;

        public event CharacterLoggedOutEventHandler CharacterLoggedOut;

        #endregion Public Events

        #region Private Properties

        private ManualResetEvent AuthenticateDone { get; }
        private Character Character { get; set; }
        private ManualResetEvent CharacterListDone { get; }
        private ManualResetEvent CharacterLoginDone { get; }
        private List<Character> Characters { get; set; }
        private AuthCredentials Credentials { get; set; }
        private System.Timers.Timer KeepAliveTimer { get; }
        private ManualResetEvent LogOutDone { get; }
        private WorldPlayerState PlayerState { get; set; }
        private WorldState State { get; set; }
        private WorldServerInfo WorldServer { get; set; }

        #endregion Private Properties

        #region Private Fields

        private const int AUTHENTIFICATION_TIMEOUT = 3000;
        private const int CHARACTER_LOGIN_TIMEOUT = 3000;
        private const int CHARACTERS_LIST_TIMEOUT = 2000;

        #endregion Private Fields

        #region Public Constructors

        public WorldClient()
        {
            AuthenticateDone = new ManualResetEvent(false);
            CharacterListDone = new ManualResetEvent(false);
            CharacterLoginDone = new ManualResetEvent(false);
            LogOutDone = new ManualResetEvent(false);

            GameSocketConnected += OnGameSocketConnected;
            GameSocketDisconnected += OnGameSocketDisconnected;

            AuthenticationCrypto.Reset();

            PacketsHandler.RegisterHandler<ServerAuthChallengeRequest>(WorldCommand.SERVER_AUTH_CHALLENGE, ServerAuthChallengeRequest);
            PacketsHandler.RegisterHandler<ClientAuthChallengeResponse>(WorldCommand.SERVER_AUTH_RESPONSE, ClientAuthChallengeResponse);
            PacketsHandler.RegisterHandler<CharactersListResponse>(WorldCommand.SMSG_CHAR_ENUM, CharactersListResponse);
            PacketsHandler.RegisterHandler<LoginCharacterResponse>(WorldCommand.SMSG_LOGIN_VERIFY_WORLD, LoginCharacterResponse);
            PacketsHandler.RegisterHandler<ClientLogOutResponse>(WorldCommand.SMSG_LOGOUT_RESPONSE, ClientLogOutResponse);
            PacketsHandler.RegisterHandler<ClientLogOutComplete>(WorldCommand.SMSG_LOGOUT_COMPLETE, ClientLogOutComplete);
            PacketsHandler.RegisterHandler<ServerTimeSyncRequest>(WorldCommand.SMSG_TIME_SYNC_REQ, ServerTimeSyncRequest);

            KeepAliveTimer = new System.Timers.Timer(15000) { Enabled = true };
            KeepAliveTimer.Elapsed += HandleKeepAlive;
            KeepAliveTimer.Start();
        }

        #endregion Public Constructors

        #region Public Methods

        public async Task<bool> Authenticate(WorldServerInfo worldServer, AuthCredentials credentials)
        {
            return await Task.Run(() =>
            {
                WorldServer = worldServer;
                Credentials = credentials;

                State = WorldState.DISCONNECTED;
                PlayerState = WorldPlayerState.NOT_LOGGED_IN;
                if (!Connect(worldServer.Address, worldServer.Port)) return false;
                AuthenticateDone.Reset();
                AuthenticateDone.WaitOne(AUTHENTIFICATION_TIMEOUT);
                return State == WorldState.AUTHENTICATED;
            });
        }

        public async Task<List<Character>> GetCharacters()
        {
            return await Task.Run(() =>
            {
                Characters = null;
                if (State != WorldState.AUTHENTICATED) return Characters;
                Send(new CharactersListRequest());
                CharacterListDone.Reset();
                CharacterListDone.WaitOne(CHARACTERS_LIST_TIMEOUT);
                return Characters;
            });
        }

        public async Task<bool> LoginCharacter(Character character)
        {
            return await Task.Run(() =>
            {
                PlayerState = WorldPlayerState.NOT_LOGGED_IN;
                Character = character;
                Send(new CharacterLoginRequest(character));
                CharacterLoginDone.Reset();
                CharacterLoginDone.WaitOne(CHARACTER_LOGIN_TIMEOUT);
                return PlayerState == WorldPlayerState.LOGGED_IN;
            });
        }

        public async Task<bool> LogOut()
        {
            return await Task.Run(() =>
            {
                LogOutDone.Reset();
                Send(new ClientLogOutRequest());
                LogOutDone.WaitOne(40000);
                if (Character == null) CharacterLoggedOut?.Invoke();
                return Character == null;
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
                    KeepAliveTimer.Dispose();
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }

        #endregion Protected Methods

        #region Private Methods

        private bool CharactersListResponse(CharactersListResponse charactersListResponse)
        {
            Characters = charactersListResponse.Characters.ToList();
            return CharacterListDone.Set();
        }

        private bool ClientAuthChallengeResponse(ClientAuthChallengeResponse authChallengeResponse)
        {
            State = (authChallengeResponse.Success) ? WorldState.AUTHENTICATED : WorldState.ERROR;
            return AuthenticateDone.Set();
        }

        private bool ClientLogOutComplete(ClientLogOutComplete clientLogOutComplete)
        {
            Character = null;
            PlayerState = WorldPlayerState.NOT_LOGGED_IN;
            return LogOutDone.Set();
        }

        private bool ClientLogOutResponse(ClientLogOutResponse logOutResponse)
        {
            if (logOutResponse.LogOut)
            {
                Character = null;
                PlayerState = WorldPlayerState.NOT_LOGGED_IN;
                return LogOutDone.Set();
            }
            return true;
        }

        private void HandleKeepAlive(object sender, ElapsedEventArgs e)
        {
            if (State != WorldState.AUTHENTICATED) return;
            if (PlayerState != WorldPlayerState.LOGGED_IN) return;
            Send(new ClientKeepAliveRequest());
        }

        private bool LoginCharacterResponse(LoginCharacterResponse loginCharacterResponse)
        {
            Character.MapId = loginCharacterResponse.MapId;
            Character.X = loginCharacterResponse.X;
            Character.Y = loginCharacterResponse.Y;
            Character.Z = loginCharacterResponse.Z;
            Character.O = loginCharacterResponse.O;
            PlayerState = WorldPlayerState.LOGGED_IN;
            CharacterLoggedIn?.Invoke(Character);
            return CharacterLoginDone.Set();
        }

        private void OnGameSocketConnected()
        {
            State = WorldState.CONNECTED;
            PlayerState = WorldPlayerState.NOT_LOGGED_IN;
            Character = null;
        }

        private void OnGameSocketDisconnected()
        {
            State = WorldState.DISCONNECTED;
            PlayerState = WorldPlayerState.NOT_LOGGED_IN;
            Character = null;
            CharacterLoggedOut?.Invoke();
        }

        private bool ServerAuthChallengeRequest(ServerAuthChallengeRequest serverAuthChallengeRequest)
        {
            Send(new ClientAuthChallengeRequest(Credentials, WorldServer, serverAuthChallengeRequest.ServerSeed));
            AuthenticationCrypto.Instance.Initialize(Credentials.SessionKey.ToCleanByteArray());
            return true;
        }

        private bool ServerTimeSyncRequest(ServerTimeSyncRequest timeSyncRequest)
        {
            return Send(new ClientTimeSyncResponse(timeSyncRequest.SyncNextCounter));
        }

        #endregion Private Methods
    }
}