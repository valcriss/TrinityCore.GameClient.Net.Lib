using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Commands
{
    internal class TutorialFlags : WorldReceivablePacket
    {
        private const int MAX_ACCOUNT_TUTORIAL_VALUES = 8;
        internal uint[] TutorialsFlags { get; set; }

        internal TutorialFlags(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            TutorialsFlags = new uint[MAX_ACCOUNT_TUTORIAL_VALUES];
            for (int i = 0; i < MAX_ACCOUNT_TUTORIAL_VALUES; i++) TutorialsFlags[i] = ReadUInt32();
        }
    }
}
