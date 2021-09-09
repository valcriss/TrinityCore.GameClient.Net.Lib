using System;
using System.Collections.Generic;
using System.Text;

namespace TrinityCore.GameClient.Net.Lib.Network.Core
{
    public delegate void OnGameSocketConnectedEventHandler(GameSocket sender);
    public delegate void OnGameSocketDisconnectedEventHandler(GameSocket sender);
}
