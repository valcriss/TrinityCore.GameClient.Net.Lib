using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;
using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Clients
{
    internal abstract class GameComponent
    {
        protected WorldClient WorldClient { get; set; }

        public virtual void SetWorldClient(WorldClient worldClient)
        {
            WorldClient = worldClient;
        }

        internal abstract void RegisterHandlers();

        protected virtual void RegisterHandler(WorldCommand command, PacketHandler handler)
        {
            WorldClient?.AppendHandler(command, handler);
        }

        protected virtual void RegisterInternalHandler(Internals command, InternalPacketHandler handler)
        {
            WorldClient?.AppendInternalHandler(command, handler);
        }

        protected virtual void Close()
        {

        }
    }
}
