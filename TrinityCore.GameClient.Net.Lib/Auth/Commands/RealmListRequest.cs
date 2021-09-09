using TrinityCore.GameClient.Net.Lib.Auth.Enums;

namespace TrinityCore.GameClient.Net.Lib.Auth.Commands
{
    public class RealmListRequest : AuthSendablePacket
    {
        public RealmListRequest() : base(AuthCommand.REALM_LIST)
        {
            Append(0);
            Append(0);
            Append(0);
            Append(0);
        }
    }
}