using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using TrinityCore.GameClient.Net.Lib.Log;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.Entities;
using TrinityCore.GameClient.Net.Lib.Network.Tools;
using TrinityCore.GameClient.Net.Lib.World.Commands;
using TrinityCore.GameClient.Net.Lib.World.Entities;
using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.World
{
    public class WorldClient : WorldSocket
    {
        private WorldPlayerState playerState;
        private WorldState state;

        internal Character Character { get; set; }
        internal Query Query { get; set; }
        private ManualResetEvent AuthenticateDone { get; }
        private ManualResetEvent CharacterListDone { get; }
        private ManualResetEvent CharacterLoginDone { get; }
        private List<Character> Characters { get; set; }
        private InternalPacketsHandler InternalsHandler { get; }
        private System.Timers.Timer KeepAliveTimer { get; }
        private ManualResetEvent LogOutDone { get; }
        private WorldPlayerState PlayerState
        {
            get=> playerState;            
            set
            {
                Logger.Log("PlayerState Updated : " + playerState + " -> " + value, LogLevel.VERBOSE);
                playerState = value;
            }
        }

        private BigInteger SessionKey { get; }
        private WorldState State
        {
            get => state; 
            set
            {
                Logger.Log("WorldState Updated : " + state + " -> " + value, LogLevel.VERBOSE);
                state = value;
            }
        }
        private string Username { get; }
        private WorldServerInfo WorldServer { get; }

        internal WorldClient(WorldServerInfo worldServer, string username, BigInteger sessionKey) : base(
            worldServer.Address, worldServer.Port)
        {
            State = WorldState.DISCONNECTED;
            PlayerState = WorldPlayerState.NOT_LOGGED_IN;
            AuthenticateDone = new ManualResetEvent(false);
            CharacterListDone = new ManualResetEvent(false);
            CharacterLoginDone = new ManualResetEvent(false);
            LogOutDone = new ManualResetEvent(false);

            Query = new Query(this);

            WorldServer = worldServer;
            SessionKey = sessionKey;
            Username = username.ToUpper();

            InternalsHandler = new InternalPacketsHandler();

            PacketsHandler.RegisterHandler(WorldCommand.SERVER_AUTH_CHALLENGE, AuthChallengeRequest);
            PacketsHandler.RegisterHandler(WorldCommand.SERVER_AUTH_RESPONSE, AuthChallengeResponse);
            PacketsHandler.RegisterHandler(WorldCommand.SMSG_CHAR_ENUM, CharactersListResponse);
            PacketsHandler.RegisterHandler(WorldCommand.SMSG_LOGIN_VERIFY_WORLD, LoginVerifyResponse);
            PacketsHandler.RegisterHandler(WorldCommand.SMSG_LOGOUT_RESPONSE, LogOutResponse);
            PacketsHandler.RegisterHandler(WorldCommand.SMSG_LOGOUT_COMPLETE, LogOutComplete);
            PacketsHandler.RegisterHandler(WorldCommand.SMSG_UPDATE_OBJECT, UpdateObject);

            KeepAliveTimer = new System.Timers.Timer(15000) { Enabled = true };
            KeepAliveTimer.Elapsed += HandleKeepAlive;
            KeepAliveTimer.Start();


        }

        internal void AppendInternalHandler(Internals command, InternalPacketHandler handler)
        {
            lock (InternalsHandler)
            {
                InternalsHandler.RegisterHandler(command, handler);
            }
        }

        internal virtual async Task<bool> Disconnect()
        {
            return await Task.Run(() =>
            {
                LogOutDone.Reset();
                Send(new ClientLogOutRequest(this));
                LogOutDone.WaitOne(30000);

                return Close();
            });
        }

        internal virtual async Task<List<Character>> GetCharacters()
        {
            return await Task.Run(() =>
            {
                Characters = null;
                if (State != WorldState.AUTHENTICATED) return Characters;
                Send(new CharactersListRequest(this));
                CharacterListDone.Reset();
                CharacterListDone.WaitOne(10000);
                return Characters;
            });
        }

        internal virtual async Task<bool> LoginCharacter(Character character)
        {
            return await Task.Run(() =>
            {
                PlayerState = WorldPlayerState.NOT_LOGGED_IN;
                Character = character;
                Send(new CharacterLoginRequest(this, character));
                CharacterLoginDone.Reset();
                CharacterLoginDone.WaitOne(10000);
                return PlayerState == WorldPlayerState.LOGGED_IN;
            });
        }

        internal virtual async Task<bool> Start()
        {
            return await Task.Run(() =>
            {
                State = WorldState.DISCONNECTED;
                Connect().Wait();
                Characters = null;
                AuthenticateDone.Reset();
                AuthenticateDone.WaitOne(10000);
                return State == WorldState.AUTHENTICATED;
            });
        }

        private void AuthChallengeRequest(ReceivablePacket content)
        {
            AuthChallengeRequest authChallengeRequest = new AuthChallengeRequest(content);
            Send(new ClientAuthChallenge(this, Username, authChallengeRequest.ServerSeed, SessionKey, WorldServer));
            AuthenticationCrypto.Initialize(SessionKey.ToCleanByteArray());
        }

        public override bool Send(SendablePacket sendablePacket)
        {
            Logger.Log("Sending : " + sendablePacket.GetType().Name);
            return base.Send(sendablePacket);
        }

        private void AuthChallengeResponse(ReceivablePacket content)
        {
            AuthChallengeResponse authChallengeResponse = new AuthChallengeResponse(content);
            if (authChallengeResponse.Success)
            {
                State = WorldState.AUTHENTICATED;
                AuthenticateDone.Set();
            }
            else
            {
                State = WorldState.ERROR;
                AuthenticateDone.Set();
            }
        }

        private void CharactersListResponse(ReceivablePacket content)
        {
            CharactersListResponse charactersListResponse = new CharactersListResponse(content);
            Characters = charactersListResponse.Characters.ToList();
            CharacterListDone.Set();
        }

        private void HandleKeepAlive(object sender, ElapsedEventArgs e)
        {
            if (State != WorldState.AUTHENTICATED) return;
            if (PlayerState != WorldPlayerState.LOGGED_IN) return;
            Logger.Log("Sending KeepAlive", LogLevel.VERBOSE);
            Send(new KeepAlive(this));
        }

        private void LoginVerifyResponse(ReceivablePacket content)
        {
            LoginVerifyResponse serverLoginVerify = new LoginVerifyResponse(content);
            Character.MapId = serverLoginVerify.MapId;
            Character.X = serverLoginVerify.X;
            Character.Y = serverLoginVerify.Y;
            Character.Z = serverLoginVerify.Z;
            Character.O = serverLoginVerify.O;
            PlayerState = WorldPlayerState.LOGGED_IN;
            CharacterLoginDone.Set();
        }

        private void LogOutComplete(ReceivablePacket content)
        {
            PlayerState = WorldPlayerState.NOT_LOGGED_IN;
            LogOutDone.Set();
        }

        private void LogOutResponse(ReceivablePacket content)
        {
            LogOutResponse response = new LogOutResponse(content);
            if (response.LogOut)
            {
                PlayerState = WorldPlayerState.NOT_LOGGED_IN;
                LogOutDone.Set();
            }
        }

        private void UpdateObject(ReceivablePacket content)
        {
            UpdateObject updateObject = new UpdateObject(content);
            if (updateObject.UpdateValues.Count > 0)
            {
                InternalsHandler.Handle(Internals.UPDATE_FIELDS, updateObject.UpdateValues);
            }

            if (updateObject.UpdateCreateObjects.Count > 0)
            {
                InternalsHandler.Handle(Internals.CREATE_OBJECTS, updateObject.UpdateCreateObjects);
            }

            if (updateObject.Movements.Count > 0)
            {
                InternalsHandler.Handle(Internals.MOVEMENTS, updateObject.Movements);
            }

            if (updateObject.UpdateOutOfRanges.Count > 0)
            {
                InternalsHandler.Handle(Internals.OUT_OF_RANGES, updateObject.UpdateOutOfRanges);
            }
        }
    }
}
