using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;
using TrinityCore.GameClient.Net.Lib.World.Entities;
using TrinityCore.GameClient.Net.Lib.World.Enums;
using TrinityCore.GameClient.Net.Lib.World.Navigation;

namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Commands
{
    public class HandleMovement : WorldReceivablePacket
    {
        public UInt64 Guid { get; set; }
        public MovementLiving MovementLiving { get; set; }
        public HandleMovement(ReceivablePacket receivablePacket) : base(receivablePacket)
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
