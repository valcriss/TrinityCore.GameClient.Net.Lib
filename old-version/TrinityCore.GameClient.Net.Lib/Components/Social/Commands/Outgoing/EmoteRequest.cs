using TrinityCore.GameClient.Net.Lib.Components.Entities.Models;
using TrinityCore.GameClient.Net.Lib.Components.Social.Enums;
using TrinityCore.GameClient.Net.Lib.Network.World;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Social.Commands.Outgoing
{
    internal class EmoteRequest : WorldSendablePacket
    {
        #region Internal Constructors

        internal EmoteRequest(TextEmotes textEmote, Entity target) : base(WorldCommand.CMSG_TEXT_EMOTE)
        {
            Append((uint)textEmote);
            Append((uint)0);
            Append(target != null ? target.Guid : 0);
        }

        #endregion Internal Constructors
    }
}