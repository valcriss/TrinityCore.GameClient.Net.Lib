using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Environment.Commands.Incoming
{
    internal class ClientCacheVersionInfo : ReceivablePacket<WorldCommand>
    {
        #region Internal Properties

        internal uint Version { get; set; }

        #endregion Internal Properties

        #region Internal Methods

        internal override void LoadData()
        {
            Version = ReadUInt32();
        }

        #endregion Internal Methods
    }
}