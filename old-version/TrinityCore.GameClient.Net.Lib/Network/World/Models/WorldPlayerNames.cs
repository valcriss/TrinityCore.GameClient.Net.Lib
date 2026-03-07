using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Commands.Outgoing;
using TrinityCore.GameClient.Net.Lib.Network.World.Commands.Incoming;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Models
{
    internal class WorldPlayerNames
    {
        #region Internal Properties

        internal Dictionary<ulong, string> PlayerNames { get; set; }

        #endregion Internal Properties

        #region Private Properties

        private ManualResetEvent CharacterNameQueryDone { get; }
        private WorldClient WorldClient { get; }

        #endregion Private Properties

        #region Internal Constructors

        internal WorldPlayerNames(WorldClient worldClient)
        {
            PlayerNames = new Dictionary<ulong, string>();
            WorldClient = worldClient;
            WorldClient.PacketsHandler.RegisterHandler<NameQueryResponse>(WorldCommand.SMSG_NAME_QUERY_RESPONSE, NameQueryResponse);
            CharacterNameQueryDone = new ManualResetEvent(false);
        }

        #endregion Internal Constructors

        #region Internal Methods

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

        #endregion Internal Methods

        #region Private Methods

        private bool NameQueryResponse(NameQueryResponse response)
        {
            lock (PlayerNames)
            {
                if (!PlayerNames.ContainsKey(response.Guid)) PlayerNames.Add(response.Guid, response.Name);
            }

            CharacterNameQueryDone.Set();

            return true;
        }

        #endregion Private Methods
    }
}