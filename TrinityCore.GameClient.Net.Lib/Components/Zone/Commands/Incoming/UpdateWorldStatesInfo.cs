using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.Components.Zone.Commands.Incoming
{
    internal class UpdateWorldStatesInfo : ReceivablePacket<Network.World.Enums.WorldCommand>
    {
        #region Internal Properties

        internal uint Id { get; set; }
        internal uint Value { get; set; }

        #endregion Internal Properties

        #region Internal Methods

        internal override void LoadData()
        {
            Id = ReadUInt32();
            Value = ReadUInt32();
        }

        #endregion Internal Methods
    }
}