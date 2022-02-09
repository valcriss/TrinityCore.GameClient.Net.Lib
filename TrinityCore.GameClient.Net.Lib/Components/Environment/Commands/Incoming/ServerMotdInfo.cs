using System.Text;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Environment.Commands.Incoming
{
    internal class ServerMotdInfo : ReceivablePacket<WorldCommand>
    {
        #region Internal Properties

        internal string Motd { get; set; }

        #endregion Internal Properties

        #region Internal Methods

        internal override void LoadData()
        {
            uint lines = ReadUInt32();
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < lines; i++) builder.Append(ReadCString() + (i != lines - 1 ? "\n" : null));
            Motd = builder.ToString();
        }

        #endregion Internal Methods
    }
}