using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TrinityCore.GameClient.Net.Lib.Map.Tools;

namespace TrinityCore.GameClient.Net.Lib.Map.MmapTile
{
    public class MmapMeshPoly
    {
        #region Public Properties

        public byte AreaAndtype { get; set; }
        public uint FirstLink { get; set; }
        public ushort Flags { get; set; }
        public string Key => Center().X + "|" + Center().Y + "|" + Center().Z;
        public ushort[] Neis { get; set; }
        public MmapMeshTriangle[] Triangles { get; set; }
        public byte VertCount { get; set; }
        public ushort[] Verts { get; set; }

        #endregion Public Properties

        #region Public Fields

        public const uint DT_NULL_LINK = 0xffffffff;

        #endregion Public Fields

        #region Private Fields

        private const int DT_POLY_BITS = 31;
        private const int DT_SALT_BITS = 12;
        private const int DT_TILE_BITS = 21;
        private Vector3? _center;

        #endregion Private Fields

        #region Public Methods

        public static MmapMeshPoly LoadBinaryReader(MmapMesh mesh, BinaryReader reader)
        {
            MmapMeshPoly poly = new MmapMeshPoly();
            poly.FirstLink = reader.ReadUint();
            poly.Verts = reader.ReadUShorts(6);
            poly.Neis = reader.ReadUShorts(6);
            poly.Flags = reader.ReadUShort();
            poly.VertCount = reader.ReadByte();
            poly.AreaAndtype = reader.ReadByte();

            switch (poly.VertCount)
            {
                case 3:
                    poly.Triangles = new MmapMeshTriangle[1];
                    poly.Triangles[0] = new MmapMeshTriangle()
                    {
                        Vertices = new ushort[] { poly.Verts[0], poly.Verts[1], poly.Verts[2] },
                        Points = new Vector3[] { mesh.Verts[poly.Verts[0]].Vector3, mesh.Verts[poly.Verts[1]].Vector3, mesh.Verts[poly.Verts[2]].Vector3 }
                    };
                    break;

                case 4:
                    poly.Triangles = new MmapMeshTriangle[2];
                    poly.Triangles[0] = new MmapMeshTriangle()
                    {
                        Vertices = new ushort[] { poly.Verts[0], poly.Verts[1], poly.Verts[2] },
                        Points = new Vector3[] { mesh.Verts[poly.Verts[0]].Vector3, mesh.Verts[poly.Verts[1]].Vector3, mesh.Verts[poly.Verts[2]].Vector3 }
                    };
                    poly.Triangles[1] = new MmapMeshTriangle()
                    {
                        Vertices = new ushort[] { poly.Verts[0], poly.Verts[2], poly.Verts[3] },
                        Points = new Vector3[] { mesh.Verts[poly.Verts[0]].Vector3, mesh.Verts[poly.Verts[2]].Vector3, mesh.Verts[poly.Verts[3]].Vector3 }
                    };
                    break;

                case 5:
                    poly.Triangles = new MmapMeshTriangle[3];
                    poly.Triangles[0] = new MmapMeshTriangle()
                    {
                        Vertices = new ushort[] { poly.Verts[0], poly.Verts[1], poly.Verts[2] },
                        Points = new Vector3[] { mesh.Verts[poly.Verts[0]].Vector3, mesh.Verts[poly.Verts[1]].Vector3, mesh.Verts[poly.Verts[2]].Vector3 }
                    };
                    poly.Triangles[1] = new MmapMeshTriangle()
                    {
                        Vertices = new ushort[] { poly.Verts[0], poly.Verts[2], poly.Verts[3] },
                        Points = new Vector3[] { mesh.Verts[poly.Verts[0]].Vector3, mesh.Verts[poly.Verts[2]].Vector3, mesh.Verts[poly.Verts[3]].Vector3 }
                    };
                    poly.Triangles[2] = new MmapMeshTriangle()
                    {
                        Vertices = new ushort[] { poly.Verts[0], poly.Verts[3], poly.Verts[4] },
                        Points = new Vector3[] { mesh.Verts[poly.Verts[0]].Vector3, mesh.Verts[poly.Verts[3]].Vector3, mesh.Verts[poly.Verts[4]].Vector3 }
                    };
                    break;

                case 6:
                    poly.Triangles = new MmapMeshTriangle[4];
                    poly.Triangles[0] = new MmapMeshTriangle()
                    {
                        Vertices = new ushort[] { poly.Verts[0], poly.Verts[1], poly.Verts[2] },
                        Points = new Vector3[] { mesh.Verts[poly.Verts[0]].Vector3, mesh.Verts[poly.Verts[1]].Vector3, mesh.Verts[poly.Verts[2]].Vector3 }
                    };
                    poly.Triangles[1] = new MmapMeshTriangle()
                    {
                        Vertices = new ushort[] { poly.Verts[0], poly.Verts[2], poly.Verts[3] },
                        Points = new Vector3[] { mesh.Verts[poly.Verts[0]].Vector3, mesh.Verts[poly.Verts[2]].Vector3, mesh.Verts[poly.Verts[3]].Vector3 }
                    };
                    poly.Triangles[2] = new MmapMeshTriangle()
                    {
                        Vertices = new ushort[] { poly.Verts[0], poly.Verts[3], poly.Verts[4] },
                        Points = new Vector3[] { mesh.Verts[poly.Verts[0]].Vector3, mesh.Verts[poly.Verts[3]].Vector3, mesh.Verts[poly.Verts[4]].Vector3 }
                    };
                    poly.Triangles[3] = new MmapMeshTriangle()
                    {
                        Vertices = new ushort[] { poly.Verts[5], poly.Verts[0], poly.Verts[4] },
                        Points = new Vector3[] { mesh.Verts[poly.Verts[5]].Vector3, mesh.Verts[poly.Verts[0]].Vector3, mesh.Verts[poly.Verts[4]].Vector3 }
                    };
                    break;
            }

            return poly;
        }

        public Vector3 Center()
        {
            if (_center == null)
            {
                float x = Triangles.Sum(c => c.Center().X) / Triangles.Length;
                float y = Triangles.Sum(c => c.Center().Y) / Triangles.Length;
                float z = Triangles.Sum(c => c.Center().Z) / Triangles.Length;
                _center = new Vector3(x, y, z);
            }
            return _center.Value;
        }

        public List<MmapMeshPoly> GetNeighbors(MmapTileFile tile)
        {
            List<MmapMeshPoly> results = new List<MmapMeshPoly>();
            for (uint i = FirstLink; i != DT_NULL_LINK; i = tile.Mesh.Links[(int)i].Next)
            {
                long neighbourRef = tile.Mesh.Links[(int)i].Reference;
                uint polyIndex = GetPolyRefIndex(neighbourRef);
                MmapMeshPoly nextPoly = tile.Mesh.Polys[(int)polyIndex];
                results.Add(nextPoly);
            }
            return results;
        }

        #endregion Public Methods

        #region Private Methods

        private uint GetPolyRefIndex(long @ref)
        {
            uint saltMask = ((uint)1 << DT_SALT_BITS) - 1;
            uint tileMask = ((uint)1 << DT_TILE_BITS) - 1;
            uint polyMask = ((uint)1 << DT_POLY_BITS) - 1;
            uint salt = (uint)(@ref >> DT_POLY_BITS + DT_TILE_BITS & saltMask);
            uint it = (uint)(@ref >> DT_POLY_BITS & tileMask);
            uint ip = (uint)(@ref & polyMask);
            return ip;
        }

        #endregion Private Methods
    }
}