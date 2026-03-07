using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Components.Player.Models;
using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands.Incoming
{
    internal class UnlearnedSpellsInfo : ReceivablePacket<Network.World.Enums.WorldCommand>
    {
        #region Public Properties

        public List<Spell> UnlearnedSpells { get; set; }

        #endregion Public Properties

        #region Internal Methods

        internal override void LoadData()
        {
            UnlearnedSpells = new List<Spell>();
            uint count = ReadUInt32();
            for (int i = 0; i < count; i++)
            {
                uint id = ReadUInt32();
                UnlearnedSpells.Add(new Spell() { Id = id });
            }
        }

        #endregion Internal Methods
    }
}