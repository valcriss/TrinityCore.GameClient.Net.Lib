using System.Collections;
using System.Buffers.Binary;

namespace TrinityCore.GameClient.Net.Protocol.World;

internal static class UpdateObjectParser
{
    public static UpdateObjectBatch Parse(byte[] payload)
    {
        var reader = new PacketReader(payload);
        var values = new List<UpdateEntityValues>();
        var outOfRange = new List<ulong>();

        var updateCount = reader.ReadUInt32();
        for (var i = 0; i < updateCount; i++)
        {
            var updateType = (ObjectUpdateType)reader.ReadSByte();
            switch (updateType)
            {
                case ObjectUpdateType.Values:
                {
                    var guid = reader.ReadPackedGuid();
                    values.Add(new UpdateEntityValues(guid, reader.ReadUpdateFields()));
                    break;
                }
                case ObjectUpdateType.Movement:
                {
                    var guid = reader.ReadPackedGuid();
                    var movement = reader.ReadMovementInfo();
                    values.Add(new UpdateEntityValues(guid, new Dictionary<int, uint>(), null, movement));
                    break;
                }
                case ObjectUpdateType.CreateObject:
                case ObjectUpdateType.CreateObject2:
                {
                    var guid = reader.ReadPackedGuid();
                    var typeId = unchecked((byte)reader.ReadSByte());
                    var movement = reader.ReadMovementInfo();
                    values.Add(new UpdateEntityValues(guid, reader.ReadUpdateFields(), typeId, movement));
                    break;
                }
                case ObjectUpdateType.OutOfRangeObjects:
                {
                    var guidCount = reader.ReadUInt32();
                    for (var g = 0; g < guidCount; g++)
                    {
                        outOfRange.Add(reader.ReadPackedGuid());
                    }
                    break;
                }
                case ObjectUpdateType.NearObjects:
                    break;
            }
        }

        return new UpdateObjectBatch(values, outOfRange);
    }

    private enum ObjectUpdateType : sbyte
    {
        Values = 0,
        Movement = 1,
        CreateObject = 2,
        CreateObject2 = 3,
        OutOfRangeObjects = 4,
        NearObjects = 5
    }

    private sealed class PacketReader(byte[] data)
    {
        private const uint MovementOnTransport = 0x00000200;
        private const uint MovementFalling = 0x00001000;
        private const uint MovementSwimming = 0x00200000;
        private const uint MovementFlying = 0x02000000;
        private const uint MovementSplineElevation = 0x04000000;
        private const uint MovementSplineEnabled = 0x08000000;

        private const ushort MovementOptionAlwaysAllowPitching = 0x00000020;
        private const ushort MovementOptionInterpolatedMovement = 0x00000400;

        private const ushort UpdateFlagTransport = 0x0002;
        private const ushort UpdateFlagHasTarget = 0x0004;
        private const ushort UpdateFlagUnknown = 0x0008;
        private const ushort UpdateFlagLowGuid = 0x0010;
        private const ushort UpdateFlagLiving = 0x0020;
        private const ushort UpdateFlagStationaryPosition = 0x0040;
        private const ushort UpdateFlagVehicle = 0x0080;
        private const ushort UpdateFlagPosition = 0x0100;
        private const ushort UpdateFlagRotation = 0x0200;

        private readonly byte[] _data = data;
        private int _index;

        public uint ReadUInt32()
        {
            var value = BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan(_index, 4));
            _index += 4;
            return value;
        }

        public int ReadInt32()
        {
            var value = BinaryPrimitives.ReadInt32LittleEndian(_data.AsSpan(_index, 4));
            _index += 4;
            return value;
        }

        public ushort ReadUInt16()
        {
            var value = BinaryPrimitives.ReadUInt16LittleEndian(_data.AsSpan(_index, 2));
            _index += 2;
            return value;
        }

        public sbyte ReadSByte()
        {
            var value = unchecked((sbyte)_data[_index]);
            _index++;
            return value;
        }

        public byte ReadByte()
        {
            var value = _data[_index];
            _index++;
            return value;
        }

        public ulong ReadPackedGuid()
        {
            var mask = ReadByte();
            if (mask == 0)
            {
                return 0;
            }

            ulong guid = 0;
            for (var i = 0; i < 8; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    guid |= (ulong)ReadByte() << (i * 8);
                }
            }

