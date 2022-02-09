using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Components.Environment.Models;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Environment.Commands.Incoming
{
    internal class TutorialFlagsInfo : ReceivablePacket<WorldCommand>
    {
        internal TutorialFlags TutorialFlags { get; set; }
        
        internal override void LoadData()
        {
            TutorialFlags = new TutorialFlags();
            for (int i = 0; i < TutorialFlags.MAX_ACCOUNT_TUTORIAL_VALUES; i++) TutorialFlags.Values[i] = ReadUInt32();
        }
    }
}
