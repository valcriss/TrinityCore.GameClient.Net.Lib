using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TrinityCore.GameClient.Net.Lib.Log;
using TrinityCore.Map.Net.IO;

namespace TrinityCore.GameClient.Net.Lib.Map
{
    public class MMapTileReader
    {
        private const double STORE_TIMEOUT = 5 * 60 * 1000;
        private Dictionary<string, StoredTile> StoredTile { get; set; }
        private MmapFilesCollection Collection { get; set; }

        public MMapTileReader(string mmapDirectory)
        {
            StoredTile = new Dictionary<string, StoredTile>();
            Collection = MmapFilesCollection.Load(mmapDirectory);
        }

        public MmapTileFile GetTile(int mapId, Vector3 position)
        {
            MmapFile mmap = Collection.GetMap(mapId);
            if (mmap == null) return null;
            string key = mmap.GetMmapTileKeyFromVector3(position);
            return GetTile(mmap, key);
        }

        private MmapTileFile GetTile(MmapFile mmap, string key)
        {
            MmapTileFile mmapTile = GetTileFromStore(key);

            if (mmapTile == null)
            {
                mmapTile = GetTileFromFile(mmap, key);
                SaveToStore(mmapTile);
            }

            ClearStore();

            return mmapTile;
        }

        private void ClearStore()
        {
            lock (StoredTile)
            {
                string[] keys = StoredTile.Where(c => DateTime.Now.Subtract(c.Value.LastUsage).TotalMilliseconds > STORE_TIMEOUT).Select(c => c.Key).ToArray();
                foreach (string key in keys)
                {
                    StoredTile.Remove(key);
                    Logger.Log("Clearning stored mmaptile " + key + " from cache", LogLevel.INFO);
                }
            }
        }

        private MmapTileFile GetTileFromFile(MmapFile mmap, string key)
        {
            return mmap.GetMmapTileFileFromKey(key);
        }

        private void SaveToStore(MmapTileFile mmapTile)
        {
            if (mmapTile == null) return;
            lock (StoredTile)
            {
                if (!StoredTile.ContainsKey(mmapTile.Key))
                {
                    StoredTile.Add(mmapTile.Key, new StoredTile()
                    {
                        LastUsage = DateTime.Now,
                        MmapTileFile = mmapTile
                    });
                    Logger.Log("Saving " + mmapTile.Key + " in cache store", LogLevel.INFO);
                }
            }
        }

        private MmapTileFile GetTileFromStore(string key)
        {
            lock (StoredTile)
            {
                if (StoredTile.ContainsKey(key))
                {
                    StoredTile[key].LastUsage = DateTime.Now;
                    Logger.Log("Reading mmaptile " + key + " from cache store", LogLevel.INFO);
                    return StoredTile[key].MmapTileFile;
                }
            }
            return null;
        }
    }

    public class StoredTile
    {
        public MmapTileFile MmapTileFile { get; set; }
        public DateTime LastUsage { get; set; }
    }
}
