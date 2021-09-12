using TrinityCore.GameClient.Net.Lib.Auth.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.Auth
{
    public class AuthPacketsHandler : PacketsHandler
    {
        internal void Handle(AuthReceivablePacket authReceivablePacket)
        {
            base.Handle(authReceivablePacket);
        }

        internal void RegisterHandler(AuthCommand command, PacketHandler handler)
        {
            RegisterHandler((uint)command, handler);
        }
    }
}
