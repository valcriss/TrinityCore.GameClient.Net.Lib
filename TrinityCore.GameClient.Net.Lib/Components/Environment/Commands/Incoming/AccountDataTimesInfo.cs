using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Components.Environment.Enums;
using TrinityCore.GameClient.Net.Lib.Components.Environment.Models;
using TrinityCore.GameClient.Net.Lib.Logging;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Environment.Commands.Incoming
{
    internal class AccountDataTimesInfo : ReceivablePacket<WorldCommand>
    {
        internal AccountDataTimes DataTimes { get; set; }
        internal uint ServerTime { get; set; }
        internal override void LoadData()
        {
            ServerTime = ReadUInt32();
            DataTimes = new AccountDataTimes();
            ReadSByte();
            ReadUInt32();
            int index = 0;
            while (Buffer.Length - ReadIndex >= 4)
            {
                DataTimes.Values.Add((AccountDataTimesTypes)index, ReadUInt32());
                index++;
            }
        }
    }
}
