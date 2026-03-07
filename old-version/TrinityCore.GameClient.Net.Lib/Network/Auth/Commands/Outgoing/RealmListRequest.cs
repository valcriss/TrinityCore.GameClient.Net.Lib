using TrinityCore.GameClient.Net.Lib.Network.Auth.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.Auth.Commands.Outgoing
{
    internal class RealmListRequest : AuthSendablePacket
    {
        #region Internal Constructors

        internal RealmListRequest() : base(AuthCommand.REALM_LIST)
        {
            Append(0);
            Append(0);
            Append(0);
            Append(0);
        }

        #endregion Internal Constructors
    }
}