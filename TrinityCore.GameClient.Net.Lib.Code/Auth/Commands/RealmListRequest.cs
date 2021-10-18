using TrinityCore.GameClient.Net.Lib.Auth.Enums;

namespace TrinityCore.GameClient.Net.Lib.Auth.Commands
{
    internal class RealmListRequest : AuthSendablePacket
    {
        internal RealmListRequest() : base(AuthCommand.REALM_LIST)
        {
            Append(0);
            Append(0);
            Append(0);
            Append(0);
        }
    }
}