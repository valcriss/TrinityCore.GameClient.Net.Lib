using System.Collections.Generic;
using System.Text;

namespace TrinityCore.GameClient.Net.Lib.Components.Zone.Models
{
    public class WorldState
    {
        #region Public Properties

        public int AreaId { get; set; }
        public int MapId { get; set; }
        public Dictionary<uint, uint> Variables { get; set; }
        public int ZoneId { get; set; }

        #endregion Public Properties

        #region Public Constructors

        public WorldState()
        {
            Variables = new Dictionary<uint, uint>();
        }

        #endregion Public Constructors

        #region Public Methods

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.Append($"MapId : {MapId}, ZoneId : {ZoneId}, AreaId : {AreaId}, ");
            foreach (KeyValuePair<uint, uint> item in Variables)
            {
                result.Append($"{item.Key} -> {item.Value}, ");
            }
            return result.ToString().Substring(0, result.Length - 2);
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

        #endregion Public Methods
    }
}