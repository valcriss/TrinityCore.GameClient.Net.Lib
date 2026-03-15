using System.Buffers.Binary;

namespace TrinityCore.GameClient.Net.Protocol.World;

public static class MovementPacketCodec
{
    public readonly record struct MovementPayloadData(
        ulong Guid,
        MovementFlags Flags,
        ushort ExtraFlags,
        float X,
        float Y,
        float Z,
        float Orientation,
        uint MovementTimeMs,
        uint FallTimeMs);

    public readonly record struct ForceSpeedChangePayloadData(
        uint MovementCounter,
        float NewSpeed);

    public readonly record struct MonsterMovePayloadData(
        ulong Guid,
        bool IsTransport,
        ulong TransportGuid,
        sbyte TransportSeat,
        byte MoveType,
        uint SplineFlags,
        uint SplineId,
        uint DurationMs,
        float StartX,
        float StartY,
        float StartZ,
        float EndX,
        float EndY,
        float EndZ);

    [Flags]
    public enum MovementFlags : uint
    {
        None = 0x00000000,
        Forward = 0x00000001,
        Backward = 0x00000002,
        StrafeLeft = 0x00000004,
        StrafeRight = 0x00000008,
        Left = 0x00000010,
        Right = 0x00000020,
        Walking = 0x00000100,
        Falling = 0x00001000,
        Swimming = 0x00200000,
        Flying = 0x02000000
    }

    public static byte[] BuildActiveMoverPayload(ulong guid)
    {
        return BitConverter.GetBytes(guid);
    }

    public static byte[] BuildMovementPayload(
        ulong guid,
        MovementFlags flags,
        uint movementTimeMs,
        float x,
        float y,
        float z,
        float orientation,
        uint fallTime = 0)
    {
        var payload = new List<byte>(48);
        AppendPackedGuid(payload, guid);
        payload.AddRange(BitConverter.GetBytes((uint)flags));
        payload.AddRange(BitConverter.GetBytes((ushort)0)); // flags2
        payload.AddRange(BitConverter.GetBytes(movementTimeMs));
        payload.AddRange(BitConverter.GetBytes(x));
        payload.AddRange(BitConverter.GetBytes(y));
        payload.AddRange(BitConverter.GetBytes(z));
        payload.AddRange(BitConverter.GetBytes(orientation));
        payload.AddRange(BitConverter.GetBytes(fallTime));
        return payload.ToArray();
    }

    public static (uint MovementCounter, float NewSpeed) ParseForceRunSpeedChangePayload(byte[] payload)
    {
        if (!TryParseForceSpeedChangePayload(payload, hasLegacyRunMarker: true, out var parsed))
        {
            throw new ArgumentOutOfRangeException(nameof(payload), "Invalid SMSG_FORCE_RUN_SPEED_CHANGE payload.");
        }

        return (parsed.MovementCounter, parsed.NewSpeed);
    }

