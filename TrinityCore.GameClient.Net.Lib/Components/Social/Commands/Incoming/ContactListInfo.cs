using System;
using TrinityCore.GameClient.Net.Lib.Components.Social.Enums;
using TrinityCore.GameClient.Net.Lib.Components.Social.Models;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Social.Commands.Incoming
{
    internal class ContactListInfo : ReceivablePacket<Network.World.Enums.WorldCommand>
    {
        #region Internal Properties

        internal Contacts Contacts { get; set; }

        #endregion Internal Properties

        #region Internal Methods

        internal override void LoadData()
        {
            Contacts = new Contacts();
            ReadUInt32();
            uint count = ReadUInt32();
            for (int i = 0; i < count; i++)
            {
                UInt64 guid = ReadUInt64();
                ContactType type = (ContactType)ReadUInt32();
                string note = ReadCString();

                if (type == ContactType.FRIEND)
                {
                    UInt32? areaId = null;
                    UInt32? level = null;
                    Class? @class = null;
                    FriendStatus status = (FriendStatus)ReadByte();
                    if ((status & FriendStatus.FRIEND_STATUS_ONLINE) == FriendStatus.FRIEND_STATUS_ONLINE)
                    {
                        areaId = ReadUInt32();
                        level = ReadUInt32();
                        @class = (Class)ReadUInt32();
                    }
                    Contacts.Friends.Add(new Friend(guid, note, status, areaId, level, @class));
                }
                else if (type == ContactType.IGNORED)
                {
                    Contacts.Ignored.Add(new Contact(guid, note));
                }
                else if (type == ContactType.MUTED)
                {
                    Contacts.Muted.Add(new Contact(guid, note));
                }
            }
        }

        #endregion Internal Methods
    }
}