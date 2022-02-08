namespace TrinityCore.GameClient.Net.Lib.Network.Core
{
    internal abstract class SendablePacket : Packet
    {
        #region Internal Constructors

        private protected SendablePacket() : base(new byte[0])
        {
        }

        #endregion Internal Constructors

        #region Internal Methods

        internal abstract byte[] GetData();

        #endregion Internal Methods
    }
}