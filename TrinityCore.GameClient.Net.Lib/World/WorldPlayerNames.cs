using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World.Commands;
using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.World
{
    public class WorldPlayerNames
    {
        public Dictionary<ulong, string> PlayerNames { get; set; }
        private ManualResetEvent CharacterNameQueryDone { get; }
        private WorldClient WorldClient { get; }

        public WorldPlayerNames(WorldClient worldClient)
        {
            PlayerNames = new Dictionary<ulong, string>();
            WorldClient = worldClient;
            WorldClient.AppendHandler(WorldCommand.SMSG_NAME_QUERY_RESPONSE, NameQueryResponse);
            CharacterNameQueryDone = new ManualResetEvent(false);
        }

        public async Task<string> GetPlayerName(ulong guid)
        {
            return await Task.Run(() =>
            {
                lock (PlayerNames)
                {
                    if (PlayerNames.ContainsKey(guid)) return PlayerNames[guid];
                }

                CharacterNameQueryDone.Reset();
                WorldClient.Send(new NameQueryRequest(WorldClient, guid));
                CharacterNameQueryDone.WaitOne(10000);
                lock (PlayerNames)
                {
                    if (PlayerNames.ContainsKey(guid)) return PlayerNames[guid];
                }

                return null;
            });
        }

        private void NameQueryResponse(ReceivablePacket content)
        {
            NameQueryResponse response = new NameQueryResponse(content);
            lock (PlayerNames)
            {
                if (!PlayerNames.ContainsKey(response.Guid)) PlayerNames.Add(response.Guid, response.Name);
            }

            CharacterNameQueryDone.Set();
        }
    }
}
