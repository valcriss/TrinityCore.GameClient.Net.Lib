namespace TrinityCore.GameClient.Net.Lib.Network.Core
{
    internal class ReceivablePacket<T> : Packet
    {
        #region Internal Properties

        internal T Command { get; set; }
        internal byte[] Content => Buffer;

        #endregion Internal Properties

        #region Internal Constructors

        internal ReceivablePacket()
        {
        }

        internal ReceivablePacket(T command, byte[] content, int readIndex = 0)
        {
            Command = command;
            Buffer = content;
            ReadIndex = readIndex;
        }

        internal ReceivablePacket(ReceivablePacket<T> receivablePacket, int readIndex = 0)
        {
            Command = receivablePacket.Command;
            Buffer = receivablePacket.Content;
            ReadIndex = readIndex;
        }

        #endregion Internal Constructors

        #region Internal Methods

        internal void Load(ReceivablePacket<T> receivablePacket)
        {
            Command = receivablePacket.Command;
            Buffer = receivablePacket.Content;
            ReadIndex = 0;
            LoadData();
        }

        internal virtual void LoadData()
        {
        }

        #endregion Internal Methods
    }
}