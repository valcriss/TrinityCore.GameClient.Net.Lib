using System.Numerics;
using TrinityCore.GameClient.Net.Lib.Components.Player.Models;
using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands.Incoming
{
    internal class BindPointUpdate : ReceivablePacket<Network.World.Enums.WorldCommand>
    {
        #region Public Properties

        public WorldPoint BindPoint { get; set; }

        #endregion Public Properties

        #region Internal Methods

        internal override void LoadData()
        {
            Vector3 position = ReadVector3();
            uint mapId = ReadUInt32();
            uint areaId = ReadUInt32();
            BindPoint = new WorldPoint() { AreaId = areaId, MapId = mapId, Position = position };
        }

        #endregion Internal Methods
    }
}