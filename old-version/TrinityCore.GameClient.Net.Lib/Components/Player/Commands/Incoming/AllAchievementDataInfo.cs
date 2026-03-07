using System;
using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Components.Player.Models;
using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands.Incoming
{
    internal class AllAchievementDataInfo : ReceivablePacket<Network.World.Enums.WorldCommand>
    {
        #region Internal Properties

        internal List<AchievementCriteria> AchievementCriteriaList { get; set; }
        internal List<CompletedAchievement> CompletedAchievements { get; set; }

        #endregion Internal Properties

        #region Internal Methods

        internal override void LoadData()
        {
            CompletedAchievements = new List<CompletedAchievement>();
            AchievementCriteriaList = new List<AchievementCriteria>();
            uint achievementId = ReadUInt32();
            while (achievementId != 0xFFFFFFFF)
            {
                DateTime time = ReadPackedTime();

                CompletedAchievements.Add(new CompletedAchievement
                {
                    AchievementId = achievementId,
                    Date = time
                });
                achievementId = ReadUInt32();
            }

            uint criteriaId = ReadUInt32();
            while (criteriaId != 0xFFFFFFFF)
            {
                ulong criteriaCounter = ReadPackedGuid();
                ReadPackedGuid(); // PlayerGuid
                ReadInt32(); // 0
                DateTime time = ReadPackedTime();
                ReadInt32(); // 0
                ReadInt32(); // 0

                AchievementCriteriaList.Add(new AchievementCriteria
                {
                    CriteriaId = criteriaId,
                    Counter = criteriaCounter,
                    Date = time
                });
                criteriaId = ReadUInt32();
            }
        }

        #endregion Internal Methods
    }
}