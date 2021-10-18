using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.World.Entities;

namespace TrinityCore.GameClient.Net.Lib.World
{
    internal class Query
    {
        private WorldUnitInfo UnitInfo { get; }
        private WorldPlayerNames PlayerNames { get; }

        internal Query(WorldClient client)
        {
            PlayerNames = new WorldPlayerNames(client);
            UnitInfo = new WorldUnitInfo(client);
        }

        internal async Task<UnitInfo> GetUnitInfo(uint entryId, ulong guid)
        {
            return await UnitInfo.GetUnitInfo(entryId, guid);
        }

        internal async Task<string> GetPlayerName(ulong guid)
        {
            return await PlayerNames.GetPlayerName(guid);
        }
    }
}
