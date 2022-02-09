﻿using TrinityCore.GameClient.Net.Lib.Components.Environment.Commands.Incoming;
using TrinityCore.GameClient.Net.Lib.Components.Environment.Enums;
using TrinityCore.GameClient.Net.Lib.Components.Environment.Models;
using TrinityCore.GameClient.Net.Lib.Logging;
using TrinityCore.GameClient.Net.Lib.Network.World;

namespace TrinityCore.GameClient.Net.Lib.Components.Environment
{
    public class EnvironmentComponent : Component
    {
        internal EnvironmentComponent(WorldClient worldClient) : base(worldClient)
        {
            WorldClient.PacketsHandler.RegisterHandler<ClientCacheVersionInfo>(Network.World.Enums.WorldCommand.SMSG_CLIENTCACHE_VERSION, ClientCacheVersionInfo);
            WorldClient.PacketsHandler.RegisterHandler<FeatureSystemStatusInfo>(Network.World.Enums.WorldCommand.SMSG_FEATURE_SYSTEM_STATUS, FeatureSystemStatusInfo);
            WorldClient.PacketsHandler.RegisterHandler<DungeonDifficultyInfo>(Network.World.Enums.WorldCommand.MSG_SET_DUNGEON_DIFFICULTY, DungeonDifficultyInfo);
            WorldClient.PacketsHandler.RegisterHandler<InstanceDifficultyInfo>(Network.World.Enums.WorldCommand.SMSG_INSTANCE_DIFFICULTY, InstanceDifficultyInfo);
            WorldClient.PacketsHandler.RegisterHandler<TutorialFlagsInfo>(Network.World.Enums.WorldCommand.SMSG_TUTORIAL_FLAGS, TutorialFlagsInfo);
            WorldClient.PacketsHandler.RegisterHandler<ServerMotdInfo>(Network.World.Enums.WorldCommand.SMSG_MOTD, ServerMotdInfo);
        }

        public uint Version { get; set; }
        public bool VoiceChatEnabled { get; set; }
        public string Motd { get; set; }
        public Difficulty DungeonDifficulty { get; set; }
        public Difficulty InstanceDifficulty { get; set; }
        public TutorialFlags TutorialFlags { get; set; }

        private bool ClientCacheVersionInfo(ClientCacheVersionInfo clientCacheVersion)
        {
            Version = clientCacheVersion.Version;
            Logger.Append(Logging.Enums.LogCategory.ENVIRONMENT, Logging.Enums.LogLevel.DEBUG, $"Version : {Version}");
            return true;
        }
        private bool FeatureSystemStatusInfo(FeatureSystemStatusInfo clientCacheVersion)
        {
            VoiceChatEnabled = clientCacheVersion.VoiceChatEnabled;
            Logger.Append(Logging.Enums.LogCategory.ENVIRONMENT, Logging.Enums.LogLevel.DEBUG, $"VoiceChatEnabled : {VoiceChatEnabled}");
            return true;
        }
        private bool DungeonDifficultyInfo(DungeonDifficultyInfo dungeonDifficulty)
        {
            DungeonDifficulty = dungeonDifficulty.Difficulty;
            Logger.Append(Logging.Enums.LogCategory.ENVIRONMENT, Logging.Enums.LogLevel.DEBUG, $"DungeonDifficulty : {DungeonDifficulty}");
            return true;
        }
        private bool InstanceDifficultyInfo(InstanceDifficultyInfo instanceDifficulty)
        {
            InstanceDifficulty = instanceDifficulty.Difficulty;
            Logger.Append(Logging.Enums.LogCategory.ENVIRONMENT, Logging.Enums.LogLevel.DEBUG, $"InstanceDifficulty : {InstanceDifficulty}");
            return true;
        }
        private bool TutorialFlagsInfo(TutorialFlagsInfo tutorialFlagsInfo)
        {
            TutorialFlags = tutorialFlagsInfo.TutorialFlags;
            Logger.Append(Logging.Enums.LogCategory.ENVIRONMENT, Logging.Enums.LogLevel.DEBUG, $"TutorialFlags : {TutorialFlags}");
            return true;
        }

        private bool ServerMotdInfo(ServerMotdInfo serverMotdInfo)
        {
            Motd = serverMotdInfo.Motd;
            Logger.Append(Logging.Enums.LogCategory.ENVIRONMENT, Logging.Enums.LogLevel.DEBUG, $"Motd : {Motd}");
            return true;
        }       
    }
}
