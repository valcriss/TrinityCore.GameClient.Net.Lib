using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Components.Player.Entities;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands
{
    public class InitialSpells : WorldReceivablePacket
    {
        public List<Spell> Spells { get; set; }

        public InitialSpells(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            Spells = new List<Spell>();
            ReadSByte();
            ushort count = ReadUInt16();
            for (int i = 0; i < count; i++)
            {
                Spells.Add(new Spell(ReadUInt32()));
                ReadUInt16();
            }

            // TODO : Add spell history struct info
        }
    }
}
