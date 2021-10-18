using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Commands
{
    internal class AccountDataTimes : WorldReceivablePacket
    {
        internal Dictionary<AccountDataTypes, uint> DataTimes { get; }
        internal uint ServerTime { get; set; }

        internal AccountDataTimes(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            ServerTime = ReadUInt32();
            DataTimes = new Dictionary<AccountDataTypes, uint>();
            ReadSByte();
            ReadUInt32();
            int index = 0;
            while (ReadIndex < Buffer.Length)
            {
                DataTimes.Add((AccountDataTypes)index, ReadUInt32());
                index++;
            }
        }
    }
}
