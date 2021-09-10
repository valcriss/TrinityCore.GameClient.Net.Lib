using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.World;
using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Commands
{
    public class TimeSyncResponse : WorldSendablePacket
    {
        public TimeSyncResponse(WorldSocket worldSocket, uint counter) : base(worldSocket, WorldCommand.CMSG_TIME_SYNC_RESP)
        {
            Append(counter);
            Append((uint)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
        }
    }
}
