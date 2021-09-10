using System.Collections.Generic;

namespace TrinityCore.GameClient.Net.Lib.Network.Core
{
    public enum Internals
    {
        UPDATE_FIELDS,
        CREATE_OBJECTS,
        MOVEMENTS,
        OUT_OF_RANGES
    }
    
    public delegate void InternalPacketHandler(object content);

    public class InternalPacketsHandler
    {
        private Dictionary<Internals, List<InternalPacketHandler>> Handlers { get; }

        public InternalPacketsHandler()
        {
            Handlers = new Dictionary<Internals, List<InternalPacketHandler>>();
        }

        public bool Handle(Internals command, object content)
        {
            if (!Handlers.ContainsKey(command)) return false;
            foreach (InternalPacketHandler handler in Handlers[command]) handler(content);
            return true;
        }

        public void RegisterHandler(Internals command, InternalPacketHandler handler)
        {
            if (Handlers.ContainsKey(command))
                Handlers[command].Add(handler);
            else
                Handlers.Add(command, new List<InternalPacketHandler> { handler });
        }
    }
}
