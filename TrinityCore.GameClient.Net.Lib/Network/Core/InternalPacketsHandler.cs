using System.Collections.Generic;

namespace TrinityCore.GameClient.Net.Lib.Network.Core
{
    internal enum Internals
    {
        UPDATE_FIELDS,
        CREATE_OBJECTS,
        MOVEMENTS,
        OUT_OF_RANGES
    }
    
    internal delegate void InternalPacketHandler(object content);

    internal class InternalPacketsHandler
    {
        private Dictionary<Internals, List<InternalPacketHandler>> Handlers { get; }

        internal InternalPacketsHandler()
        {
            Handlers = new Dictionary<Internals, List<InternalPacketHandler>>();
        }

        internal bool Handle(Internals command, object content)
        {
            if (!Handlers.ContainsKey(command)) return false;
            foreach (InternalPacketHandler handler in Handlers[command]) handler(content);
            return true;
        }

        internal void RegisterHandler(Internals command, InternalPacketHandler handler)
        {
            if (Handlers.ContainsKey(command))
                Handlers[command].Add(handler);
            else
                Handlers.Add(command, new List<InternalPacketHandler> { handler });
        }
    }
}
