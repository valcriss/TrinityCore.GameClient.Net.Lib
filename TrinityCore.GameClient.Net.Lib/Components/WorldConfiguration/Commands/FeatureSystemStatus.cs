using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Commands
{
    public class FeatureSystemStatus : WorldReceivablePacket
    {
        public bool VoiceChatEnabled { get; set; }

        public FeatureSystemStatus(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            ReadSByte();
            VoiceChatEnabled = ReadSByte() != 0;
        }
    }
}
