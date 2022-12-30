using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Enums;
using TrinityCore.GameClient.Net.Lib.Components.Social.Models;
using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.Components.Factions.Commands.Incoming
{
    internal class InitializeFactionsInfo : ReceivablePacket<Network.World.Enums.WorldCommand>
    {
        #region Internal Properties

        internal List<Reputation> Reputations { get; set; }

        #endregion Internal Properties

        #region Internal Methods

        internal override void LoadData()
        {
            Reputations = new List<Reputation>();
            uint count = ReadUInt32();
            for (uint i = 0; i < count; i++)
            {
                FactionOptions flags = (FactionOptions)ReadSByte();
                uint standing = ReadUInt32();
                Reputations.Add(new Reputation() { Id = i, Flags = flags, Standing = standing });
            }
        }

        #endregion Internal Methods
    }
}