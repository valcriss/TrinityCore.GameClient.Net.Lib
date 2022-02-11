using System;

namespace TrinityCore.GameClient.Net.Lib.Components.Social.Enums
{
    [Flags]
    internal enum ContactListAction
    {
        FRIEND_LIST_UPDATE = 0x01,
        IGNORED_LIST_UPDATE = 0x02,
        MUTED_LIST_UPDATE = 0x04,

        ALL_UPDATED = FRIEND_LIST_UPDATE | IGNORED_LIST_UPDATE | MUTED_LIST_UPDATE
    }
}