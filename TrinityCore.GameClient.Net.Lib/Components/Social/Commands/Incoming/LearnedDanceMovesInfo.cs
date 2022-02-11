using TrinityCore.GameClient.Net.Lib.Logging;
using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.Components.Social.Commands.Incoming
{
    internal class LearnedDanceMovesInfo : ReceivablePacket<Network.World.Enums.WorldCommand>
    {
        #region Internal Methods

        internal override void LoadData()
        {
            uint v1 = ReadUInt32();
            uint v2 = ReadUInt32();

            if (v1 + v2 != 0)
            {
                Logger.Append(Logging.Enums.LogCategory.SOCIAL, Logging.Enums.LogLevel.ERROR, "LearnedDanceMoves sum should be 0 according to trinity code ???");
            }
        }

        #endregion Internal Methods
    }
}