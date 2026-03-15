namespace TrinityCore.GameClient.Net.Protocol.World;

public enum ChatMessageType : uint
{
    Addon = 0xFF,
    System = 0x00,
    Say = 0x01,
    Party = 0x02,
    Raid = 0x03,
    Guild = 0x04,
    Officer = 0x05,
    Yell = 0x06,
    Whisper = 0x07,
    WhisperForeign = 0x08,
    WhisperInform = 0x09,
    Emote = 0x0A,
    TextEmote = 0x0B,
    MonsterSay = 0x0C,
    MonsterParty = 0x0D,
    MonsterYell = 0x0E,
    MonsterWhisper = 0x0F,
    MonsterEmote = 0x10,
    Channel = 0x11,
    Afk = 0x17,
    Dnd = 0x18,
    RaidLeader = 0x27,
    RaidWarning = 0x28,
    Battleground = 0x2C,
    BattlegroundLeader = 0x2D,
    GuildAchievement = 0x31,
    PartyLeader = 0x33
}

public enum ChatChannel : uint
{
    Say = 0x01,
    Party = 0x02,
    Group = Party,
    Raid = 0x03,
    Guild = 0x04,
    Officer = 0x05,
    Yell = 0x06,
    Shout = Yell,
    Whisper = 0x07,
    Emote = 0x0A,
    Channel = 0x11,
    RaidLeader = 0x27,
    RaidWarning = 0x28,
    Battleground = 0x2C,
    BattlegroundLeader = 0x2D,
    GuildAchievement = 0x31,
    PartyLeader = 0x33
}

public enum ChatLanguage : uint
{
    Auto = 0xFFFFFFFE,
    Universal = 0,
    Orcish = 1,
    Darnassian = 2,
    Taurahe = 3,
    Dwarvish = 6,
    Common = 7,
    Demonic = 8,
    Titan = 9,
    Thalassian = 10,
    Draconic = 11,
    Kalimag = 12,
    Gnomish = 13,
    Troll = 14,
    Gutterspeak = 33,
    Draenei = 35,
    Zombie = 36,
    GnomishBinary = 37,
    GoblinBinary = 38,
    Addon = 0xFFFFFFFF
}

public sealed record GroupInviteInfo(
    bool CanAccept,
    string InviterName,
    uint ProposedRoles,
    IReadOnlyList<uint> LfgSlots,
    uint LfgCompletedMask);

public sealed record GroupMemberInfo(
    ulong Guid,
    string Name,
    byte OnlineState,
    byte SubGroup,
    byte Flags,
    byte Roles);

public sealed record GroupMembershipInfo(
    byte GroupType,
    byte MemberSubGroup,
    byte MemberFlags,
    byte MemberRoles,
    ulong GroupGuid,
    uint Sequence,
    uint OtherMemberCount,
    IReadOnlyList<GroupMemberInfo> Members,
    ulong LeaderGuid);

public sealed record PartyCommandResultInfo(
    uint Operation,
    string MemberName,
    uint Result,
    uint Value);

public sealed record ReceivedChatMessage(
    ChatMessageType MessageType,
    bool IsGameMasterMessage,
    ChatLanguage Language,
    ulong SenderGuid,
    ulong ReceiverGuid,
    string Message,
    string? SenderName,
    string? ReceiverName,
    string? ChannelName,
    byte ChatTag);
