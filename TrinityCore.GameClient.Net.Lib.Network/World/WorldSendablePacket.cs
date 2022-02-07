using System;
using System.Linq;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.Security;
using TrinityCore.GameClient.Net.Lib.Network.Tools;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World
{
    internal class WorldSendablePacket : SendablePacket
    {
        #region Private Properties

        private WorldCommand Command { get; }

        #endregion Private Properties

        #region Internal Constructors

        internal WorldSendablePacket(WorldCommand command)
        {
            Command = command;
            Buffer = new byte[0];
        }

        #endregion Internal Constructors

        #region Internal Methods

        internal byte[] EncryptedCommand()
        {
            byte[] encryptedCommand = BitConverter.GetBytes((uint)Command);
            AuthenticationCrypto.Instance.Encrypt(encryptedCommand, 0, encryptedCommand.Length);

            return encryptedCommand;
        }

        internal byte[] EncryptedSize()
        {
            byte[] encryptedSize = BitConverter.GetBytes(Buffer.Length + 4).SubArray(0, 2);
            Array.Reverse(encryptedSize);
            AuthenticationCrypto.Instance.Encrypt(encryptedSize, 0, 2);

            return encryptedSize;
        }

        internal override byte[] GetData()
        {
            byte[] data = new byte[6 + Buffer.Length];
            byte[] size = EncryptedSize();
            byte[] command = EncryptedCommand();

            Array.Copy(size, 0, data, 0, 2);
            Array.Copy(command, 0, data, 2, 4);
            Array.Copy(Buffer.ToArray(), 0, data, 6, Buffer.Length);

            return data;
        }

        #endregion Internal Methods
    }
}