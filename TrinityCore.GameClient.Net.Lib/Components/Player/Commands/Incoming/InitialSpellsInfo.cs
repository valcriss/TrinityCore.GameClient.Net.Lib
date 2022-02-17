using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Components.Player.Models;
using TrinityCore.GameClient.Net.Lib.Logging;
using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands.Incoming
{
    internal class InitialSpellsInfo : ReceivablePacket<Network.World.Enums.WorldCommand>
    {
        #region Public Properties

        public List<Spell> Spells { get; set; }

        #endregion Public Properties

        #region Internal Methods

        internal override void LoadData()
        {
            Spells = new List<Spell>();

            byte packetControl = ReadByte();
            if (packetControl != 0x00)
            {
                Logger.Append(Logging.Enums.LogCategory.PLAYER, Logging.Enums.LogLevel.ERROR, $"Received initial spells info packet control != 0x00 ({packetControl})");
                return;
            }
            ushort count = ReadUInt16();
            for (int i = 0; i < count; i++)
            {
                uint id = ReadUInt32();
                ushort spellControl = ReadUInt16();
                if (spellControl != 0x00)
                {
                    Logger.Append(Logging.Enums.LogCategory.PLAYER, Logging.Enums.LogLevel.ERROR, $"Received initial spells info spell control != 0x00 ({id} - {spellControl})");
                    continue;
                }
                Spells.Add(new Spell() { Id = id });
            }
        }

        #endregion Internal Methods
    }
}