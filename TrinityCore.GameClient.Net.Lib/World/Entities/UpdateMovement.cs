﻿using System;
using System.Collections.Generic;
using System.Text;

namespace TrinityCore.GameClient.Net.Lib.World.Entities
{
    public class UpdateMovement
    {
        public ulong Guid { get; set; }
        public MovementInfo Movement { get; set; }
    }
}
