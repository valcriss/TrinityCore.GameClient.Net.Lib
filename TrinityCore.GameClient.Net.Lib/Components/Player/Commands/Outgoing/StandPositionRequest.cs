using TrinityCore.GameClient.Net.Lib.Components.Entities.Enums;
using TrinityCore.GameClient.Net.Lib.Network.World;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands.Outgoing
{
    internal class StandPositionRequest : WorldSendablePacket
    {
        #region Internal Constructors

        internal StandPositionRequest(ulong guid, UnitStandStateType standType) : base(WorldCommand.CMSG_STANDSTATECHANGE)
        {
            Append((uint)standType);
        }

        #endregion Internal Constructors
    }
}