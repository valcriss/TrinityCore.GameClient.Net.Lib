using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;
using System.Numerics;
using TrinityCore.GameClient.Net.Lib.Components.Player.Entities;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands
{
    internal class BindPointUpdate : WorldReceivablePacket
    {
        internal BindPosition BindPosition { get; set; }

        internal BindPointUpdate(ReceivablePacket receivablePacket, int readIndex = 0) : base(receivablePacket, readIndex)
        {
            Vector3 position = ReadVector3();
            uint mapId = ReadUInt32();
            uint areId = ReadUInt32();
            BindPosition = new BindPosition(position, mapId, areId);
        }
    }
}
