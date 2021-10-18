using System;
using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Components.Player.Entities;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands
{
    internal class AllAchievementData : WorldReceivablePacket
    {
        internal List<AchievementCriteria> AchievementCriteriaList { get; set; }
        internal List<CompletedAchievement> CompletedAchievements { get; set; }

        internal AllAchievementData(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            CompletedAchievements = new List<CompletedAchievement>();
            AchievementCriteriaList = new List<AchievementCriteria>();
            for (; ; )
            {
                uint achievementId = ReadUInt32();
                if (achievementId == 0xFFFFFFFF)
                    break;

                DateTime time = ReadPackedTime();

                CompletedAchievements.Add(new CompletedAchievement
                {
                    AchievementId = achievementId,
                    Date = time
                });
            }

            for (; ; )
            {
                uint criteriaId = ReadUInt32();
                if (criteriaId == 0xFFFFFFFF)
                    break;
                ulong criteriaCounter = ReadPackedGuid();
                ulong playerGuid = ReadPackedGuid();
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
            }
        }
    }
}
