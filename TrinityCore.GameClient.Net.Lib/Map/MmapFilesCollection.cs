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

        public PathFinding PathFinding { get; set; }

        #endregion Public Properties

        #region Public Constructors

        public MmapFilesCollection()
        {
            MmapFiles = new List<MmapFile>();
            PathFinding = new PathFinding(this);
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

        public float? GetHeightAtPosition(int mapId, Vector3 position)
        {
            MmapFile mmap = GetMap(mapId);
            if (mmap == null) return null;
            MmapTileFile tile = mmap.GetMmapTileFileFromVector3(position);
            if (tile == null) return null;
            return tile.GetHeightAtPosition(position);
        }

        public MmapFile GetMap(int mapId)
        {
            return MmapFiles.FirstOrDefault(c => c.MapId == mapId);
        }

        #endregion Public Methods
    }
}