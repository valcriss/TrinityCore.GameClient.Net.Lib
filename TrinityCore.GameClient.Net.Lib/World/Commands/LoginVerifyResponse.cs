using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.World.Commands
{
    internal class LoginVerifyResponse : WorldReceivablePacket
    {
        internal uint MapId { get; set; }
        internal float O { get; set; }
        internal float X { get; set; }
        internal float Y { get; set; }
        internal float Z { get; set; }

        internal LoginVerifyResponse(ReceivablePacket receivablePacket, int readIndex = 0) : base(receivablePacket,
            readIndex)
        {
            MapId = ReadUInt32();
            X = ReadSingle();
            Y = ReadSingle();
            Z = ReadSingle();
            O = ReadSingle();
        }
    }
}
