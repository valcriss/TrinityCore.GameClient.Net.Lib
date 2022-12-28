using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TrinityCore.GameClient.Net.Lib.Map.MmapTile;
using TrinityCore.GameClient.Net.Lib.Map.Tools;

namespace TrinityCore.GameClient.Net.Lib.Map
{
    public class MmapTileFileCollection
    {
        #region Public Properties

        public MmapTileFile EndTile { get; set; }
        public List<MmapTileFile> MmapTileFiles { get; set; }
        public MmapTileFile StartTile { get; set; }

        #endregion Public Properties

        #region Private Constructors

        private MmapTileFileCollection()
        {
            MmapTileFiles = new List<MmapTileFile>();
        }

        #endregion Private Constructors

        #region Public Methods

        public static MmapTileFileCollection Factory(MmapFilesCollection collection, int mapId, Vector3 start, Vector3 end)
        {
            MmapFile mmap = collection.GetMap(mapId);
            MmapTileFile startTile = mmap.GetMmapTileFileFromVector3(start);
            if (startTile == null) return null;
            MmapTileFile endTile = mmap.GetMmapTileFileFromVector3(end);
            if (endTile == null) return null;

            MmapTileFileCollection tmp = new MmapTileFileCollection() { StartTile = startTile, EndTile = endTile };

            tmp.MmapTileFiles.Add(startTile);

            if (startTile.Key == endTile.Key)
            {
                return tmp;
            }

            int stepX = (startTile.TileX < endTile.TileX) ? 1 : -1;
            int stepY = (startTile.TileY < endTile.TileY) ? 1 : -1;

            for (int x = startTile.TileX; x != endTile.TileX; x += stepX)
            {
                for (int y = startTile.TileY; y != endTile.TileY; y += stepY)
                {
                    MmapTileFile tile = mmap.GetMmapTileFileFromCoords(x, y);
                    if (tile != null)
                    {
                        if (tmp.MmapTileFiles.Any(c => c.Key == tile.Key)) continue;
                        tmp.MmapTileFiles.Add(tile);
                    }
                }
            }

            if (tmp.MmapTileFiles.All(c => c.Key != endTile.Key))
            {
                tmp.MmapTileFiles.Add(endTile);
            }

            return tmp;
        }

        #endregion Public Methods

        #region Internal Methods

        internal List<MmapMeshPoly> GetLinkedPolys(MmapMeshPoly lastMeshPoly)
        {
            List<MmapMeshPoly> linked = lastMeshPoly.GetNeighbors();
            foreach (MmapTileFile mmapTileFile in MmapTileFiles.Where(c => c.Key != lastMeshPoly.MmapMesh.MmapTileFile.Key))
            {
                MmapMeshPoly nearest = mmapTileFile.GetNearestPoly(lastMeshPoly.Center().ToWorldFormat());
                if (nearest != null && nearest.ShareAPoint(lastMeshPoly))
                {
                    linked.Add(nearest);
                }
            }
            return linked;
        }

        #endregion Internal Methods
    }
}