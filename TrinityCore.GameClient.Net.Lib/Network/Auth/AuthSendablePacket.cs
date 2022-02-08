using System;
using TrinityCore.GameClient.Net.Lib.Network.Auth.Enums;
using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.Network.Auth
{
    internal class AuthSendablePacket : SendablePacket
    {
        #region Internal Properties

        internal AuthCommand Command { get; set; }

        #endregion Internal Properties

        #region Internal Constructors

        internal AuthSendablePacket(AuthCommand command)
        {
            Command = command;
        }

        #endregion Internal Constructors

        #region Internal Methods

        internal override byte[] GetData()
        {
            byte[] data = new byte[1 + Buffer.Length];
            data[0] = (byte)Command;
            Array.Copy(Buffer, 0, data, 1, Buffer.Length);
            return data;
        }

        #endregion Internal Methods
    }
}