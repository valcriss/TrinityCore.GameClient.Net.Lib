using System;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Models
{
    internal class WorldPacketHeader
    {
        #region Internal Properties

        internal WorldCommand Command { get; }
        internal int InputDataLength { get; }
        internal int Size { get; }

        #endregion Internal Properties

        #region Internal Constructors

        internal WorldPacketHeader(byte[] data, int dataLength)
        {
            InputDataLength = dataLength;
            switch (InputDataLength)
            {
                case 4:
                    Size = (int)(((uint)data[0] << 8) | data[1]);
                    Command = (WorldCommand)BitConverter.ToUInt16(data, 2);
                    break;

                case 5:
                    Size = (int)((((uint)data[0] & 0x7F) << 16) | ((uint)data[1] << 8) | data[2]);
                    Command = (WorldCommand)BitConverter.ToUInt16(data, 3);
                    break;

                default:
                    return;
            }

            Size -= 2;
        }

        #endregion Internal Constructors

        #region Public Methods

        public override string ToString()
        {
            return $"Command {Command} Header Size {InputDataLength} Packet Size {Size}";
        }

        #endregion Public Methods
    }
}