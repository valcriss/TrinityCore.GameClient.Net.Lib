using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.World.Entities
{
    public class UnitInfo
    {
        public CreatureEliteType Classification { get; set; }
        public uint[] CreatureDisplayId { get; set; }
        public CreatureFamily CreatureFamily { get; set; }
        public uint CreatureId { get; set; }
        public uint CreatureMovementInfoId { get; set; }
        public CreatureType CreatureType { get; set; }
        public string CursorName { get; set; }
        public float EnergyMulti { get; set; }
        public CreatureTypeFlags Flags { get; set; }
        public float HpMulti { get; set; }
        public bool Leader { get; set; }
        public string Name { get; set; }
        public string NameAlt { get; set; }
        public uint[] ProxyCreatureId { get; set; }
        public uint[] QuestItems { get; set; }

        public UnitInfo()
        {
            ProxyCreatureId = new uint[2];
            CreatureDisplayId = new uint[4];
            QuestItems = new uint[6];
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("CreatureId     : " + CreatureId);
            builder.AppendLine("Name           : " + Name);
            if (!string.IsNullOrEmpty(NameAlt))
                builder.AppendLine("NameAlt        : " + NameAlt);
            builder.AppendLine("CursorName     : " + CursorName);
            builder.AppendLine("Flags          : " + Flags);
            builder.AppendLine("CreatureType   : " + CreatureType);
            builder.AppendLine("CreatureFamily : " + CreatureFamily);
            builder.AppendLine("Classification : " + Classification);
            return builder.ToString();
        }
    }
}
