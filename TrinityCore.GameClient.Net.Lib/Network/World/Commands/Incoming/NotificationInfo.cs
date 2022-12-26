using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Commands.Incoming
{
    internal class NotificationInfo : ReceivablePacket<WorldCommand>
    {
        public string Message { get; set; }

        internal override void LoadData()
        {
            Message = ReadCString();
        }
    }
}
