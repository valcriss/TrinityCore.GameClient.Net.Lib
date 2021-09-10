using TrinityCore.GameClient.Net.Lib.Clients;
using TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Commands;
using TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Enums;
using TrinityCore.GameClient.Net.Lib.Log;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.Tools;
using TrinityCore.GameClient.Net.Lib.World.Enums;
using WorldState = TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Entities.WorldState;

namespace TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration
{
    public class WorldConfigurationComponent : GameComponent
    {
        public Difficulty DungeonDifficulty { get; set; }
        public Difficulty InstanceDifficulty { get; set; }
        public TutorialFlags TutorialFlags { get; set; }
        public uint Version { get; set; }
        public string Motd { get; set; }
        public AccountDataTimes DataTimes { get; set; }
        public LoginSetTimeSpeed LoginSetTimeSpeed { get; set; }
        public bool VoiceChatEnabled { get; set; }
        public UpdateWeatherState Weather { get; set; }

        public WorldState WorldState { get; set; }

        public override void RegisterHandlers()
        {
            RegisterHandler(WorldCommand.SMSG_CLIENTCACHE_VERSION, ClientCacheVersion);
            RegisterHandler(WorldCommand.SMSG_TUTORIAL_FLAGS, SetTutorialFlags);
            RegisterHandler(WorldCommand.SMSG_ACCOUNT_DATA_TIMES, AccountDataTimes);
            RegisterHandler(WorldCommand.SMSG_MOTD, ServerMotd);
            RegisterHandler(WorldCommand.MSG_SET_DUNGEON_DIFFICULTY, SetDungeonDifficulty);
            RegisterHandler(WorldCommand.SMSG_INSTANCE_DIFFICULTY, SetInstanceDifficulty);
            RegisterHandler(WorldCommand.SMSG_LOGIN_SETTIMESPEED, SetLoginSetTimeSpeed);
            RegisterHandler(WorldCommand.SMSG_FEATURE_SYSTEM_STATUS, SetFeatureSystemStatus);
            RegisterHandler(WorldCommand.SMSG_TIME_SYNC_REQ, TimeSyncRequest);
            RegisterHandler(WorldCommand.SMSG_INIT_WORLD_STATES, InitWorldStates);
            RegisterHandler(WorldCommand.SMSG_UPDATE_WORLD_STATE, UpdateWorldStates);
            RegisterHandler(WorldCommand.SMSG_WEATHER, UpdateWeather);
        }

        private void ClientCacheVersion(ReceivablePacket content)
        {
            ClientCacheVersion version = new ClientCacheVersion(content);
            Version = version.Version;
            Logger.Log("Version (" + Version + ")", LogLevel.DETAIL);
        }

        private void SetTutorialFlags(ReceivablePacket content)
        {
            TutorialFlags = new TutorialFlags(content);
        }

        private void AccountDataTimes(ReceivablePacket content)
        {
            AccountDataTimes accountDataTimes = new AccountDataTimes(content);
            DataTimes = accountDataTimes;
            Logger.Log("Server UTC DateTime " + accountDataTimes.ServerTime.ToDateTime(), LogLevel.DETAIL);
            foreach (var item in DataTimes.DataTimes)
            {
                Logger.Log("DataTime " + item.Key + " = " + item.Value, LogLevel.VERBOSE);
            }
        }

        private void ServerMotd(ReceivablePacket content)
        {
            ServerMotd serverMotd = new ServerMotd(content);
            Motd = serverMotd.Motd;
            Logger.Log("MOTD : " + serverMotd.Motd, LogLevel.INFO);
        }

        private void SetDungeonDifficulty(ReceivablePacket content)
        {
            DungeonDifficulty difficulty = new DungeonDifficulty(content);
            DungeonDifficulty = difficulty.Difficulty;
            Logger.Log("Dungeon difficulty : " + difficulty.Difficulty, LogLevel.DETAIL);
        }

        private void SetInstanceDifficulty(ReceivablePacket content)
        {
            InstanceDifficulty difficulty = new InstanceDifficulty(content);
            InstanceDifficulty = difficulty.Difficulty;
            Logger.Log("Instance difficulty : " + difficulty.Difficulty, LogLevel.DETAIL);
        }

        private void SetLoginSetTimeSpeed(ReceivablePacket content)
        {
            LoginSetTimeSpeed = new LoginSetTimeSpeed(content);
        }

        private void SetFeatureSystemStatus(ReceivablePacket content)
        {
            FeatureSystemStatus featureSystemStatus = new FeatureSystemStatus(content);
            VoiceChatEnabled = featureSystemStatus.VoiceChatEnabled;
            Logger.Log("Voice Chat Enabled : " + (VoiceChatEnabled ? "YES" : "NO"), LogLevel.DETAIL);
        }
        private void TimeSyncRequest(ReceivablePacket content)
        {
            TimeSyncRequest timeSyncRequest = new TimeSyncRequest(content);
            Logger.Log("Received TimeSyncRequest Counter ("+timeSyncRequest.SyncNextCounter+")", LogLevel.VERBOSE);
            WorldClient.Send(new TimeSyncResponse(WorldClient, timeSyncRequest.SyncNextCounter));
        }

        private void InitWorldStates(ReceivablePacket content)
        {
            InitWorldStates initWorldStates = new InitWorldStates(content);
            WorldState = initWorldStates.WorldState;
            Logger.Log("World State Initialized", LogLevel.DETAIL);
        }

        private void UpdateWorldStates(ReceivablePacket content)
        {
            UpdateWorldStates updateWorldStates = new UpdateWorldStates(content);
            WorldState.UpdateVariable(updateWorldStates.Variable);
            Logger.Log("World State Updated", LogLevel.VERBOSE);
        }

        private void UpdateWeather(ReceivablePacket content)
        {
            UpdateWeatherState updateWeatherState = new UpdateWeatherState(content);
            Weather = updateWeatherState;
            Logger.Log("Current Weather : "+Weather.State, LogLevel.DETAIL);
        }
    }
}
