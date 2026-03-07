using System.Collections.Generic;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Models
{
    internal class UpdateOutOfRange
    {
        #region Internal Properties

        internal List<ulong> GuidList { get; set; }

        #endregion Internal Properties

        #region Internal Constructors

        internal UpdateOutOfRange()
        {
            GuidList = new List<ulong>();
        }

        #endregion Internal Constructors
    }
}