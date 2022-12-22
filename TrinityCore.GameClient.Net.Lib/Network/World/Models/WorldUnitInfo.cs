using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Models;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Commands.Incoming;
using TrinityCore.GameClient.Net.Lib.Network.World.Commands.Outgoing;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Models
{
    internal class WorldUnitInfo
    {
        internal Dictionary<ulong, UnitInfo> WorldUnitInfos { get; set; }
        private ManualResetEvent UnitQueryDone { get; }
        private ulong QueryGuid { get; set; }
        private WorldClient WorldClient { get; }

        internal WorldUnitInfo(WorldClient worldClient)
        {
            WorldUnitInfos = new Dictionary<ulong, UnitInfo>();
            WorldClient = worldClient;
            WorldClient.PacketsHandler.RegisterHandler<CreatureQueryResponse>(WorldCommand.SMSG_CREATURE_QUERY_RESPONSE, CreatureQueryResponse);
            UnitQueryDone = new ManualResetEvent(false);
        }

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
    }
}
