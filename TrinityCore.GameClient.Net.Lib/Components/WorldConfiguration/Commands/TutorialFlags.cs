using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Commands
{
    public class TutorialFlags : WorldReceivablePacket
    {
        private const int MAX_ACCOUNT_TUTORIAL_VALUES = 8;
        public uint[] TutorialsFlags { get; set; }

        public TutorialFlags(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            TutorialsFlags = new uint[MAX_ACCOUNT_TUTORIAL_VALUES];
            for (int i = 0; i < MAX_ACCOUNT_TUTORIAL_VALUES; i++) TutorialsFlags[i] = ReadUInt32();
        }
    }
}