    public static bool TryParseForceSpeedChangePayload(
        byte[] payload,
        bool hasLegacyRunMarker,
        out ForceSpeedChangePayloadData data)
    {
        data = default;
        try
        {
            // TrinityCore 3.3.5 SMSG_FORCE_*_SPEED_CHANGE payload:
            // packed guid + movementCounter + [legacy byte for RUN only] + new speed (float).
            var index = 0;
            SkipPackedGuid(payload, ref index);
            if (payload.Length < index + 4)
            {
                return false;
            }

            var movementCounter = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(index, 4));
            index += 4;
            if (hasLegacyRunMarker)
            {
                if (payload.Length < index + 1)
                {
                    return false;
                }

                index += 1;
            }

            if (payload.Length < index + 4)
            {
                return false;
            }

            var newSpeed = BinaryPrimitives.ReadSingleLittleEndian(payload.AsSpan(index, 4));
            data = new ForceSpeedChangePayloadData(movementCounter, newSpeed);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static byte[] BuildForceSpeedChangeAckPayload(
        ulong guid,
        uint movementCounter,
        uint movementTimeMs,
        float x,
        float y,
        float z,
        float orientation,
        float runSpeed)
    {
        var payload = new List<byte>(64);
        AppendPackedGuid(payload, guid);
        payload.AddRange(BitConverter.GetBytes(movementCounter));
        payload.AddRange(BitConverter.GetBytes((uint)MovementFlags.None));
        payload.AddRange(BitConverter.GetBytes((ushort)0)); // flags2
        payload.AddRange(BitConverter.GetBytes(movementTimeMs));
        payload.AddRange(BitConverter.GetBytes(x));
        payload.AddRange(BitConverter.GetBytes(y));
        payload.AddRange(BitConverter.GetBytes(z));
        payload.AddRange(BitConverter.GetBytes(orientation));
        payload.AddRange(BitConverter.GetBytes((uint)0)); // fallTime
        payload.AddRange(BitConverter.GetBytes(runSpeed));
        return payload.ToArray();
    }

    public static byte[] BuildForceRunSpeedChangeAckPayload(
        ulong guid,
        uint movementCounter,
        uint movementTimeMs,
        float x,
        float y,
        float z,
        float orientation,
        float runSpeed) =>
        BuildForceSpeedChangeAckPayload(guid, movementCounter, movementTimeMs, x, y, z, orientation, runSpeed);

    public static bool TryParseMovementPayload(byte[] payload, out MovementPayloadData data)
    {
        data = default;
        try
        {
            var index = 0;
            var guid = ReadPackedGuid(payload, ref index);
            data = ReadMovementInfoData(payload, ref index, guid);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryParseForceSpeedChangeAckPayload(
        byte[] payload,
        out uint movementCounter,
        out MovementPayloadData movementData,
        out float runSpeed)
    {
        movementCounter = default;
        movementData = default;
        runSpeed = default;
        try
        {
            var index = 0;
            var guid = ReadPackedGuid(payload, ref index);
            movementCounter = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(index, 4));
            index += 4;
            movementData = ReadMovementInfoData(payload, ref index, guid);
            runSpeed = BinaryPrimitives.ReadSingleLittleEndian(payload.AsSpan(index, 4));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryParseForceRunSpeedChangeAckPayload(
        byte[] payload,
        out uint movementCounter,
        out MovementPayloadData movementData,
        out float runSpeed) =>
        TryParseForceSpeedChangeAckPayload(payload, out movementCounter, out movementData, out runSpeed);

    public static bool TryParseMonsterMovePayload(WorldOpcode opcode, byte[] payload, out MonsterMovePayloadData data)
    {
        data = default;
        try
        {
            var index = 0;
            var guid = ReadPackedGuid(payload, ref index);

            var isTransport = opcode == WorldOpcode.SmsgMonsterMoveTransport;
            ulong transportGuid = 0;
            sbyte transportSeat = -1;
            if (isTransport)
            {
                transportGuid = ReadPackedGuid(payload, ref index);
                transportSeat = unchecked((sbyte)payload[index++]);
            }

            index += 1; // unk movement flag2 toggle byte
            var startX = BinaryPrimitives.ReadSingleLittleEndian(payload.AsSpan(index, 4));
            index += 4;
            var startY = BinaryPrimitives.ReadSingleLittleEndian(payload.AsSpan(index, 4));
            index += 4;
            var startZ = BinaryPrimitives.ReadSingleLittleEndian(payload.AsSpan(index, 4));
            index += 4;

            var splineId = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(index, 4));
            index += 4;

            var moveType = payload[index++];
            if (moveType == MonsterMoveTypeStop)
            {
                data = new MonsterMovePayloadData(
                    guid,
                    isTransport,
                    transportGuid,
                    transportSeat,
                    moveType,
                    0,
                    splineId,
                    0,
                    startX,
                    startY,
                    startZ,
                    startX,
                    startY,
                    startZ);
                return true;
            }

            switch (moveType)
            {
                case MonsterMoveTypeFacingTarget:
                    index += 8;
                    break;
                case MonsterMoveTypeFacingAngle:
                    index += 4;
                    break;
                case MonsterMoveTypeFacingSpot:
                    index += 12;
                    break;
            }

            var splineFlags = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(index, 4));
            index += 4;

            if ((splineFlags & SplineFlagAnimation) != 0)
            {
                index += 1; // anim tier
                index += 4; // effect start time
            }

            var durationMs = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(index, 4));
            index += 4;

            if ((splineFlags & SplineFlagParabolic) != 0)
            {
                index += 4; // vertical acceleration
                index += 4; // effect start time
            }

            var pointCount = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(index, 4));
            index += 4;

            float endX;
            float endY;
            float endZ;
            var isCatmull = (splineFlags & SplineFlagCatmullMask) != 0;
            if (isCatmull)
            {
                if (pointCount > 0)
                {
                    var lastPointOffset = checked((int)((pointCount - 1) * 12));
                    var last = index + lastPointOffset;
                    endX = BinaryPrimitives.ReadSingleLittleEndian(payload.AsSpan(last, 4));
                    endY = BinaryPrimitives.ReadSingleLittleEndian(payload.AsSpan(last + 4, 4));
                    endZ = BinaryPrimitives.ReadSingleLittleEndian(payload.AsSpan(last + 8, 4));
                }
                else
                {
                    endX = startX;
                    endY = startY;
                    endZ = startZ;
                }
            }
            else
            {
                if (pointCount > 0)
                {
                    endX = BinaryPrimitives.ReadSingleLittleEndian(payload.AsSpan(index, 4));
                    endY = BinaryPrimitives.ReadSingleLittleEndian(payload.AsSpan(index + 4, 4));
                    endZ = BinaryPrimitives.ReadSingleLittleEndian(payload.AsSpan(index + 8, 4));
                }
                else
                {
                    endX = startX;
                    endY = startY;
                    endZ = startZ;
                }
            }

            data = new MonsterMovePayloadData(
                guid,
                isTransport,
                transportGuid,
                transportSeat,
                moveType,
                splineFlags,
                splineId,
                durationMs,
                startX,
                startY,
                startZ,
                endX,
                endY,
                endZ);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private const byte MonsterMoveTypeStop = 1;
    private const byte MonsterMoveTypeFacingSpot = 2;
    private const byte MonsterMoveTypeFacingTarget = 3;
    private const byte MonsterMoveTypeFacingAngle = 4;
    private const uint SplineFlagParabolic = 0x00000800;
    private const uint SplineFlagCatmullMask = 0x00042000;
    private const uint SplineFlagAnimation = 0x00200000;

    private static void AppendPackedGuid(List<byte> payload, ulong guid)
    {
        byte mask = 0;
        Span<byte> packed = stackalloc byte[8];
        var size = 0;
        for (var i = 0; i < 8; i++)
        {
            var b = (byte)((guid >> (i * 8)) & 0xFF);
            if (b == 0)
            {
                continue;
            }

            mask |= (byte)(1 << i);
            packed[size++] = b;
        }

        payload.Add(mask);
        for (var i = 0; i < size; i++)
        {
            payload.Add(packed[i]);
        }
    }

    private static void SkipPackedGuid(byte[] payload, ref int index)
    {
        var mask = payload[index++];
        for (var i = 0; i < 8; i++)
        {
            if ((mask & (1 << i)) != 0)
            {
                index++;
            }
        }
    }

    private static ulong ReadPackedGuid(byte[] payload, ref int index)
    {
        var mask = payload[index++];
        ulong guid = 0;
        for (var i = 0; i < 8; i++)
        {
            if ((mask & (1 << i)) != 0)
            {
                guid |= (ulong)payload[index++] << (i * 8);
            }
        }

        return guid;
    }

    private static void SkipMovementInfo(byte[] payload, ref int index)
    {
        var flags = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(index, 4));
        index += 4;
        var flags2 = BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan(index, 2));
        index += 2;

        index += 4; // time
        index += 4 * 4; // x,y,z,o
        _ = ReadMovementInfoTailAndReturnFallTime(payload, ref index, (MovementFlags)flags, flags2);
    }

