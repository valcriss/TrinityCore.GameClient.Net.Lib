using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;
using TrinityCore.GameClient.Net.Lib.Network.World.Models;

namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Commands.Incoming
{
    internal class HandleMovement : ReceivablePacket<Network.World.Enums.WorldCommand>
    {
        internal UInt64 Guid { get; set; }
        internal MovementLiving MovementLiving { get; set; }

        internal override void LoadData()
        {
            Guid = ReadPackedGuid();
            MovementLiving = new MovementLiving();
            MovementLiving.MovementFlags = (MovementFlags)ReadUInt32();
            MovementLiving.ExtraMovementFlags = (MovementFlags2)ReadUInt16();
            MovementLiving.Time = ReadUInt32();
            MovementLiving.Position = new Position(ReadVector3(), ReadSingle());


            if (MovementLiving.MovementFlags.HasFlag(MovementFlags.MOVEMENTFLAG_ONTRANSPORT))
            {
                MovementLiving.TransportGuid = ReadPackedGuid();
                MovementLiving.TransportPosition = new Position(ReadVector3(), ReadSingle());

                MovementLiving.TransportTime = ReadUInt32();
                MovementLiving.TransportSeat = ReadByte();
                if (MovementLiving.ExtraMovementFlags.HasFlag(MovementFlags2.MOVEMENTFLAG2_INTERPOLATED_MOVEMENT))
                    MovementLiving.TransportTime2 = ReadUInt32();
            }

            if (MovementLiving.MovementFlags.HasFlag(MovementFlags.MOVEMENTFLAG_SWIMMING) || MovementLiving.MovementFlags.HasFlag(MovementFlags.MOVEMENTFLAG_FLYING)
                                                                                          || MovementLiving.ExtraMovementFlags.HasFlag(MovementFlags2
                                                                                              .MOVEMENTFLAG2_ALWAYS_ALLOW_PITCHING))
            {
                MovementLiving.Pitch = ReadSingle();
            }

            MovementLiving.FallTime = ReadUInt32();

            if (MovementLiving.MovementFlags.HasFlag(MovementFlags.MOVEMENTFLAG_FALLING))
            {
                MovementLiving.JumpZSpeed = ReadSingle();
                MovementLiving.JumpSinAngle = ReadSingle();
                MovementLiving.JumpCosAngle = ReadSingle();
                MovementLiving.JumpXySpeed = ReadSingle();
            }

            if (MovementLiving.MovementFlags.HasFlag(MovementFlags.MOVEMENTFLAG_SPLINE_ELEVATION))
            {
                MovementLiving.SplineElevation = ReadSingle();
            }
        }
    }
}
