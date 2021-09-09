using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World.Enums;
using TrinityCore.GameClient.Net.Lib.World.Navigation;

namespace TrinityCore.GameClient.Net.Lib.World.Entities
{
    public class MovementInfo : Packet
    {
        public MovementLiving MovementLiving { get; set; }
        public MovementPosition MovementPosition { get; set; }
        public MovementStationary MovementStationary { get; set; }
        public MovementHasTarget MovementHasTarget { get; set; }
        public MovementRotation MovementRotation { get; set; }

        public MovementInfo()
        {
        }

        public MovementInfo(byte[] buffer, int readIndex, TypeID typeId) : base(buffer, readIndex)
        {
            MovementLiving = null;

            ushort flags = ReadUInt16();

            if ((flags & (ushort)ObjectUpdateFlags.UPDATEFLAG_LIVING) != 0)
            {
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

                if (MovementLiving.MovementFlags.HasFlag(MovementFlags.MOVEMENTFLAG_SWIMMING) ||
                    MovementLiving.MovementFlags.HasFlag(MovementFlags.MOVEMENTFLAG_FLYING) ||
                    MovementLiving.ExtraMovementFlags.HasFlag(MovementFlags2.MOVEMENTFLAG2_ALWAYS_ALLOW_PITCHING))
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

                for (int i = 0; i < 9; i++) MovementLiving.Speeds.Add((UnitMoveType)i, ReadSingle());

                // if (unit->m_movementInfo.GetMovementFlags() & MOVEMENTFLAG_SPLINE_ENABLED)
                //    Movement::PacketBuilder::WriteCreate(*unit->movespline, *data);
                if (MovementLiving.MovementFlags.HasFlag(MovementFlags.MOVEMENTFLAG_SPLINE_ENABLED))
                {
                    MovementLiving.MovementSpline = new MovementSpline();
                    MovementLiving.MovementSpline.SplineFlags = (SplineFlags)ReadUInt32();
                    if (MovementLiving.MovementSpline.SplineFlags == SplineFlags.Final_Angle)
                    {
                        MovementLiving.MovementSpline.FacingAngle = ReadSingle();
                    }
                    else if (MovementLiving.MovementSpline.SplineFlags == SplineFlags.Final_Target)
                    {
                        MovementLiving.MovementSpline.FacingTarget = ReadSingle();
                    }
                    else if (MovementLiving.MovementSpline.SplineFlags == SplineFlags.Final_Point)
                    {
                        MovementLiving.MovementSpline.FinalPosition = new Position(ReadVector3(), 0);
                    }

                    MovementLiving.MovementSpline.TimePassed = ReadInt32();
                    MovementLiving.MovementSpline.Duration = ReadInt32();
                    MovementLiving.MovementSpline.SplineId = ReadUInt32();

                    float f1 = ReadSingle(); // = 1.0
                    float f2 = ReadSingle(); // = 1.0

                    MovementLiving.MovementSpline.VerticalAcceleration = ReadSingle();
                    MovementLiving.MovementSpline.EffectStartTime = ReadInt32();

                    uint nodesCount = ReadUInt32();
                    MovementLiving.MovementSpline.SplineNodes = new Position[nodesCount];
                    for (int i = 0; i < nodesCount; i++) MovementLiving.MovementSpline.SplineNodes[i] = new Position(ReadVector3(), 0);

                    MovementLiving.MovementSpline.SplineEvaluationMode = (SplineEvaluationMode)ReadSByte();
                    MovementLiving.MovementSpline.FinalDestination = new Position(ReadVector3(), 0);
                }
            }
            else
            {
                if ((flags & (ushort)ObjectUpdateFlags.UPDATEFLAG_POSITION) != 0)
                {
                    MovementPosition = new MovementPosition();
                    MovementPosition.Transport = PeekByte() != 0;

                    if (MovementPosition.Transport)
                        MovementPosition.TransportGuid = ReadPackedGuid();
                    else
                        ReadSByte();

                    MovementPosition.Position = new Position(ReadVector3(), 0);

                    if (MovementPosition.Transport)
                        MovementPosition.TransportPosition = new Position(ReadVector3(), 0);
                    else
                        ReadVector3();

                    float o = ReadSingle();

                    MovementPosition.Position.O = o;
                    if (MovementPosition.Transport) MovementPosition.TransportPosition.O = o;

                    ReadSingle();
                }
                else
                {
                    if ((flags & (ushort)ObjectUpdateFlags.UPDATEFLAG_STATIONARY_POSITION) != 0)
                    {
                        MovementStationary = new MovementStationary();
                        MovementStationary.Stationary = new Position(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
                    }
                }
            }

            if ((flags & (ushort)ObjectUpdateFlags.UPDATEFLAG_UNKNOWN) != 0)
            {
                ReadUInt32();
            }

            if ((flags & (ushort)ObjectUpdateFlags.UPDATEFLAG_LOWGUID) != 0)
            {
                switch (typeId)
                {
                    case TypeID.TYPEID_OBJECT:
                    case TypeID.TYPEID_ITEM:
                    case TypeID.TYPEID_CONTAINER:
                    case TypeID.TYPEID_GAMEOBJECT:
                    case TypeID.TYPEID_DYNAMICOBJECT:
                    case TypeID.TYPEID_CORPSE:
                        ReadUInt32(); // GetGUID().GetCounter()
                        break;
                    //! Unit, Player and default here are sending wrong values.
                    /// @todo Research the proper formula
                    case TypeID.TYPEID_UNIT:
                        ReadUInt32(); // unk
                        break;

                    case TypeID.TYPEID_PLAYER:
                        if ((flags & (ushort)ObjectUpdateFlags.UPDATEFLAG_SELF) != 0)
                            ReadUInt32(); // unk
                        else
                            ReadUInt32(); // unk
                        break;

                    default:
                        ReadUInt32(); // unk
                        break;
                }
            }

            if ((flags & (ushort)ObjectUpdateFlags.UPDATEFLAG_HAS_TARGET) != 0)
            {
                MovementHasTarget = new MovementHasTarget();
                if (PeekByte() != 0)
                    MovementHasTarget.Target = ReadPackedGuid();
                else
                    ReadSByte();
            }

            if ((flags & (ushort)ObjectUpdateFlags.UPDATEFLAG_TRANSPORT) != 0)
            {
                ReadUInt32();
            }

            if ((flags & (ushort)ObjectUpdateFlags.UPDATEFLAG_VEHICLE) != 0)
            {
                ReadUInt32();
                ReadSingle();
            }

            if ((flags & (ushort)ObjectUpdateFlags.UPDATEFLAG_ROTATION) != 0)
            {
                MovementRotation = new MovementRotation();
                MovementRotation.Rotation = ReadInt64();
            }
        }
    }
}
