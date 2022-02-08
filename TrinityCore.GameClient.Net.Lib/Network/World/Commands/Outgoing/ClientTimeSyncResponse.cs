using System;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Commands.Outgoing
{
    internal class ClientTimeSyncResponse : WorldSendablePacket
    {
        #region Internal Constructors

        internal ClientTimeSyncResponse(uint counter) : base(WorldCommand.CMSG_TIME_SYNC_RESP)
        {
            Append(counter);
            Append((uint)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
        }

        #endregion Internal Constructors
    }
}