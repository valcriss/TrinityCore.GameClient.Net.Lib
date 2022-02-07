using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Commands.Incoming
{
    internal class LoginCharacterResponse : ReceivablePacket<WorldCommand>
    {
        #region Internal Properties

        internal uint MapId { get; set; }
        internal float O { get; set; }
        internal float X { get; set; }
        internal float Y { get; set; }
        internal float Z { get; set; }

        #endregion Internal Properties

        #region Internal Constructors

        internal LoginCharacterResponse(ReceivablePacket<WorldCommand> receivablePacket) : base(receivablePacket)
        {
            MapId = ReadUInt32();
            X = ReadSingle();
            Y = ReadSingle();
            Z = ReadSingle();
            O = ReadSingle();
        }

        #endregion Internal Constructors
    }
}