using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Models;
using TrinityCore.GameClient.Net.Lib.Components.Social.Commands.Enums;
using TrinityCore.GameClient.Net.Lib.Network.World;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Social.Commands.Outgoing
{
    internal class EmoteRequest : WorldSendablePacket
    {
        internal EmoteRequest(TextEmotes textEmote, Entity target) : base(WorldCommand.CMSG_TEXT_EMOTE)
        {
            Append((UInt32)textEmote);
            Append((UInt32)0);
            Append(target != null ? (ulong)target.Guid : 0);
        }
    }
}
