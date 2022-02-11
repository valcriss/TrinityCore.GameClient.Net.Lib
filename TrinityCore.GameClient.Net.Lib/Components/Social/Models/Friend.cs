using System;
using TrinityCore.GameClient.Net.Lib.Components.Social.Enums;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Social.Models
{
    public class Friend : Contact
    {
        #region Public Properties

        public UInt32? AreaId { get; set; }
        public Class? Class { get; set; }
        public UInt32? Level { get; set; }
        public FriendStatus Status { get; set; }

        #endregion Public Properties

        #region Public Constructors

        public Friend(UInt64 guid, string note, FriendStatus status, UInt32? areaId = null, UInt32? level = null, Class? @class = null) : base(guid, note)
        {
            Status = status;
            AreaId = areaId;
            Level = level;
            Class = @class;
        }

        #endregion Public Constructors
    }
}