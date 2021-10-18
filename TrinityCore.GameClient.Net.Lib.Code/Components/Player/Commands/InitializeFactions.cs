using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Components.Player.Entities;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands
{
    internal class InitializeFactions : WorldReceivablePacket
    {
        internal List<Faction> Factions { get; set; }

        internal InitializeFactions(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            Factions = new List<Faction>();
            uint count = ReadUInt32();
            for (int i = 0; i < count; i++)
                Factions.Add(new Faction
                {
                    Id = i,
                    Flags = ReadSByte(),
                    Standing = ReadUInt32()
                });
        }
    }
}
