using System;
using TrinityCore.GameClient.Net.Lib.Components.Social.Enums;
using TrinityCore.GameClient.Net.Lib.Components.Social.Models;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Social.Commands.Incoming
{
    internal class FriendStatusUpdateInfo : ReceivablePacket<Network.World.Enums.WorldCommand>
    {
        #region Internal Properties

        internal Friend Friend { get; set; }

        #endregion Internal Properties

        #region Internal Methods

        internal override void LoadData()
        {
            FriendsResult result = (FriendsResult)ReadByte();
            UInt64 guid = ReadUInt64();
            string note = string.Empty;
            UInt32? areaId = null;
            UInt32? level = null;
            Class? @class = null;
            FriendStatus status = FriendStatus.FRIEND_STATUS_OFFLINE;

            switch (result)
            {
                case FriendsResult.FRIEND_ADDED_OFFLINE:
                case FriendsResult.FRIEND_ADDED_ONLINE:
                    note = ReadCString();
                    break;

                default:
                    break;
            }

            switch (result)
            {
                case FriendsResult.FRIEND_ADDED_ONLINE:
                case FriendsResult.FRIEND_ONLINE:
                    status = (FriendStatus)ReadByte();
                    areaId = ReadUInt32();
                    level = ReadUInt32();
                    @class = (Class)ReadUInt32();
                    break;

                default:
                    break;
            }

            Friend = new Friend(guid, note, status, areaId, level, @class);
        }

        #endregion Internal Methods
    }
}