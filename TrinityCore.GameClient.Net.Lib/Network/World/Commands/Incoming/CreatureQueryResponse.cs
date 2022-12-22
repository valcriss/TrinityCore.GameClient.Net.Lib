using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Enums;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Models;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Commands.Incoming
{
    internal class CreatureQueryResponse : ReceivablePacket<WorldCommand>
    {
        internal UnitInfo CreatureInfo { get; set; }

        internal override void LoadData()
        {
            CreatureInfo = new UnitInfo();
            CreatureInfo.CreatureId = ReadUInt32();
            CreatureInfo.Name = ReadCString();
            CreatureInfo.NameAlt = ReadCString();
            CreatureInfo.CursorName = ReadCString();
            CreatureInfo.Flags = (CreatureTypeFlags)ReadUInt32();
            CreatureInfo.CreatureType = (CreatureType)ReadUInt32(); // CreatureType.dbc
            CreatureInfo.CreatureFamily = (CreatureFamily)ReadUInt32(); // CreatureFamily.dbc
            CreatureInfo.Classification = (CreatureEliteType)ReadUInt32(); // Creature Rank (elite, boss, etc)
            for (int i = 0; i < CreatureInfo.ProxyCreatureId.Length; i++)
                CreatureInfo.ProxyCreatureId[i] = ReadUInt32();
            for (int i = 0; i < CreatureInfo.CreatureDisplayId.Length; i++)
                CreatureInfo.CreatureDisplayId[i] = ReadUInt32();

            CreatureInfo.HpMulti = ReadSingle();
            CreatureInfo.EnergyMulti = ReadSingle();
            CreatureInfo.Leader = ReadSByte() != 0;

            for (int i = 0; i < CreatureInfo.QuestItems.Length; i++) CreatureInfo.QuestItems[i] = ReadUInt32();

            CreatureInfo.CreatureMovementInfoId = ReadUInt32();
        }
    }
}
