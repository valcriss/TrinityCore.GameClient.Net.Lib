using System.Text;
using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.World.Entities
{
    internal class UnitInfo
    {
        internal CreatureEliteType Classification { get; set; }
        internal uint[] CreatureDisplayId { get; set; }
        internal CreatureFamily CreatureFamily { get; set; }
        internal uint CreatureId { get; set; }
        internal uint CreatureMovementInfoId { get; set; }
        internal CreatureType CreatureType { get; set; }
        internal string CursorName { get; set; }
        internal float EnergyMulti { get; set; }
        internal CreatureTypeFlags Flags { get; set; }
        internal float HpMulti { get; set; }
        internal bool Leader { get; set; }
        internal string Name { get; set; }
        internal string NameAlt { get; set; }
        internal uint[] ProxyCreatureId { get; set; }
        internal uint[] QuestItems { get; set; }

        internal UnitInfo()
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
