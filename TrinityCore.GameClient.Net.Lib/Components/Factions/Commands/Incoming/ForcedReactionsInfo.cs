using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Components.Social.Commands.Models;
using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.Components.Factions.Commands.Incoming
{
    internal class ForcedReactionsInfo : ReceivablePacket<Network.World.Enums.WorldCommand>
    {
        #region Internal Properties

        internal List<Reputation> ForcedReactions { get; set; }

        #endregion Internal Properties

        #region Internal Methods

        internal override void LoadData()
        {
            ForcedReactions = new List<Reputation>();
            uint count = ReadUInt32();
            for (uint i = 0; i < count; i++)
            {
                uint id = ReadUInt32();
                uint standing = ReadUInt32();
                ForcedReactions.Add(new Reputation() { Id = id, Standing = standing });
            }
        }

        #endregion Internal Methods
    }
}