using System.Buffers.Binary;
using System.Text;

namespace TrinityCore.GameClient.Net.Protocol.World;

public static class SocialPacketCodec
{
    public static byte[] BuildGroupAcceptPayload(uint roles = 0)
    {
        var payload = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(payload, roles);
        return payload;
    }

    public static byte[] BuildGroupDeclinePayload() => [];

    public static byte[] BuildChatMessagePayload(
        ChatChannel channel,
        ChatLanguage language,
        string message,
        string? whisperTarget = null,
        string? channelName = null)
    {
        var payload = new List<byte>(64 + message.Length);
        payload.AddRange(BitConverter.GetBytes((uint)channel));
        payload.AddRange(BitConverter.GetBytes((uint)language));

        switch (channel)
        {
            case ChatChannel.Whisper:
                payload.AddRange(Encoding.UTF8.GetBytes(whisperTarget ?? string.Empty));
                payload.Add(0);
                break;
            case ChatChannel.Channel:
                payload.AddRange(Encoding.UTF8.GetBytes(channelName ?? string.Empty));
                payload.Add(0);
                break;
        }

        payload.AddRange(Encoding.UTF8.GetBytes(message));
        if (language != ChatLanguage.Addon)
        {
            payload.Add(0);
        }

        return payload.ToArray();
    }

    public static byte[] BuildEmotePayload(uint emoteId)
    {
        var payload = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(payload, emoteId);
        return payload;
    }

