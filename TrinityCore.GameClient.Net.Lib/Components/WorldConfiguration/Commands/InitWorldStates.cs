using TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Entities;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Commands
{
    internal class InitWorldStates : WorldReceivablePacket
    {
        internal WorldState WorldState { get; set; }

        internal InitWorldStates(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            WorldState = new WorldState { MapId = ReadInt32(), ZoneId = ReadInt32(), AreaId = ReadInt32() };

            ushort count = ReadUInt16();

            for (int i = 0; i < count; i++)
            {
                uint id = ReadUInt32();
                uint value = ReadUInt32();
                WorldState.Variables.Add(id, new WorldStateVariable
                {
                    Id = id,
                    Value = value
                });
            }
        }
    }
}
