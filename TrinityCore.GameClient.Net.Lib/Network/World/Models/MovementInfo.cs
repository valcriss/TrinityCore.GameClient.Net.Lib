using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Models
{
    internal class MovementInfo : Packet
    {
        #region Internal Properties

        internal MovementHasTarget MovementHasTarget { get; set; }
        internal MovementLiving MovementLiving { get; set; }
        internal MovementPosition MovementPosition { get; set; }
        internal MovementRotation MovementRotation { get; set; }
        internal MovementStationary MovementStationary { get; set; }

        #endregion Internal Properties

        #region Internal Constructors

        internal MovementInfo()
        {
        }

#pragma warning disable IDE0060 // Supprimer le paramètre inutilisé

        internal MovementInfo(byte[] buffer, int readIndex, TypeID typeId) : base(buffer, readIndex)
#pragma warning restore IDE0060 // Supprimer le paramètre inutilisé
        {
            MovementLiving = null;

            ushort flags = ReadUInt16();

            if ((flags & (ushort)ObjectUpdateOptions.UPDATEFLAG_LIVING) != 0)
            {
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

                if (MovementLiving.MovementFlags.HasFlag(MovementTypes.SWIMMING) ||
                    MovementLiving.MovementFlags.HasFlag(MovementTypes.FLYING) ||
                    MovementLiving.ExtraMovementFlags.HasFlag(MovementOptions.ALWAYS_ALLOW_PITCHING))
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

                for (int i = 0; i < 9; i++) MovementLiving.Speeds.Add((UnitMoveType)i, ReadSingle());

                if (MovementLiving.MovementFlags.HasFlag(MovementTypes.SPLINE_ENABLED))
                {
                    MovementLiving.MovementSpline = new MovementSpline();
                    MovementLiving.MovementSpline.SplineFlags = (SplineTypes)ReadUInt32();
                    if (MovementLiving.MovementSpline.SplineFlags == SplineTypes.Final_Angle)
                    {
                        MovementLiving.MovementSpline.FacingAngle = ReadSingle();
                    }
                    else if (MovementLiving.MovementSpline.SplineFlags == SplineTypes.Final_Target)
                    {
                        MovementLiving.MovementSpline.FacingTarget = ReadSingle();
                    }
                    else if (MovementLiving.MovementSpline.SplineFlags == SplineTypes.Final_Point)
                    {
                        MovementLiving.MovementSpline.FinalPosition = new Position(ReadVector3(), 0);
                    }

                    MovementLiving.MovementSpline.TimePassed = ReadInt32();
                    MovementLiving.MovementSpline.Duration = ReadInt32();
                    MovementLiving.MovementSpline.SplineId = ReadUInt32();

                    ReadSingle(); // = 1.0
                    ReadSingle(); // = 1.0

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
                if ((flags & (ushort)ObjectUpdateOptions.UPDATEFLAG_POSITION) != 0)
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
                    if ((flags & (ushort)ObjectUpdateOptions.UPDATEFLAG_STATIONARY_POSITION) != 0)
                    {
                        MovementStationary = new MovementStationary();
                        MovementStationary.Stationary = new Position(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
                    }
                }
            }

            if ((flags & (ushort)ObjectUpdateOptions.UPDATEFLAG_UNKNOWN) != 0)
            {
                ReadUInt32();
            }

            if ((flags & (ushort)ObjectUpdateOptions.UPDATEFLAG_LOWGUID) != 0)
            {
                ReadUInt32();
            }

            if ((flags & (ushort)ObjectUpdateOptions.UPDATEFLAG_HAS_TARGET) != 0)
            {
                MovementHasTarget = new MovementHasTarget();
                if (PeekByte() != 0)
                    MovementHasTarget.Target = ReadPackedGuid();
                else
                    ReadSByte();
            }

            if ((flags & (ushort)ObjectUpdateOptions.UPDATEFLAG_TRANSPORT) != 0)
            {
                ReadUInt32();
            }

            if ((flags & (ushort)ObjectUpdateOptions.UPDATEFLAG_VEHICLE) != 0)
            {
                ReadUInt32();
                ReadSingle();
            }

            if ((flags & (ushort)ObjectUpdateOptions.UPDATEFLAG_ROTATION) != 0)
            {
                MovementRotation = new MovementRotation();
                MovementRotation.Rotation = ReadInt64();
            }
        }

        #endregion Internal Constructors
    }
}