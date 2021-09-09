using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.World.Enums;
using TrinityCore.GameClient.Net.Lib.World.Navigation;

namespace TrinityCore.GameClient.Net.Lib.World.Entities
{
    public class MovementLiving
    {
        public MovementLiving()
        {
            Speeds = new Dictionary<UnitMoveType, float>();
        }
        public MovementFlags MovementFlags { get; set; }
        public MovementFlags2 ExtraMovementFlags { get; set; }
        public uint Time { get; set; }
        public Position Position { get; set; }

        // MOVEMENTFLAG_ONTRANSPORT
        public ulong? TransportGuid { get; set; }
        public Position TransportPosition { get; set; }
        public byte? TransportSeat { get; set; }
        public ulong? TransportTime { get; set; }
        public ulong? TransportTime2 { get; set; }

        // MOVEMENTFLAG_SWIMMING || MOVEMENTFLAG_FLYING || MOVEMENTFLAG2_ALWAYS_ALLOW_PITCHING
        public float? Pitch { get; set; }

        public uint FallTime { get; set; }

        // MOVEMENTFLAG_FALLING
        public float? JumpZSpeed { get; set; }
        public float? JumpSinAngle { get; set; }
        public float? JumpCosAngle { get; set; }
        public float? JumpXySpeed { get; set; }

        // MOVEMENTFLAG_SPLINE_ELEVATION
        public float? SplineElevation { get; set; }

        // SPEEDS
        public Dictionary<UnitMoveType, float> Speeds { get; set; }

        // MOVEMENTFLAG_SPLINE_ENABLED
        public MovementSpline MovementSpline { get; set; }



    }
}
