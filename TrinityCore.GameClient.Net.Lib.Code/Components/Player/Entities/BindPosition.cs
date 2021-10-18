using System.Numerics;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Entities
{
    internal class BindPosition
    {
        internal uint AreaId { get; set; }
        internal uint MapId { get; set; }
        internal Vector3 Position { get; set; }

        internal BindPosition()
        {
            Position = new Vector3();
        }

        internal BindPosition(Vector3 position, uint mapId, uint areaId)
        {
            Position = position;
            MapId = mapId;
            AreaId = areaId;
        }

        public override string ToString()
        {
            return "MapId:" + MapId + ", AreaId:" + AreaId + ", Position:" + Position;
        }
    }
}
