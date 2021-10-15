using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Entities;

namespace TrinityCore.GameClient.Net.Lib.Display.Models
{
    public class MoveToEntityOrder : Order
    {
        private ulong EntityGuid { get; set; }
        public MoveToEntityOrder(Client client, ulong entityGuid) : base(client)
        {
            EntityGuid = entityGuid;
        }

        public override bool Process()
        {
            Creature creature = Components.Entities.EntitiesComponent.Instance.Collection.Creatures.Values.Where(c => c.Guid == EntityGuid).FirstOrDefault();
            if(creature != null)
            {
                Map.TravelState creatureResult = Client.MoveTo(creature);
                return creatureResult == Map.TravelState.ERROR || creatureResult == Map.TravelState.DESTINATION_REACH;
            }
            Player player = Components.Entities.EntitiesComponent.Instance.Collection.Players.Values.Where(c => c.Guid == EntityGuid).FirstOrDefault();
            if (player != null)
            {
                Map.TravelState playerResult = Client.MoveTo(player);
                return playerResult == Map.TravelState.ERROR || playerResult == Map.TravelState.DESTINATION_REACH;
            }
            return true;
        }

    }
}
