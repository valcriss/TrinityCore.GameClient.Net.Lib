using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using TrinityCore.GameClient.Net.Navigation.MmapEngine.Exceptions;

namespace TrinityCore.GameClient.Net.Navigation.MmapEngine
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
            if (mmap == null)
            {
                NavTrace.WriteLine($"NAV MMAPS PROJECT map={mapId} no mmap for pos=({position.X:F3},{position.Y:F3},{position.Z:F3})");
                return position;
            }

            MmapTileFile tile = mmap.GetMmapTileFileFromVector3(position);
            if (tile == null)
            {
                NavTrace.WriteLine($"NAV MMAPS PROJECT map={mapId} no tile for pos=({position.X:F3},{position.Y:F3},{position.Z:F3})");
                return position;
            }

            NavTrace.WriteLine($"NAV MMAPS PROJECT map={mapId} tile={tile.Key} pos=({position.X:F3},{position.Y:F3},{position.Z:F3})");
            return tile.ClosestPointAtPosition(position);
        }

        public MmapFile GetMap(int mapId)
        {
            return MmapFiles.FirstOrDefault(c => c.MapId == mapId);
        }

        #endregion Public Methods
    }
}


