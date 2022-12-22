using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Models;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Models
{
    internal class Query
    {
        #region Private Properties

        private WorldPlayerNames PlayerNames { get; }
        private WorldUnitInfo UnitInfo { get; }

        #endregion Private Properties

        #region Internal Constructors

        internal Query(WorldClient client)
        {
            PlayerNames = new WorldPlayerNames(client);
            UnitInfo = new WorldUnitInfo(client);
        }

        #endregion Internal Constructors

        #region Internal Methods

        internal async Task<string> GetPlayerName(ulong guid)
        {
            return await PlayerNames.GetPlayerName(guid);
        }

        internal async Task<UnitInfo> GetUnitInfo(uint entryId, ulong guid)
        {
            return await UnitInfo.GetUnitInfo(entryId, guid);
        }

        #endregion Internal Methods
    }
}