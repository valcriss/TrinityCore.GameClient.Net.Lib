using System.Numerics;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Models
{
    public class WorldPoint
    {
        #region Public Properties

        public uint AreaId { get; set; }
        public uint MapId { get; set; }
        public Vector3 Position { get; set; }

        #endregion Public Properties

        #region Public Methods

        public override string ToString()
        {
            return $"MapId:{MapId}, AreaId:{AreaId}, Position:{Position}";
        }

        #endregion Public Methods
    }
}