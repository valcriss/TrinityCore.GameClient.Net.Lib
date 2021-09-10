using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.World.Entities;

namespace TrinityCore.GameClient.Net.Lib.World
{
    public class Query
    {
        private WorldUnitInfo UnitInfo { get; }
        private WorldPlayerNames PlayerNames { get; }

        public Query(WorldClient client)
        {
            PlayerNames = new WorldPlayerNames(client);
            UnitInfo = new WorldUnitInfo(client);
        }

        public async Task<UnitInfo> GetUnitInfo(uint entryId, ulong guid)
        {
            return await UnitInfo.GetUnitInfo(entryId, guid);
        }

        public async Task<string> GetPlayerName(ulong guid)
        {
            return await PlayerNames.GetPlayerName(guid);
        }
    }
}
