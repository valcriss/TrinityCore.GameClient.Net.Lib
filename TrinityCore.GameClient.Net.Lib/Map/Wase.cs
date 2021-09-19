using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using TrinityCore.Map.Net.IO;

namespace TrinityCore.GameClient.Net.Lib.Map
{
    public class Wase
    {

        private static Wase _instance;
        public static Wase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Wase();
                }
                return _instance;
            }
        }

        private string MmapDirectory { get; set; }
        private MMapTileReader TileReader { get; set; }

        public Wase()
        {

        }

        public bool Initialize(string mmapDirectory)
        {
            if (!System.IO.Directory.Exists(MmapDirectory) || !System.IO.File.Exists(System.IO.Path.Combine(MmapDirectory, "000.mmap")))
            {
                return false;
            }

            MmapDirectory = mmapDirectory;
            TileReader = new MMapTileReader(MmapDirectory);

            return true;
        }

        public Vector3 GetNearestPolyCenter(int mapId, Vector3 position)
        {
            MmapTileFile tile = TileReader.GetTile(mapId, position);
            
        }
    }
}
