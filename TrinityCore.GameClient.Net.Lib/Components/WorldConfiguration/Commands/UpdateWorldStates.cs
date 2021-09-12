using TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Entities;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Commands
{
    internal class UpdateWorldStates : WorldReceivablePacket
    {
        internal WorldStateVariable Variable { get; set; }

        internal UpdateWorldStates(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            Variable = new WorldStateVariable
            {
                Id = ReadUInt32(),
                Value = ReadUInt32()
            };
        }
    }
}
