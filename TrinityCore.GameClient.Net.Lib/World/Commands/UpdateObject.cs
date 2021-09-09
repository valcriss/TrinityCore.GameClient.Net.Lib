using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Log;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World.Entities;
using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.World.Commands
{
    public class UpdateObject : WorldReceivablePacket
    {
        public List<UpdateMovement> Movements { get; set; }
        public List<UpdateCreateObject> UpdateCreateObjects { get; set; }
        public List<UpdateOutOfRange> UpdateOutOfRanges { get; set; }
        public List<UpdateValues> UpdateValues { get; set; }

        public UpdateObject(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            try
            {
                UpdateValues = new List<UpdateValues>();
                UpdateCreateObjects = new List<UpdateCreateObject>();
                Movements = new List<UpdateMovement>();
                UpdateOutOfRanges = new List<UpdateOutOfRange>();
                List<ObjectUpdateType> objectUpdateTypes = new List<ObjectUpdateType>();
                uint count = ReadUInt32();
                for (int i = 0; i < count; i++)
                {
                    ObjectUpdateType type = (ObjectUpdateType)ReadSByte();
                    objectUpdateTypes.Add(type);
                    switch (type)
                    {
                        case ObjectUpdateType.UPDATETYPE_VALUES:
                            UpdateValues updateValues = new UpdateValues();
                            updateValues.Guid = ReadPackedGuid();
                            updateValues.Fields = GetUpdateValues();
                            UpdateValues.Add(updateValues);
                            break;

                        case ObjectUpdateType.UPDATETYPE_MOVEMENT:
                            UpdateMovement movement = new UpdateMovement();
                            movement.Guid = ReadPackedGuid();
                            MovementInfo movementInfo1 = new MovementInfo(Buffer, ReadIndex, TypeID.TYPEID_PLAYER);
                            ReadIndex = movementInfo1.ReadIndex;
                            movement.Movement = movementInfo1;
                            Movements.Add(movement);
                            break;

                        case ObjectUpdateType.UPDATETYPE_CREATE_OBJECT:
                        case ObjectUpdateType.UPDATETYPE_CREATE_OBJECT2:
                            UpdateCreateObject updateCreateObject = new UpdateCreateObject();
                            updateCreateObject.Guid = ReadPackedGuid();
                            updateCreateObject.ObjectType = (TypeID)ReadSByte();
                            MovementInfo movementInfo2 =
                                new MovementInfo(Buffer, ReadIndex, updateCreateObject.ObjectType);
                            ReadIndex = movementInfo2.ReadIndex;
                            updateCreateObject.Movement = movementInfo2;
                            updateCreateObject.Fields = GetUpdateValues();
                            UpdateCreateObjects.Add(updateCreateObject);
                            break;

                        case ObjectUpdateType.UPDATETYPE_OUT_OF_RANGE_OBJECTS:
                            UpdateOutOfRange updateOutOfRange = new UpdateOutOfRange();
                            var guidCount = ReadUInt32();
                            for (var guidIndex = 0; guidIndex < guidCount; guidIndex++)
                                updateOutOfRange.GuidList.Add(ReadPackedGuid());
                            UpdateOutOfRanges.Add(updateOutOfRange);
                            break;

                        case ObjectUpdateType.UPDATETYPE_NEAR_OBJECTS:

                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                if (ReadIndex != Buffer.Length)
                    Logger.Log(
                        "Difference : " + ReadIndex + " -> " + Buffer.Length + " (" + (Buffer.Length - ReadIndex) +
                        " bytes unused) for UpdateType : " + string.Join(" or ", objectUpdateTypes), LogLevel.ERROR);
            }
            catch (Exception e)
            {
                Logger.LogException(e);
            }
        }

        private Dictionary<UpdateFields, uint> GetUpdateValues()
        {
            Dictionary<UpdateFields, uint> values = new Dictionary<UpdateFields, uint>();
            byte blockCount = ReadByte();
            int[] updateMask = new int[blockCount];
            for (var i = 0; i < blockCount; i++)
                updateMask[i] = ReadInt32();
            var mask = new BitArray(updateMask);

            for (uint i = 0; i < mask.Count; ++i)
            {
                if (!mask[(int)i])
                    continue;

                values.Add((UpdateFields)i, ReadUInt32());
            }

            return values;
        }
    }
}
