using System;
using System.Numerics;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;
using TrinityCore.GameClient.Net.Lib.Network.World.Models;

namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Commands.Incoming
{
    internal class MonsterMove : ReceivablePacket<Network.World.Enums.WorldCommand>
    {
        #region Internal Properties

        internal UInt64 MonsterGuid { get; set; }
        internal Position Position { get; set; }

        #endregion Internal Properties

        #region Internal Methods

        internal override void LoadData()
        {
            MonsterGuid = ReadPackedGuid();
            ReadSByte(); // = 0
            Vector3 point = ReadVector3();
            Position = new Position(point, 0);
            ReadUInt32();
            MonsterMoveType monsterMove = (MonsterMoveType)ReadSByte();
            switch (monsterMove)
            {
                case MonsterMoveType.MonsterMoveFacingSpot:
                    ReadVector3();
                    break;

                case MonsterMoveType.MonsterMoveFacingTarget:
                    ReadUInt64();
                    break;

                case MonsterMoveType.MonsterMoveFacingAngle:
                    float angle = ReadSingle();
                    Position.O = angle;
                    break;

                case MonsterMoveType.MonsterMoveStop:
                    return;
            }

            UInt32 splineFlags = ReadUInt32();

            if ((splineFlags & (uint)SplineTypes.Animation) != 0)
            {
                ReadSByte();
                ReadInt32();
            }

            ReadInt32();

            if ((splineFlags & (uint)SplineTypes.Parabolic) != 0)
            {
                ReadSingle();
                ReadInt32();
            }

            if ((splineFlags & (uint)SplineTypes.Mask_CatmullRom) != 0)
            {
                UInt32 count = ReadUInt32();
                for (int i = 0; i < count; i++)
                {
                    ReadVector3();
                }
            }
            else
            {
                UInt32 lastIndex = ReadUInt32();
                ReadVector3();
                if (lastIndex > 1)
                {
                    for (int i = 1; i < lastIndex; ++i)
                    {
                        ReadVector3();
                    }
                }
            }
        }

        #endregion Internal Methods
    }
}