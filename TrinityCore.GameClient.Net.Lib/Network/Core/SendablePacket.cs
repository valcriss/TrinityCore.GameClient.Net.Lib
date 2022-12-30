namespace TrinityCore.GameClient.Net.Lib.Network.Core
{
    internal abstract class SendablePacket : Packet
    {
        #region Protected Constructors

        protected SendablePacket() : base(System.Array.Empty<byte>())
        {
        }

        #endregion Protected Constructors

        #region Internal Methods

        internal abstract byte[] GetData();

        #endregion Internal Methods
    }
}