using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Commands.Outgoing
{
    internal class CreatureQueryRequest : WorldSendablePacket
    {
        #region Internal Constructors

        internal CreatureQueryRequest(uint creatureId, ulong guid) : base(WorldCommand.CMSG_CREATURE_QUERY)
        {
            Append(creatureId);
            Append(guid);
        }

        #endregion Internal Constructors
    }
}