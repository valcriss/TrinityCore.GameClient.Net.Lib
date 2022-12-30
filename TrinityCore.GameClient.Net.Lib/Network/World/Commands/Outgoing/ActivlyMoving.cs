using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Commands.Outgoing
{
    internal class ActivlyMoving : WorldSendablePacket
    {
        #region Internal Constructors

        internal ActivlyMoving(ulong guid) : base(WorldCommand.CMSG_SET_ACTIVE_MOVER)
        {
            Append(guid);
        }

        #endregion Internal Constructors
    }
}