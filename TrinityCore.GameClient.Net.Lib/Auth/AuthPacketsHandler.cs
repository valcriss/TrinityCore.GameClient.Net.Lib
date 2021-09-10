using TrinityCore.GameClient.Net.Lib.Auth.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.Auth
{
    public class AuthPacketsHandler : PacketsHandler
    {
        public void Handle(AuthReceivablePacket authReceivablePacket)
        {
            base.Handle(authReceivablePacket);
        }

        public void RegisterHandler(AuthCommand command, PacketHandler handler)
        {
            RegisterHandler((uint)command, handler);
        }
    }
}
