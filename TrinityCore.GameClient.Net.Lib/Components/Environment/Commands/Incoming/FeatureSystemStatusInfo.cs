using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Environment.Commands.Incoming
{
    internal class FeatureSystemStatusInfo : ReceivablePacket<WorldCommand>
    {
        internal bool VoiceChatEnabled { get; set; }
        internal override void LoadData()
        {
            ReadSByte();
            VoiceChatEnabled = ReadSByte() != 0;
        }
    }
}
