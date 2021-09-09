using System;
using System.Collections.Generic;
using System.Text;

namespace TrinityCore.GameClient.Net.Lib.World.Enums
{
    public enum UnitMoveType : sbyte
    {
        MOVE_WALK = 0,
        MOVE_RUN = 1,
        MOVE_RUN_BACK = 2,
        MOVE_SWIM = 3,
        MOVE_SWIM_BACK = 4,
        MOVE_TURN_RATE = 5,
        MOVE_FLIGHT = 6,
        MOVE_FLIGHT_BACK = 7,
        MOVE_PITCH_RATE = 8
    }
}
