using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World.Commands;
using TrinityCore.GameClient.Net.Lib.World.Entities;
using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.World
{
    public class WorldUnitInfo
    {
        public Dictionary<ulong, UnitInfo> WorldUnitInfos { get; set; }
        private ManualResetEvent UnitQueryDone { get; }
        private ulong QueryGuid { get; set; }
        private WorldClient WorldClient { get; }

        public WorldUnitInfo(WorldClient worldClient)
        {
            WorldUnitInfos = new Dictionary<ulong, UnitInfo>();
            WorldClient = worldClient;
            WorldClient.AppendHandler(WorldCommand.SMSG_CREATURE_QUERY_RESPONSE, CreatureQueryResponse);
            UnitQueryDone = new ManualResetEvent(false);
        }

        public async Task<UnitInfo> GetUnitInfo(uint creatureId, ulong guid)
        {
            return await Task.Run(() =>
            {
                lock (WorldUnitInfos)
                {
                    if (WorldUnitInfos.ContainsKey(guid)) return WorldUnitInfos[guid];
                }

                UnitQueryDone.Reset();
                QueryGuid = guid;
                WorldClient.Send(new CreatureQueryRequest(WorldClient, creatureId, guid));
                UnitQueryDone.WaitOne(5000);
                lock (WorldUnitInfos)
                {
                    if (WorldUnitInfos.ContainsKey(guid)) return WorldUnitInfos[guid];
                }

                return null;
            });
        }

        private void CreatureQueryResponse(ReceivablePacket content)
        {
            CreatureQueryResponse response = new CreatureQueryResponse(content);
            lock (WorldUnitInfos)
            {
                if (!WorldUnitInfos.ContainsKey(QueryGuid))
                    WorldUnitInfos.Add(QueryGuid, response.CreatureInfo);
            }

            UnitQueryDone.Set();
        }
    }
}
