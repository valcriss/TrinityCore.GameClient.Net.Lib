using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Models
{
    internal class UpdateCreateObject
    {
        #region Internal Properties

        internal Dictionary<UpdateFields, uint> Fields { get; set; }
        internal ulong Guid { get; set; }
        internal MovementInfo Movement { get; set; }
        internal TypeID ObjectType { get; set; }

        #endregion Internal Properties

        #region Internal Constructors

        internal UpdateCreateObject()
        {
            Fields = new Dictionary<UpdateFields, uint>();
        }

        #endregion Internal Constructors
    }
}