using System;
using TrinityCore.GameClient.Net.Lib.Components.Social.Commands.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Social.Commands.Incoming
{
    public class MessageChatInfo : ReceivablePacket<WorldCommand>
    {
        #region Public Properties

        public UInt32 AchievementId { get; set; }
        public string ChannelName { get; set; }
        public ChatType ChatType { get; set; }
        public Language Language { get; set; }
        public string Message { get; set; }
        public ulong ReceiverGuid { get; set; }
        public string ReceiverName { get; set; }
        public ulong SenderGuid { get; set; }
        public string SenderName { get; set; }

        #endregion Public Properties

        #region Internal Methods

        internal override void LoadData()
        {
            bool gmMessage = Command == WorldCommand.SMSG_GM_MESSAGECHAT;
            ChatType = (ChatType)ReadByte();
            Language = (Language)ReadUInt32();
            SenderGuid = (ulong)ReadUInt64();
            uint empty = ReadUInt32();
            switch (ChatType)
            {
                case ChatType.CHAT_MSG_MONSTER_SAY:
                case ChatType.CHAT_MSG_MONSTER_PARTY:
                case ChatType.CHAT_MSG_MONSTER_YELL:
                case ChatType.CHAT_MSG_MONSTER_WHISPER:
                case ChatType.CHAT_MSG_MONSTER_EMOTE:
                case ChatType.CHAT_MSG_RAID_BOSS_EMOTE:
                case ChatType.CHAT_MSG_RAID_BOSS_WHISPER:
                case ChatType.CHAT_MSG_BATTLENET:
                    SenderName = ReadUInt32String();
                    ReceiverGuid = (ulong)ReadUInt64();
                    if (IsDataLeft())
                    {
                        ReceiverName = ReadUInt32String();
                    }
                    break;

                case ChatType.CHAT_MSG_WHISPER_FOREIGN:
                    SenderName = ReadUInt32String();
                    ReceiverGuid = (ulong)ReadUInt64();
                    break;

                case ChatType.CHAT_MSG_BG_SYSTEM_NEUTRAL:
                case ChatType.CHAT_MSG_BG_SYSTEM_ALLIANCE:
                case ChatType.CHAT_MSG_BG_SYSTEM_HORDE:
                    ReceiverGuid = (ulong)ReadUInt64();
                    if (IsDataLeft())
                    {
                        ReceiverName = ReadUInt32String();
                    }
                    break;

                case ChatType.CHAT_MSG_ACHIEVEMENT:
                case ChatType.CHAT_MSG_GUILD_ACHIEVEMENT:
                    ReceiverGuid = (ulong)ReadUInt64();
                    break;

                default:
                    if (gmMessage)
                    {
                        SenderName = ReadUInt32String();
                    }
                    if (ChatType == ChatType.CHAT_MSG_CHANNEL)
                    {
                        ChannelName = ReadUInt32String();
                    }
                    ReceiverGuid = (ulong)ReadUInt64();
                    break;
            }

            Message = ReadUInt32String();
            sbyte empty2 = ReadSByte();
            if (ChatType == ChatType.CHAT_MSG_ACHIEVEMENT || ChatType == ChatType.CHAT_MSG_GUILD_ACHIEVEMENT)
                AchievementId = ReadUInt32();
        }

        #endregion Internal Methods
    }
}