using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Entities
{
    public class BindPosition
    {
        public uint AreaId { get; set; }
        public uint MapId { get; set; }
        public Vector3 Position { get; set; }

        public BindPosition()
        {
            Position = new Vector3();
        }

        public BindPosition(Vector3 position, uint mapId, uint areaId)
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
