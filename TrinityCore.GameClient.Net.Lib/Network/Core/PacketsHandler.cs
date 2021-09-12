using System.Collections.Generic;

namespace TrinityCore.GameClient.Net.Lib.Network.Core
{
    internal delegate void PacketHandler(ReceivablePacket content);
    public class PacketsHandler
    {
        private Dictionary<uint, List<PacketHandler>> Handlers { get; }

        internal PacketsHandler()
        {
            Handlers = new Dictionary<uint, List<PacketHandler>>();
        }

        internal void RegisterHandler(uint command, PacketHandler handler)
        {
            if (Handlers.ContainsKey(command))
                Handlers[command].Add(handler);
            else
                Handlers.Add(command, new List<PacketHandler> { handler });
        }

        protected bool Handle(ReceivablePacket packet)
        {
            if (!Handlers.ContainsKey(packet.Command)) return false;
            foreach (PacketHandler handler in Handlers[packet.Command]) handler(packet);
            return true;
        }
    }
}
