using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Components.Social.Commands.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Social.Commands.Incoming
{
    internal class EmoteInfo : ReceivablePacket<WorldCommand>
    {
        public Emote Emote { get; set; }
        public ulong Guid { get; set; }

        internal override void LoadData()
        {
            Emote = (Emote)ReadUInt32();
            Guid = (ulong)ReadUInt64();
        }
    }
}
