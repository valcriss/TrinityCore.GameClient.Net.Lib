using TrinityCore.GameClient.Net.Lib.Components.Social.Commands.Incoming;
using TrinityCore.GameClient.Net.Lib.Components.Social.Models;
using TrinityCore.GameClient.Net.Lib.Network.World;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Social
{
    public class SocialComponent : Component
    {
        #region Public Properties

        public Contacts Contacts { get; set; }

        #endregion Public Properties

        #region Public Constructors

        public SocialComponent(WorldClient worldClient) : base(worldClient)
        {
            WorldClient.PacketsHandler.RegisterHandler<LearnedDanceMovesInfo>(WorldCommand.SMSG_LEARNED_DANCE_MOVES, LearnedDanceMovesInfo);
            WorldClient.PacketsHandler.RegisterHandler<ContactListInfo>(WorldCommand.SMSG_CONTACT_LIST, ContactListInfo);
        }

        #endregion Public Constructors

        #region Private Methods

        private bool ContactListInfo(ContactListInfo contactList)
        {
            Contacts = contactList.Contacts;
            return true;
        }

        private bool LearnedDanceMovesInfo(LearnedDanceMovesInfo learnedDanceMoves)
        {
            return true;
        }

        #endregion Private Methods
    }
}