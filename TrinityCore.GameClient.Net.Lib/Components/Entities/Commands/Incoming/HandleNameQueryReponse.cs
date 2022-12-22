using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Commands.Incoming
{
    internal class HandleNameQueryReponse : ReceivablePacket<Network.World.Enums.WorldCommand>
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