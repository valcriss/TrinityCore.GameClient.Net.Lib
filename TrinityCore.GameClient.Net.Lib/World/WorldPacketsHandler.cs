using System;
using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Log;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.Tools;
using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.World
{
    public class WorldPacketsHandler : PacketsHandler
    {
        private Dictionary<WorldCommand, WorldCommand> CompressedCommands { get; }
        private List<WorldCommand> UnhandledCommands { get; }

        public WorldPacketsHandler()
        {
            CompressedCommands = new Dictionary<WorldCommand, WorldCommand>
            {
                {WorldCommand.SMSG_COMPRESSED_UPDATE_OBJECT, WorldCommand.SMSG_UPDATE_OBJECT}
            };
            UnhandledCommands = new List<WorldCommand>();
        }

        public void Handle(WorldReceivablePacket worldReceivablePacket)
        {
            try
            {
                if (CompressedCommands.ContainsKey((WorldCommand)worldReceivablePacket.Command))
                {
                    byte[] decompressed = worldReceivablePacket.Content.Decompress();
                    if (decompressed == null) return;
                    WorldCommand decompressedCommand = CompressedCommands[(WorldCommand)worldReceivablePacket.Command];

                    worldReceivablePacket = new WorldReceivablePacket(decompressedCommand, decompressed);
                }

                bool handled = base.Handle(worldReceivablePacket);
                if (!handled && !UnhandledCommands.Contains((WorldCommand)worldReceivablePacket.Command))
                {
                    UnhandledCommands.Add((WorldCommand)worldReceivablePacket.Command);
                    Logger.Log("Unhandled OpCode : " + worldReceivablePacket, LogLevel.WARNING);
                }
            }
            catch (Exception e)
            {
                Logger.LogException(e);
            }


        }

        public void RegisterHandler(WorldCommand command, PacketHandler handler)
        {
            RegisterHandler((uint)command, handler);
        }
    }
}
