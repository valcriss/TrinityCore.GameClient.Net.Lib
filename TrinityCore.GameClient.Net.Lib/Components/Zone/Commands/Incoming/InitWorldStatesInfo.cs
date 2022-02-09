﻿using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Components.Zone.Models;
using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.Components.Zone.Commands.Incoming
{
    internal class InitWorldStatesInfo : ReceivablePacket<Network.World.Enums.WorldCommand>
    {
        internal WorldState WorldState { get; set; }
        internal override void LoadData()
        {
            WorldState = new WorldState();
            WorldState.MapId = ReadInt32();
            WorldState.ZoneId = ReadInt32();
            WorldState.AreaId = ReadInt32();

            ushort count = ReadUInt16();

            for (int i = 0; i < count; i++)
            {
                uint id = ReadUInt32();
                uint value = ReadUInt32();
                WorldState.Variables.Add(id, value);
            }
        }
    }
}
