using System;
using System.Linq;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.Tools;
using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.World
{
    internal class WorldSendablePacket : SendablePacket
    {
        private WorldCommand Command { get; }
        private WorldSocket WorldSocket { get; }

        internal WorldSendablePacket(WorldSocket worldSocket, WorldCommand command)
        {
            WorldSocket = worldSocket;
            Command = command;
            Reset();
        }

        internal byte[] EncryptedCommand()
        {
            byte[] encryptedCommand = BitConverter.GetBytes((uint)Command);
            WorldSocket.AuthenticationCrypto.Encrypt(encryptedCommand, 0, encryptedCommand.Length);

            return encryptedCommand;
        }

        internal byte[] EncryptedSize()
        {
            byte[] encryptedSize = BitConverter.GetBytes(Buffer.Length + 4).SubArray(0, 2);
            Array.Reverse(encryptedSize);
            WorldSocket.AuthenticationCrypto.Encrypt(encryptedSize, 0, 2);

            return encryptedSize;
        }

        internal override byte[] GetBuffer()
        {
            byte[] data = new byte[6 + Buffer.Length];
            byte[] size = EncryptedSize();
            byte[] command = EncryptedCommand();

            Array.Copy(size, 0, data, 0, 2);
            Array.Copy(command, 0, data, 2, 4);
            Array.Copy(Buffer.ToArray(), 0, data, 6, Buffer.Length);

            return data;
        }

        protected void Reset()
        {
            Buffer = new byte[0];
        }
    }
}
