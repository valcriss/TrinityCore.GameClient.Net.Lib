using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Commands.Incoming
{
    internal class NameQueryResponse : ReceivablePacket<WorldCommand>
    {
        #region Internal Properties

        internal bool Found { get; set; }
        internal ulong Guid { get; set; }
        internal string Name { get; set; }

        #endregion Internal Properties

        #region Internal Methods

        internal override void LoadData()
        {
            Name = string.Empty;
            Guid = ReadPackedGuid();
            Found = !ReadBoolean();
            if (!Found) //! True if not found, false if found
                return;
            Name = ReadCString();
        }

        #endregion Internal Methods
    }
}