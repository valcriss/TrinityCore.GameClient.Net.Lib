using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Models;
using TrinityCore.GameClient.Net.Lib.Network.World.Commands.Incoming;
using TrinityCore.GameClient.Net.Lib.Network.World.Commands.Outgoing;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Models
{
    internal class WorldUnitInfo
    {
        #region Internal Properties

        internal Dictionary<ulong, UnitInfo> WorldUnitInfos { get; set; }

        #endregion Internal Properties

        #region Private Properties

        private ulong QueryGuid { get; set; }
        private ManualResetEvent UnitQueryDone { get; }
        private WorldClient WorldClient { get; }

        #endregion Private Properties

        #region Internal Constructors

        internal WorldUnitInfo(WorldClient worldClient)
        {
            WorldUnitInfos = new Dictionary<ulong, UnitInfo>();
            WorldClient = worldClient;
            WorldClient.PacketsHandler.RegisterHandler<CreatureQueryResponse>(WorldCommand.SMSG_CREATURE_QUERY_RESPONSE, CreatureQueryResponse);
            UnitQueryDone = new ManualResetEvent(false);
        }

        #endregion Internal Constructors

        #region Internal Methods

        internal async Task<UnitInfo> GetUnitInfo(uint creatureId, ulong guid)
        {
            return await Task.Run(() =>
            {
                lock (WorldUnitInfos)
                {
                    if (WorldUnitInfos.ContainsKey(guid)) return WorldUnitInfos[guid];
                }

                UnitQueryDone.Reset();
                QueryGuid = guid;
                WorldClient.Send(new CreatureQueryRequest(creatureId, guid));
                UnitQueryDone.WaitOne(5000);
                lock (WorldUnitInfos)
                {
                    if (WorldUnitInfos.ContainsKey(guid)) return WorldUnitInfos[guid];
                }

                return null;
            });
        }

        #endregion Internal Methods

        #region Private Methods

        private bool CreatureQueryResponse(CreatureQueryResponse creatureQueryResponse)
        {
            lock (WorldUnitInfos)
            {
                if (!WorldUnitInfos.ContainsKey(QueryGuid))
                    WorldUnitInfos.Add(QueryGuid, creatureQueryResponse.CreatureInfo);
            }

            UnitQueryDone.Set();

            return true;
        }

        #endregion Private Methods
    }
}