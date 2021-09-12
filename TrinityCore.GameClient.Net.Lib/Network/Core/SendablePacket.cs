namespace TrinityCore.GameClient.Net.Lib.Network.Core
{
    internal abstract class SendablePacket : Packet
    {
        protected SendablePacket(byte[] content = null, int readIndex = 0) : base(content, readIndex)
        {
        }
    }
}
