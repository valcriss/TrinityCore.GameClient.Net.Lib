using System;
using System.Linq;
using System.Numerics;
using TrinityCore.GameClient.Net.Lib.Map.Exceptions;
using TrinityCore.GameClient.Net.Lib.Map.MmapTile;
using TrinityCore.GameClient.Net.Lib.Map.Tools;

namespace TrinityCore.GameClient.Net.Lib.Map
{
    public class MmapTileFile
    {
        #region Public Properties

        public string File { get; set; }

        public MmapTileHeader Header
        {
            get
            {
                if (!_loaded)
                    LoadOnDemand();
                return _mmapTileHeader;
            }
        }

        public string Key { get; set; }
        public int MapId { get; set; }

        public MmapMesh Mesh
        {
            get
            {
                if (!_loaded)
                    LoadOnDemand();
                return _mmapMesh;
            }
        }

        public int TileX { get; set; }
        public int TileY { get; set; }

        #endregion Public Properties

        #region Private Fields

        private bool _loaded;

        private MmapMesh _mmapMesh;

        private MmapTileHeader _mmapTileHeader;

        #endregion Private Fields

        #region Private Constructors

        private MmapTileFile(string file, int mapId, int tileX, int tileY)
        {
            File = file;
            Key = System.IO.Path.GetFileNameWithoutExtension(file);
            MapId = mapId;
            TileX = tileX;
            TileY = tileY;
        }

        #endregion Private Constructors

        #region Public Methods

        public static MmapTileFile Load(string file)
        {
            CheckFile(file);

            int mapId = GetMapIdFromFilename(file);
            int tileX = GetTileXFromFilename(file);
            int tileY = GetTileYFromFilename(file);

            return new MmapTileFile(file, mapId, tileX, tileY);
        }

        public MmapMeshPoly GetNearestPoly(Vector3 position)
        {
            position = position.ToFileFormat();
            MmapMeshPoly poly = Mesh.Polys.FirstOrDefault(c => c.Triangles.Any(d => d.PointInTriangle(position)));
            if (poly == null)
            {
                poly = Mesh.Polys.OrderBy(c => c.Triangles.Min(d => Vector3.Distance(position, d.Center()))).FirstOrDefault();
            }
            return poly;
        }

        #endregion Public Methods

        #region Internal Methods

        internal float? GetHeightAtPosition(Vector3 position)
        {
            position = position.ToFileFormat();
            MmapMeshVert vert = Mesh.Verts.OrderBy(c => (c.Vector3 - position).Length()).FirstOrDefault();
            if (vert == null) return null;
            return vert.Y;
        }

        #endregion Internal Methods

        #region Private Methods

        private static void CheckFile(string file)
        {
            if (!file.Exists())
            {
                throw new System.IO.FileNotFoundException();
            }
            else if (!file.CheckExtension(Constants.MMAP_TILE_FILE_EXTENSION))
            {
                throw new FileBadExtensionException(Constants.MMAP_TILE_FILE_EXTENSION);
            }
            else if (!file.CheckRegExp(Constants.MMAP_TILE_FILE_CHECK_REGEXP))
            {
                throw new FileBadFilenamePatternException(Constants.MMAP_TILE_FILE_EXTENSION, Constants.MMAP_TILE_FILE_CHECK_REGEXP);
            }
        }

        private static int GetMapIdFromFilename(string file)
        {
            try
            {
                return int.Parse(System.IO.Path.GetFileNameWithoutExtension(file).Substring(0, 3));
            }
            catch (Exception)
            {
                throw new FileBadFilenamePatternException(Constants.MMAP_TILE_FILE_EXTENSION, Constants.MMAP_TILE_FILE_CHECK_REGEXP);
            }
        }

        private static int GetTileXFromFilename(string file)
        {
            try
            {
                return int.Parse(System.IO.Path.GetFileNameWithoutExtension(file).Substring(3, 2));
            }
            catch (Exception)
            {
                throw new FileBadFilenamePatternException(Constants.MMAP_TILE_FILE_EXTENSION, Constants.MMAP_TILE_FILE_CHECK_REGEXP);
            }
        }

        private static int GetTileYFromFilename(string file)
        {
            try
            {
                return int.Parse(System.IO.Path.GetFileNameWithoutExtension(file).Substring(5, 2));
            }
            catch (Exception)
            {
                throw new FileBadFilenamePatternException(Constants.MMAP_TILE_FILE_EXTENSION, Constants.MMAP_TILE_FILE_CHECK_REGEXP);
            }
        }

        private void LoadOnDemand()
        {
            DateTime start = DateTime.Now;
            BinaryReader reader = BinaryReader.FromFile(File);
            _mmapTileHeader = MmapTileHeader.FromBinaryReader(reader);
            _mmapMesh = MmapMesh.FromBinaryReader(reader);
            if (reader.BytesLeft > 0)
            {
                throw new Exception("Bytes left");
            }
            _loaded = true;
            System.Diagnostics.Debug.WriteLine("LoadOnDemand [" + System.IO.Path.GetFileName(File) + "] Duration :" + DateTime.Now.Subtract(start).TotalMilliseconds + " ms");
        }

        #endregion Private Methods
    }
}