            return guid;
        }

        public IReadOnlyDictionary<int, uint> ReadUpdateFields()
        {
            var blockCount = ReadByte();
            var maskBlocks = new int[blockCount];
            for (var i = 0; i < blockCount; i++)
            {
                maskBlocks[i] = ReadInt32();
            }

            var bitMask = new BitArray(maskBlocks);
            var values = new Dictionary<int, uint>();
            for (var field = 0; field < bitMask.Count; field++)
            {
                if (!bitMask[field])
                {
                    continue;
                }

                values[field] = ReadUInt32();
            }

            return values;
        }

        public MovementSnapshot? ReadMovementInfo()
        {
            var flags = ReadUInt16();
            MovementSnapshot? movement = null;

            if ((flags & UpdateFlagLiving) != 0)
            {
                movement = ReadMovementLiving();
            }
            else if ((flags & UpdateFlagPosition) != 0)
            {
                movement = ReadMovementPosition();
            }
            else if ((flags & UpdateFlagStationaryPosition) != 0)
            {
                var x = ReadSingle();
                var y = ReadSingle();
                var z = ReadSingle();
                var o = ReadSingle();
                movement = new MovementSnapshot(x, y, z, o);
            }

            if ((flags & UpdateFlagUnknown) != 0)
            {
                SkipBytes(4);
            }

            if ((flags & UpdateFlagLowGuid) != 0)
            {
                SkipBytes(4);
            }

            if ((flags & UpdateFlagHasTarget) != 0)
            {
                if (PeekByte() != 0)
                {
                    _ = ReadPackedGuid();
                }
                else
                {
                    SkipBytes(1);
                }
            }

            if ((flags & UpdateFlagTransport) != 0)
            {
                SkipBytes(4);
            }

            if ((flags & UpdateFlagVehicle) != 0)
            {
                SkipBytes(8);
            }

            if ((flags & UpdateFlagRotation) != 0)
            {
                SkipBytes(8);
            }

            return movement;
        }

        private MovementSnapshot? ReadMovementPosition()
        {
            var hasTransport = PeekByte() != 0;
            if (hasTransport)
            {
                _ = ReadPackedGuid();
            }
            else
            {
                SkipBytes(1);
            }

            var x = ReadSingle();
            var y = ReadSingle();
            var z = ReadSingle();
            if (hasTransport)
            {
                SkipBytes(12);
            }
            else
            {
                SkipBytes(12);
            }

            var o = ReadSingle();
            SkipBytes(4);
            return new MovementSnapshot(x, y, z, o);
        }

        private MovementSnapshot? ReadMovementLiving()
        {
            var movementFlags = ReadUInt32();
            var movementOptions = ReadUInt16();

            SkipBytes(4); // time
            var x = ReadSingle();
            var y = ReadSingle();
            var z = ReadSingle();
            var o = ReadSingle();

            if ((movementFlags & MovementOnTransport) != 0)
            {
                _ = ReadPackedGuid();
                SkipBytes(12); // transport xyz
                SkipBytes(4); // transport o
                SkipBytes(4); // transport time
                SkipBytes(1); // transport seat
                if ((movementOptions & MovementOptionInterpolatedMovement) != 0)
                {
                    SkipBytes(4);
                }
            }

            if ((movementFlags & MovementSwimming) != 0 ||
                (movementFlags & MovementFlying) != 0 ||
                (movementOptions & MovementOptionAlwaysAllowPitching) != 0)
            {
                SkipBytes(4);
            }

            SkipBytes(4); // fall time
            if ((movementFlags & MovementFalling) != 0)
            {
                SkipBytes(16);
            }

            if ((movementFlags & MovementSplineElevation) != 0)
            {
                SkipBytes(4);
            }

            SkipBytes(9 * 4); // speeds

            if ((movementFlags & MovementSplineEnabled) != 0)
            {
                var splineFlags = ReadUInt32();
                var facingMask = splineFlags & 0x00038000;
                if (facingMask == 0x00020000) // Final_Angle
                {
                    SkipBytes(4);
                }
                else if (facingMask == 0x00010000) // Final_Target
                {
                    SkipBytes(8); // ObjectGuid serialized as uint64 here
                }
                else if (facingMask == 0x00008000) // Final_Point
                {
                    SkipBytes(12);
                }

                SkipBytes(4 + 4 + 4); // timePassed, duration, splineId
                SkipBytes(4 + 4); // scale values
                SkipBytes(4 + 4); // vertical accel + effect start

                var nodesCount = ReadUInt32();
                SkipBytes((int)nodesCount * 12);

                SkipBytes(1); // evaluation mode
                SkipBytes(12); // final destination
            }

            return new MovementSnapshot(x, y, z, o);
        }

        private byte PeekByte() => _data[_index];

        private float ReadSingle()
        {
            var value = BinaryPrimitives.ReadSingleLittleEndian(_data.AsSpan(_index, 4));
            _index += 4;
            return value;
        }

        private void SkipBytes(int count)
        {
            _index += count;
        }
    }
}
