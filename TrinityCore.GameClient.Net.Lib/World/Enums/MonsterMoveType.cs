using System;
using System.Collections.Generic;
using System.Text;

namespace TrinityCore.GameClient.Net.Lib.World.Enums
{
    public enum MonsterMoveType
    {
        MonsterMoveNormal = 0,
        MonsterMoveStop = 1,
        MonsterMoveFacingSpot = 2,
        MonsterMoveFacingTarget = 3,
        MonsterMoveFacingAngle = 4
    }
}
