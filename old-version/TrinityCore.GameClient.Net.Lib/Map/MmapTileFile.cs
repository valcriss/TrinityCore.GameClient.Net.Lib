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

        public Vector3 ClosestPointAtPosition(Vector3 position)
        {
            MmapMeshPoly poly = GetNearestPoly(position);
            if (poly == null) return position;
            position = position.ToFileFormat();
            MmapMeshTriangle triangle = poly.Triangles.FirstOrDefault(triangle => triangle.PointInTriangle(position));
            if (triangle == null) return position;
            return ClosestPointAtPosition(position, triangle.Points[0], triangle.Points[1], triangle.Points[2]);
        }

        public MmapMeshPoly GetNearestPoly(Vector3 position)
        {
            position = position.ToFileFormat();
            MmapMeshPoly poly = Mesh.Polys.FirstOrDefault(c => c.Triangles.Any(d => d.PointInTriangle(position)));
            poly ??= Mesh.Polys.OrderBy(c => c.Triangles.Min(d => Vector3.Distance(position, d.Center()))).FirstOrDefault();
            return poly;
        }

        #endregion Public Methods

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

        private static Vector3 ClosestPointAtPosition(Vector3 position, Vector3 point1, Vector3 point2, Vector3 point3)
        {
            // Check if P in vertex region outside A
            Vector3 ab = Vector3.Subtract(point2, point1);
            Vector3 ac = Vector3.Subtract(point3, point1);
            Vector3 ap = Vector3.Subtract(position, point1);

            float d1 = Vector3.Dot(ab, ap);
            float d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0.0f && d2 <= 0.0f)
            {
                // barycentric coordinates (1,0,0)
                return point1.ToWorldFormat();
            }

            // Check if P in vertex region outside B
            Vector3 bp = Vector3.Subtract(position, point2);
            float d3 = Vector3.Dot(ab, bp);
            float d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0.0f && d4 <= d3)
            {
                // barycentric coordinates (0,1,0)
                return point2.ToWorldFormat();
            }

            // Check if P in edge region of AB, if so return projection of P onto AB
            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0.0f && d1 >= 0.0f && d3 <= 0.0f)
            {
                // barycentric coordinates (1-v,v,0)
                float v1 = d1 / (d1 - d3);

                return new Vector3(point1.X + v1 * ab.X, point1.Y + v1 * ab.Y, point1.Z + v1 * ab.Z).ToWorldFormat();
            }

            // Check if P in vertex region outside C
            Vector3 cp = Vector3.Subtract(position, point3);
            float d5 = Vector3.Dot(ab, cp);
            float d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0.0f && d5 <= d6)
            {
                // barycentric coordinates (0,0,1)
                return point3.ToWorldFormat();
            }

            // Check if P in edge region of AC, if so return projection of P onto AC
            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0.0f && d2 >= 0.0f && d6 <= 0.0f)
            {
                // barycentric coordinates (1-w,0,w)
                float w1 = d2 / (d2 - d6);
                return new Vector3(point1.X + w1 * ac.X, point1.Y + w1 * ac.Y, point1.Z + w1 * ac.Z).ToWorldFormat();
            }

            // Check if P in edge region of BC, if so return projection of P onto BC
            float va = d3 * d6 - d5 * d4;
            if (va <= 0.0f && (d4 - d3) >= 0.0f && (d5 - d6) >= 0.0f)
            {
                // barycentric coordinates (0,1-w,w)
                float w2 = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return new Vector3(point2.X + w2 * (point3.X - point2.X), point2.Y + w2 * (point3.Y - point2.Y), point2.Z + w2 * (point3.Z - point2.Z)).ToWorldFormat();
            }

            // P inside face region. Compute Q through its barycentric coordinates (u,v,w)
            float denom = 1.0f / (va + vb + vc);
            float v = vb * denom;
            float w = vc * denom;
            return new Vector3(point1.X + ab.X * v + ac.X * w, point1.Y + ab.Y * v + ac.Y * w, point1.Z + ab.Z * v + ac.Z * w).ToWorldFormat();
        }

        private static int GetMapIdFromFilename(string file)
        {
            try
            {
                return int.Parse(System.IO.Path.GetFileNameWithoutExtension(file)[..3]);
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
            _mmapMesh = MmapMesh.FromBinaryReader(this, reader);
            if (reader.BytesLeft > 0)
            {
                throw new FileBadLengthException(reader.BytesLeft, 0);
            }
            _loaded = true;
            System.Diagnostics.Debug.WriteLine("LoadOnDemand [" + System.IO.Path.GetFileName(File) + "] Duration :" + DateTime.Now.Subtract(start).TotalMilliseconds + " ms");
        }

        #endregion Private Methods
    }
}