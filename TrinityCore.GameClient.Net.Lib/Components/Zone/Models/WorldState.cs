using System;
using System.Collections.Generic;
using System.Text;

namespace TrinityCore.GameClient.Net.Lib.Components.Zone.Models
{
    public class WorldState
    {
        public int MapId { get; set; }
        public int ZoneId { get; set; }
        public int AreaId { get; set; }
        public Dictionary<uint, uint> Variables { get; set; }

        public WorldState()
        {
            Variables = new Dictionary<uint, uint>();
        }

        public void UpdateVariable(uint id, uint value)
        {
            lock (Variables)
            {
                if (!Variables.ContainsKey(id))
                {
                    Variables.Add(id, value);
                }
                else
                {
                    Variables[id] = value;
                }
            }
        }

        public override string ToString()
        {
            string result =  $"MapId : {MapId}, ZoneId : {ZoneId}, AreaId : {AreaId}, ";
            foreach(KeyValuePair<uint,uint> item in Variables)
            {
                result += $"{item.Key} -> {item.Value}, ";
            }
            return result.Substring(0, result.Length - 2);
        }
    }
}
