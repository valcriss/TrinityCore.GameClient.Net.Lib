using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Commands
{
    internal class FeatureSystemStatus : WorldReceivablePacket
    {
        internal bool VoiceChatEnabled { get; set; }

        internal FeatureSystemStatus(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            ReadSByte();
            VoiceChatEnabled = ReadSByte() != 0;
        }
    }
}
