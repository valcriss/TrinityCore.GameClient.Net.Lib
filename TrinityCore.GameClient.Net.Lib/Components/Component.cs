using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Network.World;

namespace TrinityCore.GameClient.Net.Lib.Components
{
    public abstract class Component
    {
        protected WorldClient WorldClient { get; set; }
        internal Component(WorldClient worldClient)
        {
            WorldClient = worldClient;
        }
    }
}
