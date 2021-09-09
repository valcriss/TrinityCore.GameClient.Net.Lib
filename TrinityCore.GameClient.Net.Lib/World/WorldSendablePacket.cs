using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.Tools;
using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.World
{
    public class WorldSendablePacket : SendablePacket
    {
        private WorldCommand Command { get; }
        private WorldSocket WorldSocket { get; }

        public WorldSendablePacket(WorldSocket worldSocket, WorldCommand command)
        {
            WorldSocket = worldSocket;
            Command = command;
            Reset();
        }

        public byte[] EncryptedCommand()
        {
            byte[] encryptedCommand = BitConverter.GetBytes((uint)Command);
            WorldSocket.AuthenticationCrypto.Encrypt(encryptedCommand, 0, encryptedCommand.Length);

            return encryptedCommand;
        }

        public byte[] EncryptedSize()
        {
            byte[] encryptedSize = BitConverter.GetBytes(Buffer.Length + 4).SubArray(0, 2);
            Array.Reverse(encryptedSize);
            WorldSocket.AuthenticationCrypto.Encrypt(encryptedSize, 0, 2);

            return encryptedSize;
        }

        public override byte[] GetBuffer()
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
