using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TrinityCore.GameClient.Net.Lib.Map.Exceptions;

namespace TrinityCore.GameClient.Net.Lib.Map
{
    public class MmapFilesCollection
    {
        #region Public Properties

        public List<MmapFile> MmapFiles { get; set; }

        #endregion Public Properties

        #region Public Constructors

        public MmapFilesCollection()
        {
            MmapFiles = new List<MmapFile>();
            PathFinding.Initialize(this);
        }

        #endregion Public Constructors

        #region Public Methods

        public static MmapFilesCollection Load(string mmapsDirectory)
        {
            MmapFilesCollection collection = new MmapFilesCollection();

            if (!System.IO.Directory.Exists(mmapsDirectory) || !System.IO.File.Exists(System.IO.Path.Combine(mmapsDirectory, "000.mmap")))
            {
                throw new DirectoryInvalidException();
            }

            foreach (string file in System.IO.Directory.GetFiles(mmapsDirectory, "*." + Constants.MMAP_FILE_EXTENSION))
            {
                collection.MmapFiles.Add(MmapFile.Load(file));
            }

            return collection;
        }

        public Vector3 ClosestPointAtPosition(int mapId, Vector3 position)
        {
            MmapFile mmap = GetMap(mapId);
            if (mmap == null) return position;
            MmapTileFile tile = mmap.GetMmapTileFileFromVector3(position);
            if (tile == null) return position;
            return tile.ClosestPointAtPosition(position);
        }

        public MmapFile GetMap(int mapId)
        {
            return MmapFiles.FirstOrDefault(c => c.MapId == mapId);
        }

        #endregion Public Methods
    }
}