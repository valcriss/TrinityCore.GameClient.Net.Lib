using System;
using System.Collections.Generic;
using System.Text;

namespace TrinityCore.GameClient.Net.Lib.World.Enums
{
    public enum CreatureEliteType
    {
        CREATURE_ELITE_NORMAL = 0,
        CREATURE_ELITE_ELITE = 1,
        CREATURE_ELITE_RAREELITE = 2,
        CREATURE_ELITE_WORLDBOSS = 3,
        CREATURE_ELITE_RARE = 4,
        CREATURE_UNKNOWN = 5 // found in 2.2.3 for 2 mobs
    }
}