    public static byte[] BuildTextEmotePayload(uint textEmoteId, uint emoteNum, ulong targetGuid)
    {
        var payload = new byte[16];
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(0, 4), textEmoteId);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(4, 4), emoteNum);
        BinaryPrimitives.WriteUInt64LittleEndian(payload.AsSpan(8, 8), targetGuid);
        return payload;
    }

    public static GroupInviteInfo ParseGroupInvitePayload(byte[] payload)
    {
        var reader = new PacketReader(payload);
        var canAccept = reader.ReadByte() != 0;
        var inviterName = reader.ReadCString();
        var proposedRoles = reader.ReadUInt32();
        var slotCount = reader.ReadByte();
        var lfgSlots = new List<uint>(slotCount);
        for (var i = 0; i < slotCount; i++)
        {
            lfgSlots.Add(reader.ReadByte());
        }

        var completedMask = reader.ReadUInt32();
        return new GroupInviteInfo(canAccept, inviterName, proposedRoles, lfgSlots, completedMask);
    }

    public static GroupMembershipInfo? ParseGroupListPayload(byte[] payload)
    {
        var reader = new PacketReader(payload);
        var groupType = reader.ReadByte();
        var memberSubGroup = reader.ReadByte();
        var memberFlags = reader.ReadByte();
        var memberRoles = reader.ReadByte();

        if ((groupType & 0x08) != 0)
        {
            _ = reader.ReadByte();
            _ = reader.ReadUInt32();
        }

        var groupGuid = reader.ReadUInt64();
        var sequence = reader.ReadUInt32();
        var otherMemberCount = reader.ReadUInt32();
        var members = new List<GroupMemberInfo>((int)Math.Min(otherMemberCount, 40));
        for (var i = 0; i < otherMemberCount; i++)
        {
            var memberName = reader.ReadCString();
            var guid = reader.ReadUInt64();
            var onlineState = reader.ReadByte();
            var subGroup = reader.ReadByte();
            var flags = reader.ReadByte();
            var roles = reader.ReadByte();
            members.Add(new GroupMemberInfo(guid, memberName, onlineState, subGroup, flags, roles));
        }

        var leaderGuid = reader.ReadUInt64();
        return leaderGuid == 0
            ? null
            : new GroupMembershipInfo(
                groupType,
                memberSubGroup,
                memberFlags,
                memberRoles,
                groupGuid,
                sequence,
                otherMemberCount,
                members,
                leaderGuid);
    }

    public static PartyCommandResultInfo ParsePartyCommandResultPayload(byte[] payload)
    {
        var reader = new PacketReader(payload);
        return new PartyCommandResultInfo(
            reader.ReadUInt32(),
            reader.ReadCString(),
            reader.ReadUInt32(),
            reader.ReadUInt32());
    }

    public static string ParseGroupSetLeaderPayload(byte[] payload)
    {
        var reader = new PacketReader(payload);
        return reader.ReadCString();
    }

    public static ReceivedChatMessage? ParseIncomingChatPayload(byte[] payload, bool gmMessage = false)
    {
        var reader = new PacketReader(payload);
        if (!reader.CanRead(1 + 4 + 8 + 4 + 4))
        {
            return null;
        }

        var rawMessageType = reader.ReadByte();
        var messageType = Enum.IsDefined(typeof(ChatMessageType), (uint)rawMessageType)
            ? (ChatMessageType)rawMessageType
            : (ChatMessageType)rawMessageType;
        var language = (ChatLanguage)reader.ReadInt32();
        var senderGuid = reader.ReadUInt64();
        _ = reader.ReadUInt32(); // chat flags

        string? senderName = null;
        string? receiverName = null;
        string? channelName = null;
        ulong receiverGuid;
        var gmSenderPrefixed = gmMessage && RequiresGmSenderNamePrefix(messageType);
        if (gmSenderPrefixed)
        {
            senderName = reader.ReadSizedCString();
        }

        switch (messageType)
        {
            case ChatMessageType.MonsterSay:
            case ChatMessageType.MonsterParty:
            case ChatMessageType.MonsterYell:
            case ChatMessageType.MonsterWhisper:
            case ChatMessageType.MonsterEmote:
                senderName = reader.ReadSizedCString();
                receiverGuid = reader.ReadUInt64();
                break;

            case ChatMessageType.WhisperForeign:
                senderName = reader.ReadSizedCString();
                receiverGuid = reader.ReadUInt64();
                break;

            case ChatMessageType.Channel:
                channelName = reader.ReadCString();
                receiverGuid = reader.ReadUInt64();
                break;

            case ChatMessageType.GuildAchievement:
                receiverGuid = reader.ReadUInt64();
                break;

            default:
                receiverGuid = reader.ReadUInt64();
                break;
        }

        var message = reader.ReadSizedCString();
        var chatTag = reader.CanRead(1) ? reader.ReadByte() : (byte)0;
        return new ReceivedChatMessage(
            messageType,
            gmMessage,
            language,
            senderGuid,
            receiverGuid,
            message,
            senderName,
            receiverName,
            channelName,
            chatTag);
    }

    private static bool RequiresGmSenderNamePrefix(ChatMessageType messageType)
    {
        return messageType switch
        {
            ChatMessageType.MonsterSay => false,
            ChatMessageType.MonsterParty => false,
            ChatMessageType.MonsterYell => false,
            ChatMessageType.MonsterWhisper => false,
            ChatMessageType.MonsterEmote => false,
            ChatMessageType.WhisperForeign => false,
            ChatMessageType.GuildAchievement => false,
            _ => true
        };
    }

    private ref struct PacketReader
    {
        private readonly ReadOnlySpan<byte> _buffer;
        private int _index;

        public PacketReader(ReadOnlySpan<byte> buffer)
        {
            _buffer = buffer;
            _index = 0;
        }

        public byte ReadByte()
        {
            var value = _buffer[_index];
            _index++;
            return value;
        }

        public bool CanRead(int byteCount)
        {
            return byteCount >= 0 && _index + byteCount <= _buffer.Length;
        }

        public int ReadInt32()
        {
            var value = BinaryPrimitives.ReadInt32LittleEndian(_buffer.Slice(_index, 4));
            _index += 4;
            return value;
        }

        public uint ReadUInt32()
        {
            var value = BinaryPrimitives.ReadUInt32LittleEndian(_buffer.Slice(_index, 4));
            _index += 4;
            return value;
        }

        public ulong ReadUInt64()
        {
            var value = BinaryPrimitives.ReadUInt64LittleEndian(_buffer.Slice(_index, 8));
            _index += 8;
            return value;
        }

        public string ReadSizedCString()
        {
            if (!CanRead(4))
            {
                return string.Empty;
            }

            var length = (int)ReadUInt32();
            if (length <= 0 || !CanRead(length))
            {
                return string.Empty;
            }

            var valueLength = Math.Max(0, length - 1);
            var value = valueLength == 0
                ? string.Empty
                : Encoding.UTF8.GetString(_buffer.Slice(_index, valueLength));
            _index += length;
            return value;
        }

        public string ReadCString()
        {
            var start = _index;
            while (_index < _buffer.Length && _buffer[_index] != 0)
            {
                _index++;
            }

            var value = Encoding.UTF8.GetString(_buffer.Slice(start, _index - start));
            if (_index < _buffer.Length)
            {
                _index++;
            }

            return value;
        }
    }
}