    private static MovementPayloadData ReadMovementInfoData(byte[] payload, ref int index, ulong guid)
    {
        var flags = (MovementFlags)BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(index, 4));
        index += 4;
        var extraFlags = BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan(index, 2));
        index += 2;

        var movementTime = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(index, 4));
        index += 4;
        var x = BinaryPrimitives.ReadSingleLittleEndian(payload.AsSpan(index, 4));
        index += 4;
        var y = BinaryPrimitives.ReadSingleLittleEndian(payload.AsSpan(index, 4));
        index += 4;
        var z = BinaryPrimitives.ReadSingleLittleEndian(payload.AsSpan(index, 4));
        index += 4;
        var orientation = BinaryPrimitives.ReadSingleLittleEndian(payload.AsSpan(index, 4));
        index += 4;
        var fallTimeMs = ReadMovementInfoTailAndReturnFallTime(payload, ref index, flags, extraFlags);

        return new MovementPayloadData(
            guid,
            flags,
            extraFlags,
            x,
            y,
            z,
            orientation,
            movementTime,
            fallTimeMs);
    }

    private static uint ReadMovementInfoTailAndReturnFallTime(byte[] payload, ref int index, MovementFlags flags, ushort flags2)
    {
        var flagsValue = (uint)flags;

        const uint movementOnTransport = 0x00000200;
        const uint movementSwimming = 0x00200000;
        const uint movementFlying = 0x02000000;
        const uint movementFalling = 0x00001000;
        const uint movementSplineElevation = 0x04000000;
        const ushort movementAlwaysAllowPitching = 0x00000020;
        const ushort movementInterpolated = 0x00000400;

        if ((flagsValue & movementOnTransport) != 0)
        {
            SkipPackedGuid(payload, ref index);
            index += 4 * 4; // trans x y z o
            index += 4; // trans time
            index += 1; // seat
            if ((flags2 & movementInterpolated) != 0)
            {
                index += 4;
            }
        }

        if ((flagsValue & (movementSwimming | movementFlying)) != 0 || (flags2 & movementAlwaysAllowPitching) != 0)
        {
            index += 4; // pitch
        }

        var fallTimeMs = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(index, 4));
        index += 4; // fall time
        if ((flagsValue & movementFalling) != 0)
        {
            index += 16;
        }

        if ((flagsValue & movementSplineElevation) != 0)
        {
            index += 4;
        }

        return fallTimeMs;
    }
}
