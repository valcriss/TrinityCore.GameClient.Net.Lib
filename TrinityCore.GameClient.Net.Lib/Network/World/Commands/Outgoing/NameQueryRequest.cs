using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Commands.Outgoing
{
    internal class NameQueryRequest : WorldSendablePacket
    {
        #region Internal Constructors

        internal NameQueryRequest(ulong guid) : base(WorldCommand.CMSG_NAME_QUERY)
        {
            Append(guid);
        }

        #endregion Internal Constructors
    }
}