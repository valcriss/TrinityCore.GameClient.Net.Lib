using System;
using System.Collections.Generic;
using System.Linq;

namespace TrinityCore.GameClient.Net.Lib.Map
{
    public class MmapTileFileCache
    {
        #region Public Properties

        public static MmapTileFileCache Instance
        {
            get
            {
                _instance ??= new MmapTileFileCache();
                return _instance;
            }
        }

        #endregion Public Properties

        #region Private Properties

        private Dictionary<string, MmapTileFileCacheItem> Items { get; set; }

        #endregion Private Properties

        #region Private Fields

        private static MmapTileFileCache _instance;

        #endregion Private Fields

        #region Private Constructors

        private MmapTileFileCache()
        {
            Items = new Dictionary<string, MmapTileFileCacheItem>();
        }

        #endregion Private Constructors

        #region Public Methods

        public MmapTileFile Get(string key)
        {
            lock (Items)
            {
                if (Items.ContainsKey(key))
                {
                    Items[key].LastUsage = DateTime.Now;
                    return Items[key].Tile;
                }
                string[] removeKeys = Items.Where(c => DateTime.Now.Subtract(c.Value.LastUsage).TotalMinutes > 5).Select(c => c.Key).ToArray();
                foreach (string removeKey in removeKeys)
                {
                    Items.Remove(removeKey);
                }
            }
            return null;
        }

        public void Set(string key, MmapTileFile value)
        {
            lock (Items)
            {
                if (Items.ContainsKey(key))
                {
                    Items[key].LastUsage = DateTime.Now;
                    Items[key].Tile = value;
                }
                else
                {
                    Items.Add(key, new MmapTileFileCacheItem() { LastUsage = DateTime.Now, Tile = value });
                }
            }
        }

        #endregion Public Methods
    }

    public class MmapTileFileCacheItem
    {
        #region Public Properties

        public DateTime LastUsage { get; set; }
        public MmapTileFile Tile { get; set; }

        #endregion Public Properties
    }
}