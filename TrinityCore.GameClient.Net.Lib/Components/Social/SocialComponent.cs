using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Models;
using TrinityCore.GameClient.Net.Lib.Components.Social.Commands.Enums;
using TrinityCore.GameClient.Net.Lib.Components.Social.Commands.Incoming;
using TrinityCore.GameClient.Net.Lib.Components.Social.Commands.Outgoing;
using TrinityCore.GameClient.Net.Lib.Components.Social.Models;
using TrinityCore.GameClient.Net.Lib.Logging;
using TrinityCore.GameClient.Net.Lib.Logging.Enums;
using TrinityCore.GameClient.Net.Lib.Network.World;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Social
{
    public class SocialComponent : Component
    {
        #region Public Delegates

        public delegate void SocialChatMessageEventHandler(MessageChatInfo chatMessage);
        public delegate void SocialTextEmoteEventHandler(TextEmoteInfo textEmote);

        #endregion Public Delegates

        #region Public Events

        public event SocialChatMessageEventHandler OnChatMessage;
        public event SocialTextEmoteEventHandler OnTextEmote;

        #endregion Public Events

        #region Public Properties

        public Contacts Contacts { get; set; }

        #endregion Public Properties

        #region Public Constructors

        public SocialComponent(WorldClient worldClient) : base(worldClient)
        {
            WorldClient.PacketsHandler.RegisterHandler<LearnedDanceMovesInfo>(WorldCommand.SMSG_LEARNED_DANCE_MOVES, LearnedDanceMovesInfo);
            WorldClient.PacketsHandler.RegisterHandler<ContactListInfo>(WorldCommand.SMSG_CONTACT_LIST, ContactListInfo);
            WorldClient.PacketsHandler.RegisterHandler<FriendStatusUpdateInfo>(WorldCommand.SMSG_FRIEND_STATUS, FriendStatusUpdateInfo);
            WorldClient.PacketsHandler.RegisterHandler<MessageChatInfo>(WorldCommand.SMSG_GM_MESSAGECHAT, MessageChatInfo);
            WorldClient.PacketsHandler.RegisterHandler<MessageChatInfo>(WorldCommand.SMSG_MESSAGECHAT, MessageChatInfo);
            WorldClient.PacketsHandler.RegisterHandler<EmoteInfo>(WorldCommand.SMSG_EMOTE, EmoteInfo);
            WorldClient.PacketsHandler.RegisterHandler<TextEmoteInfo>(WorldCommand.SMSG_TEXT_EMOTE, TextEmoteInfo);
        }

        #endregion Public Constructors

        #region Private Methods

        private bool ContactListInfo(ContactListInfo contactList)
        {
            Contacts = contactList.Contacts;
            Logger.Append(LogCategory.SOCIAL, LogLevel.DEBUG, "Friends : " + Contacts.Friends.Count.ToString());
            return true;
        }

        private bool FriendStatusUpdateInfo(FriendStatusUpdateInfo friendStatusUpdate)
        {
            return Contacts.UpdateFriend(friendStatusUpdate.Friend);
        }

        private bool LearnedDanceMovesInfo(LearnedDanceMovesInfo learnedDanceMoves)
        {
            return true;
        }

        private bool MessageChatInfo(MessageChatInfo messageChatInfo)
        {
            Logger.Append(LogCategory.SOCIAL, LogLevel.VERBOSE, "MessageChat (" + messageChatInfo.SenderName + ") (" + messageChatInfo.Message + ")");
            Task.Run(() => OnChatMessage?.Invoke(messageChatInfo));
            return true;
        }

        public bool Say(string value, Language language = Language.LANG_COMMON)
        {
            return WorldClient.Send(new MessageChatRequest(OutChatType.CHAT_MSG_SAY, language, value));
        }

        public bool Shout(string value, Language language = Language.LANG_COMMON)
        {
            return Yell(value, language);
        }

        public bool Yell(string value, Language language = Language.LANG_COMMON)
        {
            return WorldClient.Send(new MessageChatRequest(OutChatType.CHAT_MSG_YELL, language, value));
        }

        public bool Party(string value, Language language = Language.LANG_COMMON)
        {
            return WorldClient.Send(new MessageChatRequest(OutChatType.CHAT_MSG_PARTY, language, value));
        }

        public bool Raid(string value, Language language = Language.LANG_COMMON)
        {
            return WorldClient.Send(new MessageChatRequest(OutChatType.CHAT_MSG_RAID, language, value));
        }

        public bool Guild(string value, Language language = Language.LANG_COMMON)
        {
            return WorldClient.Send(new MessageChatRequest(OutChatType.CHAT_MSG_GUILD, language, value));
        }

        public bool Officer(string value, Language language = Language.LANG_COMMON)
        {
            return WorldClient.Send(new MessageChatRequest(OutChatType.CHAT_MSG_OFFICER, language, value));
        }

        public bool Whisper(string to, string value, Language language = Language.LANG_COMMON)
        {
            return WorldClient.Send(new MessageChatRequest(OutChatType.CHAT_MSG_WHISPER, language, to, value));
        }

        public bool Channel(string channel, string value, Language language = Language.LANG_COMMON)
        {
            return WorldClient.Send(new MessageChatRequest(channel, OutChatType.CHAT_MSG_CHANNEL, language, value));
        }

        public bool Emote(TextEmotes textEmote, Entity target = null)
        {
            return WorldClient.Send(new EmoteRequest(textEmote, target));
        }

        private bool EmoteInfo(EmoteInfo emoteInfo)
        {
            Logger.Append(LogCategory.SOCIAL, LogLevel.VERBOSE, "Emote (" + emoteInfo.Emote + ") (" + emoteInfo.Guid + ")");
            return true;
        }

        private bool TextEmoteInfo(TextEmoteInfo textEmoteInfo)
        {
            Logger.Append(LogCategory.SOCIAL, LogLevel.VERBOSE, "Text Emote (" + textEmoteInfo.Guid + ") (" + textEmoteInfo.TextEmote + ") (" + textEmoteInfo.Name + ")");
            if (textEmoteInfo.Guid != WorldClient.GetCharacter()?.GUID)
            {
                Task.Run(() => OnTextEmote?.Invoke(textEmoteInfo));
            }
            return true;
        }

        #endregion Private Methods
    }
}