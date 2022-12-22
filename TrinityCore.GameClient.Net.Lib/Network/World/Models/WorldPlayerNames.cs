using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Commands.Outgoing;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Commands.Incoming;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Models
{
    internal class WorldPlayerNames
    {
        internal Dictionary<ulong, string> PlayerNames { get; set; }
        private ManualResetEvent CharacterNameQueryDone { get; }
        private WorldClient WorldClient { get; }

        internal WorldPlayerNames(WorldClient worldClient)
        {
            PlayerNames = new Dictionary<ulong, string>();
            WorldClient = worldClient;
            WorldClient.PacketsHandler.RegisterHandler<NameQueryResponse>(WorldCommand.SMSG_NAME_QUERY_RESPONSE, NameQueryResponse);
            CharacterNameQueryDone = new ManualResetEvent(false);
        }

        internal async Task<string> GetPlayerName(ulong guid)
        {
            return await Task.Run(() =>
            {
                lock (PlayerNames)
                {
                    if (PlayerNames.ContainsKey(guid)) return PlayerNames[guid];
                }

                CharacterNameQueryDone.Reset();
                WorldClient.Send(new NameQueryRequest(guid));
                CharacterNameQueryDone.WaitOne(10000);
                lock (PlayerNames)
                {
                    if (PlayerNames.ContainsKey(guid)) return PlayerNames[guid];
                }

                return null;
            });
        }

        private bool NameQueryResponse(NameQueryResponse response)
        {
            lock (PlayerNames)
            {
                if (!PlayerNames.ContainsKey(response.Guid)) PlayerNames.Add(response.Guid, response.Name);
            }

            CharacterNameQueryDone.Set();

            return true;
        }
    }
}
