using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.World.Enums;
using TrinityCore.GameClient.Net.Lib.World.Navigation;

namespace TrinityCore.GameClient.Net.Lib.World.Entities
{
    internal class MovementLiving
    {
        internal MovementLiving()
        {
            Speeds = new Dictionary<UnitMoveType, float>();
        }
        internal MovementFlags MovementFlags { get; set; }
        internal MovementFlags2 ExtraMovementFlags { get; set; }
        internal uint Time { get; set; }
        internal Position Position { get; set; }

        // MOVEMENTFLAG_ONTRANSPORT
        internal ulong? TransportGuid { get; set; }
        internal Position TransportPosition { get; set; }
        internal byte? TransportSeat { get; set; }
        internal ulong? TransportTime { get; set; }
        internal ulong? TransportTime2 { get; set; }

        // MOVEMENTFLAG_SWIMMING || MOVEMENTFLAG_FLYING || MOVEMENTFLAG2_ALWAYS_ALLOW_PITCHING
        internal float? Pitch { get; set; }

        internal uint FallTime { get; set; }

        // MOVEMENTFLAG_FALLING
        internal float? JumpZSpeed { get; set; }
        internal float? JumpSinAngle { get; set; }
        internal float? JumpCosAngle { get; set; }
        internal float? JumpXySpeed { get; set; }

        // MOVEMENTFLAG_SPLINE_ELEVATION
        internal float? SplineElevation { get; set; }

        // SPEEDS
        internal Dictionary<UnitMoveType, float> Speeds { get; set; }

        // MOVEMENTFLAG_SPLINE_ENABLED
        internal MovementSpline MovementSpline { get; set; }



    }
}
