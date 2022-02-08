using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Logging;

namespace TrinityCore.GameClient.Net.Lib.Network.Core
{
    internal class PacketsHandler<T>
    {
        #region Internal Delegates

        internal delegate void PacketHandler(ReceivablePacket<T> content);

        #endregion Internal Delegates

        #region Internal Properties

        internal Dictionary<T, List<PacketHandler>> Handlers { get; }

        #endregion Internal Properties

        private List<T> Ignored { get; set; }

        #region Internal Constructors

        internal PacketsHandler()
        {
            Handlers = new Dictionary<T, List<PacketHandler>>();
            Ignored = new List<T>();
        }

        #endregion Internal Constructors

        #region Internal Methods

        internal bool Handle(ReceivablePacket<T> packet)
        {
            if (!Handlers.ContainsKey(packet.Command))
            {
                if (!Ignored.Contains(packet.Command))
                {
                    Logger.Append(Logging.Enums.LogCategory.NETWORK, Logging.Enums.LogLevel.WARNING, "Unhandled OpCode : " + packet.Command + " (" + packet.Content.Length + ")");
                    Ignored.Add(packet.Command);
                }
                return false;
            }
            foreach (PacketHandler handler in Handlers[packet.Command]) handler(packet);
            return true;
        }

        internal void RegisterHandler(T command, PacketHandler handler)
        {
            if (Handlers.ContainsKey(command))
                Handlers[command].Add(handler);
            else
                Handlers.Add(command, new List<PacketHandler> { handler });
        }

        #endregion Internal Methods
    }
}