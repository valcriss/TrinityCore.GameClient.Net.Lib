using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Models
{
    internal class MovementLiving
    {
        #region Internal Properties

        internal MovementFlags2 ExtraMovementFlags { get; set; }

        internal uint FallTime { get; set; }

        internal float? JumpCosAngle { get; set; }

        internal float? JumpSinAngle { get; set; }

        internal float? JumpXySpeed { get; set; }

        // MOVEMENTFLAG_FALLING
        internal float? JumpZSpeed { get; set; }

        internal MovementFlags MovementFlags { get; set; }

        // MOVEMENTFLAG_SPLINE_ENABLED
        internal MovementSpline MovementSpline { get; set; }

        // MOVEMENTFLAG_SWIMMING || MOVEMENTFLAG_FLYING || MOVEMENTFLAG2_ALWAYS_ALLOW_PITCHING
        internal float? Pitch { get; set; }

        internal Position Position { get; set; }

        // SPEEDS
        internal Dictionary<UnitMoveType, float> Speeds { get; set; }

        // MOVEMENTFLAG_SPLINE_ELEVATION
        internal float? SplineElevation { get; set; }

        internal uint Time { get; set; }

        // MOVEMENTFLAG_ONTRANSPORT
        internal ulong? TransportGuid { get; set; }

        internal Position TransportPosition { get; set; }

        internal byte? TransportSeat { get; set; }

        internal ulong? TransportTime { get; set; }

        internal ulong? TransportTime2 { get; set; }

        #endregion Internal Properties

        #region Internal Constructors

        internal MovementLiving()
        {
            Speeds = new Dictionary<UnitMoveType, float>();
        }

        #endregion Internal Constructors
    }
}