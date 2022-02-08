namespace TrinityCore.GameClient.Net.Lib.Network.Core
{
    internal abstract class SendablePacket : Packet
    {
        #region Protected Constructors

        protected SendablePacket() : base(new byte[0])
        {
        }

        #endregion Protected Constructors

        #region Internal Methods

        internal abstract byte[] GetData();

        #endregion Internal Methods
    }
}