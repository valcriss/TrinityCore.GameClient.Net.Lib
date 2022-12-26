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
    public class TextEmoteInfo : ReceivablePacket<WorldCommand>
    {
        public ulong Guid { get; set; }
        public TextEmotes TextEmote { get; set; }
        public string Name { get; set; }

        internal override void LoadData()
        {
            Guid = ReadUInt64();
            TextEmote = (TextEmotes)ReadUInt32();
            uint number = ReadUInt32();
            Name = ReadUInt32String();
        }
    }
}
