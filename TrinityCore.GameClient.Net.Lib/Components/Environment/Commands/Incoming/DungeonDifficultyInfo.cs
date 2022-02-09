using TrinityCore.GameClient.Net.Lib.Components.Environment.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Environment.Commands.Incoming
{
    internal class DungeonDifficultyInfo : ReceivablePacket<WorldCommand>
    {
        #region Internal Properties

        internal Difficulty Difficulty { get; set; }
        internal bool IsInGroup { get; set; }

        #endregion Internal Properties

        #region Internal Methods

        internal override void LoadData()
        {
            Difficulty = (Difficulty)ReadUInt32();
            ReadUInt32();
            IsInGroup = ReadUInt32() != 0;
        }

        #endregion Internal Methods
    }
}