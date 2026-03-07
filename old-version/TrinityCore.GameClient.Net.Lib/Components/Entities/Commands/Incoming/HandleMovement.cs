using System;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;
using TrinityCore.GameClient.Net.Lib.Network.World.Models;

namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Commands.Incoming
{
    internal class HandleMovement : ReceivablePacket<Network.World.Enums.WorldCommand>
    {
        #region Internal Properties

        internal UInt64 Guid { get; set; }
        internal MovementLiving MovementLiving { get; set; }

        #endregion Internal Properties

        #region Internal Methods

        internal override void LoadData()
        {
            Guid = ReadPackedGuid();
            MovementLiving = new MovementLiving();
            MovementLiving.MovementFlags = (MovementTypes)ReadUInt32();
            MovementLiving.ExtraMovementFlags = (MovementOptions)ReadUInt16();
            MovementLiving.Time = ReadUInt32();
            MovementLiving.Position = new Position(ReadVector3(), ReadSingle());

            if (MovementLiving.MovementFlags.HasFlag(MovementTypes.ONTRANSPORT))
            {
                MovementLiving.TransportGuid = ReadPackedGuid();
                MovementLiving.TransportPosition = new Position(ReadVector3(), ReadSingle());

                MovementLiving.TransportTime = ReadUInt32();
                MovementLiving.TransportSeat = ReadByte();
                if (MovementLiving.ExtraMovementFlags.HasFlag(MovementOptions.INTERPOLATED_MOVEMENT))
                    MovementLiving.TransportTime2 = ReadUInt32();
            }

            if (MovementLiving.MovementFlags.HasFlag(MovementTypes.SWIMMING) || MovementLiving.MovementFlags.HasFlag(MovementTypes.FLYING)
                                                                                          || MovementLiving.ExtraMovementFlags.HasFlag(MovementOptions
                                                                                              .ALWAYS_ALLOW_PITCHING))
            {
                MovementLiving.Pitch = ReadSingle();
            }

            MovementLiving.FallTime = ReadUInt32();

            if (MovementLiving.MovementFlags.HasFlag(MovementTypes.FALLING))
            {
                MovementLiving.JumpZSpeed = ReadSingle();
                MovementLiving.JumpSinAngle = ReadSingle();
                MovementLiving.JumpCosAngle = ReadSingle();
                MovementLiving.JumpXySpeed = ReadSingle();
            }

            if (MovementLiving.MovementFlags.HasFlag(MovementTypes.SPLINE_ELEVATION))
            {
                MovementLiving.SplineElevation = ReadSingle();
            }
        }

        #endregion Internal Methods
    }
}