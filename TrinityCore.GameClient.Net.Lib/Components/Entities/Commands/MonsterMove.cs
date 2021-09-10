using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;
using TrinityCore.GameClient.Net.Lib.World.Enums;
using TrinityCore.GameClient.Net.Lib.World.Navigation;

namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Commands
{
    public class MonsterMove : WorldReceivablePacket
    {
        public UInt64 MonsterGuid { get; set; }
        public Position Position { get; set; }

        public MonsterMove(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            MonsterGuid = ReadPackedGuid();
            sbyte zero = ReadSByte(); // = 0
            Vector3 point = ReadVector3();
            Position = new Position(point, 0);
            UInt32 splineId = ReadUInt32();
            MonsterMoveType monsterMove = (MonsterMoveType)ReadSByte();
            switch (monsterMove)
            {
                case MonsterMoveType.MonsterMoveFacingSpot:
                    Vector3 spot = ReadVector3();
                    break;

                case MonsterMoveType.MonsterMoveFacingTarget:
                    UInt64 target = ReadUInt64();
                    break;

                case MonsterMoveType.MonsterMoveFacingAngle:
                    float angle = ReadSingle();
                    Position.O = angle;
                    break;

                case MonsterMoveType.MonsterMoveStop:
                    return;
            }

            UInt32 splineFlags = ReadUInt32();

            if ((splineFlags & (uint)SplineFlags.Animation) != 0)
            {
                sbyte animationId = ReadSByte();
                Int32 effectStartTime = ReadInt32();
            }

            Int32 duration = ReadInt32();

            if ((splineFlags & (uint)SplineFlags.Parabolic) != 0)
            {
                float verticalAcceleration = ReadSingle();
                Int32 effectStartTime = ReadInt32();
            }

            if ((splineFlags & (uint)SplineFlags.Mask_CatmullRom) != 0)
            {
                if ((splineFlags & (uint)SplineFlags.Cyclic) != 0)
                {
                    UInt32 count = ReadUInt32();
                    for (int i = 0; i < count; i++)
                    {
                        Vector3 pathPoint = ReadVector3();
                    }
                }
                else
                {
                    UInt32 count = ReadUInt32();
                    for (int i = 0; i < count; i++)
                    {
                        Vector3 pathPoint = ReadVector3();
                    }
                }
            }
            else
            {
                UInt32 lastIndex = ReadUInt32();
                Vector3 destination = ReadVector3();
                if (lastIndex > 1)
                {
                    for (int i = 1; i < lastIndex; ++i)
                    {
                        Vector3 pathPoint = ReadVector3();
                    }
                }
            }
        }
    }
}
