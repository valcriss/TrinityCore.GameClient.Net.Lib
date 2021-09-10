using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Components.Player.Entities;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands
{
    public class ForcedReactions : WorldReceivablePacket
    {
        public List<Faction> ForcedFactions { get; set; }

        public ForcedReactions(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            uint count = ReadUInt32();
            ForcedFactions = new List<Faction>();
            for (int i = 0; i < count; i++)
                ForcedFactions.Add(new Faction
                {
                    Id = (int)ReadUInt32(),
                    Standing = ReadUInt32()
                });
        }
    }
}
