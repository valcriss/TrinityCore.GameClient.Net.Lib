using System;
using TrinityCore.GameClient.Net.Lib.Components.Social.Enums;
using TrinityCore.GameClient.Net.Lib.Network.World;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Social.Commands.Outgoing
{
    internal class MessageChatRequest : WorldSendablePacket
    {
        #region Internal Constructors

        internal MessageChatRequest(OutChatType chatType, Language language, string message) : this(chatType, language, null, null, message)
        {
        }

        internal MessageChatRequest(OutChatType chatType, Language language, string to, string message) : this(chatType, language, to, null, message)
        {
        }

        internal MessageChatRequest(string channel, OutChatType chatType, Language language, string message) : this(chatType, language, null, channel, message)
        {
        }

        #endregion Internal Constructors

        #region Private Constructors

        private MessageChatRequest(OutChatType chatType, Language language, string to, string channel, string message) : base(WorldCommand.CMSG_MESSAGECHAT)
        {
            Append((UInt32)chatType);
            Append((UInt32)language);
            if (to != null) Append(to);
            if (channel != null) Append(channel);
            Append(message);
        }

        #endregion Private Constructors
    }
}