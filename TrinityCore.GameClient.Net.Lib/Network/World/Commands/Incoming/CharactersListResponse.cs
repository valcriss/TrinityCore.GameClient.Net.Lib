using TrinityCore.GameClient.Net.Lib.Logging;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;
using TrinityCore.GameClient.Net.Lib.Network.World.Models;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Commands.Incoming
{
    internal class CharactersListResponse : ReceivablePacket<WorldCommand>
    {
        #region Internal Properties

        internal Character[] Characters { get; set; }

        #endregion Internal Properties

        #region Internal Methods

        internal override void LoadData()
        {
            byte count = ReadByte();
            if (count == 0)
            {
                Characters = new Character[0];
            }
            else
            {
                Characters = new Character[count];
                for (byte i = 0; i < count; ++i)
                {
                    Characters[i] = new Character(Buffer, ReadIndex);
                    Logger.Append(Logging.Enums.LogCategory.NETWORK, Logging.Enums.LogLevel.DEBUG, "Character : " + Characters[i].Name);
                    ReadIndex = Characters[i].ReadIndex;
                }
            }
            base.LoadData();
        }

        #endregion Internal Methods

    }
}