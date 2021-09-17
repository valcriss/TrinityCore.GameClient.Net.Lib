using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.Map.Net.IO;

namespace TrinityCore.GameClient.Net.Lib.Map
{
    public class MapReader
    {
        private Dictionary<string, StoredTile> StoredTile { get; set; }

        public MapReader()
        {
            StoredTile = new Dictionary<string, StoredTile>();
        }
    }

    public class StoredTile
    {
        public MmapTileFile MmapTileFile { get; set; }
        public DateTime LastUsage { get; set; }
    }
}
