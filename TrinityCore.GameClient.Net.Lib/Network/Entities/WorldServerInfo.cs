using System;
using System.Collections.Generic;
using System.Text;

namespace TrinityCore.GameClient.Net.Lib.Network.Entities
{
    public class WorldServerInfo
    {
        public byte Locked;
        public string Address { get; set; }
        public ushort Build { get; set; }
        public byte Flags { get; set; }
        public uint Id { get; set; }
        public byte Load { get; set; }
        public string Name { get; set; }
        public float Population { get; set; }
        public int Port { get; set; }
        public byte Timezone { get; set; }
        public byte Type { get; set; }
        public byte VersionBugFix { get; set; }
        public byte VersionMajor { get; set; }
        public byte VersionMinor { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
