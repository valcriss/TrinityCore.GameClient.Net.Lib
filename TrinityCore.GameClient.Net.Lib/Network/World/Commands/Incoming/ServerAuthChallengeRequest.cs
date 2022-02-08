﻿using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Commands.Incoming
{
    internal class ServerAuthChallengeRequest : ReceivablePacket<WorldCommand>
    {
        #region Internal Properties

        internal uint ServerSeed { get; set; }

        #endregion Internal Properties

        #region Internal Constructors

        internal ServerAuthChallengeRequest(ReceivablePacket<WorldCommand> receivablePacket) : base(receivablePacket)
        {
            ReadUInt32();
            ServerSeed = ReadUInt32();
            ReadBytes(16);
            ReadBytes(16);
        }

        #endregion Internal Constructors
    }
}