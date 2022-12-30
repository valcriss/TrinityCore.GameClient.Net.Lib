namespace TrinityCore.GameClient.Net.Lib.Components.Social.Enums
{
    internal enum OutChatType : byte
    {
        CHAT_MSG_SAY = 0x01,
        CHAT_MSG_PARTY = 0x02,
        CHAT_MSG_RAID = 0x03,
        CHAT_MSG_GUILD = 0x04,
        CHAT_MSG_OFFICER = 0x05,
        CHAT_MSG_YELL = 0x06,
        CHAT_MSG_WHISPER = 0x07,
        CHAT_MSG_CHANNEL = 0x11,
    };
}