using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Commands.Incoming
{
    internal class ServerTimeSyncRequest : ReceivablePacket<WorldCommand>
    {
        #region Internal Properties

        internal uint SyncNextCounter { get; set; }

        #endregion Internal Properties

        #region Internal Methods

        internal override void LoadData()
        {
            SyncNextCounter = ReadUInt32();
            base.LoadData();
        }

        #endregion Internal Methods

    }
}