using System;
using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Logging;
using TrinityCore.GameClient.Net.Lib.Network.Tools;

namespace TrinityCore.GameClient.Net.Lib.Network.Core
{
    internal class PacketsHandler<T> where T : struct, IConvertible
    {
        #region Internal Classes

        internal class PacketsHandlerItem
        {
            #region Internal Properties

            internal dynamic Handler { get; set; }
            internal Type Type { get; set; }

            #endregion Internal Properties
        }

        #endregion Internal Classes

        #region Internal Properties

        internal Dictionary<T, List<PacketsHandlerItem>> Handlers { get; }

        #endregion Internal Properties

        #region Private Properties

        private Dictionary<T, T> CompressedCommands { get; }
        private List<T> Ignored { get; set; }

        #endregion Private Properties

        #region Internal Constructors

        internal PacketsHandler()
        {
            CompressedCommands = new Dictionary<T, T>();
            Handlers = new Dictionary<T, List<PacketsHandlerItem>>();
            Ignored = new List<T>();
        }

        #endregion Internal Constructors

        #region Internal Methods

        internal void AddCompressed(T source, T target)
        {
            CompressedCommands.Add(source, target);
        }

        internal bool Handle(ReceivablePacket<T> packet)
        {
            if (CompressedCommands.ContainsKey(packet.Command))
            {
                byte[] decompressed = packet.Content.Decompress();
                if (decompressed.Length == 0) return false;
                T decompressedCommand = CompressedCommands[packet.Command];
                packet = new ReceivablePacket<T>(decompressedCommand, decompressed);
            }

            if (!Handlers.ContainsKey(packet.Command))
            {
                if (!Ignored.Contains(packet.Command))
                {
                    Logger.Append(Logging.Enums.LogCategory.NETWORK, Logging.Enums.LogLevel.WARNING, "Unhandled OpCode : " + packet.Command + " (" + packet.Content.Length + ")");
                    Ignored.Add(packet.Command);
                }
                return false;
            }
            foreach (PacketsHandlerItem item in Handlers[packet.Command])
            {
                dynamic obj = Activator.CreateInstance(item.Type);
                obj.Load(packet);
                item.Handler(obj);
            }
            return true;
        }

        internal void RegisterHandler<U>(T command, Func<U, bool> handler)
        {
            PacketsHandlerItem item = new PacketsHandlerItem()
            {
                Handler = handler,
                Type = typeof(U)
            };
            if (Handlers.ContainsKey(command))
                Handlers[command].Add(item);
            else
                Handlers.Add(command, new List<PacketsHandlerItem> { item });
        }

        #endregion Internal Methods
    }
}