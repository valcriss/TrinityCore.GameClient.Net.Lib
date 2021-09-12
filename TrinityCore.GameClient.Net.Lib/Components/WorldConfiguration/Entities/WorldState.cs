using System.Collections.Generic;

namespace TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Entities
{
    internal class WorldState
    {
        internal int AreaId { get; set; }
        internal int MapId { get; set; }
        internal Dictionary<uint, WorldStateVariable> Variables { get; set; }
        internal int ZoneId { get; set; }

        internal WorldState()
        {
            Variables = new Dictionary<uint, WorldStateVariable>();
        }

        internal void UpdateVariable(WorldStateVariable variable)
        {
            if (Variables.ContainsKey(variable.Id))
                Variables[variable.Id] = variable;
            else
                Variables.Add(variable.Id, variable);
        }
    }
}
