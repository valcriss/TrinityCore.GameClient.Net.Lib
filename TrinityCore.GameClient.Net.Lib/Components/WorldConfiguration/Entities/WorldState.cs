using System;
using System.Collections.Generic;
using System.Text;

namespace TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Entities
{
    public class WorldState
    {
        public int AreaId { get; set; }
        public int MapId { get; set; }
        public Dictionary<uint, WorldStateVariable> Variables { get; set; }
        public int ZoneId { get; set; }

        public WorldState()
        {
            Variables = new Dictionary<uint, WorldStateVariable>();
        }

        public void UpdateVariable(WorldStateVariable variable)
        {
            if (Variables.ContainsKey(variable.Id))
                Variables[variable.Id] = variable;
            else
                Variables.Add(variable.Id, variable);
        }
    }
}
