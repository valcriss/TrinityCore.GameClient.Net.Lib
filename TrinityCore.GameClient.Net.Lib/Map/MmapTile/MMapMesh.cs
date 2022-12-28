using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Map.Tools;

namespace TrinityCore.GameClient.Net.Lib.Map.MmapTile
{
    public class MmapMesh
    {
        #region Public Properties

        public MmapMeshHeader Header { get; set; }
        public List<MmapMeshLink> Links { get; set; }
        public MmapTileFile MmapTileFile { get; set; }
        public List<MmapMeshOffMeshConnection> OffMeshConnections { get; set; }
        public List<MmapMeshPolyDetail> PolyDetails { get; set; }
        public List<MmapMeshPoly> Polys { get; set; }
        public List<MmapMeshNode> Tree { get; set; }
        public List<MmapMeshTriDetail> TriDetails { get; set; }
        public List<MmapMeshVert> VertDetails { get; set; }
        public List<MmapMeshVert> Verts { get; set; }

        #endregion Public Properties

        #region Public Constructors

        public MmapMesh()
        {
            Verts = new List<MmapMeshVert>();
            Polys = new List<MmapMeshPoly>();
            Links = new List<MmapMeshLink>();
            PolyDetails = new List<MmapMeshPolyDetail>();
            VertDetails = new List<MmapMeshVert>();
            TriDetails = new List<MmapMeshTriDetail>();
            Tree = new List<MmapMeshNode>();
            OffMeshConnections = new List<MmapMeshOffMeshConnection>();
        }

        #endregion Public Constructors

        #region Public Methods

        public static MmapMesh FromBinaryReader(MmapTileFile mmapTileFile, BinaryReader reader)
        {
            MmapMesh mesh = new MmapMesh();
            mesh.MmapTileFile = mmapTileFile;
            mesh.Header = MmapMeshHeader.LoadFromBinaryReader(reader);

            for (int i = 0; i < mesh.Header.VertCount; i++)
            {
                mesh.Verts.Add(MmapMeshVert.LoadBinaryReader(reader));
            }
            for (int i = 0; i < mesh.Header.PolyCount; i++)
            {
                mesh.Polys.Add(MmapMeshPoly.LoadBinaryReader(mesh, reader));
            }
            for (int i = 0; i < mesh.Header.MaxLinkCount; i++)
            {
                mesh.Links.Add(MmapMeshLink.LoadBinaryReader(reader));
            }
            for (int i = 0; i < mesh.Header.DetailMeshCount; i++)
            {
                mesh.PolyDetails.Add(MmapMeshPolyDetail.LoadBinaryReader(reader));
            }
            for (int i = 0; i < mesh.Header.DetailVertCount; i++)
            {
                mesh.VertDetails.Add(MmapMeshVert.LoadBinaryReader(reader));
            }
            for (int i = 0; i < mesh.Header.DetailTriCount; i++)
            {
                mesh.TriDetails.Add(MmapMeshTriDetail.LoadBinaryReader(reader));
            }
            for (int i = 0; i < mesh.Header.NodeCount; i++)
            {
                mesh.Tree.Add(MmapMeshNode.LoadBinaryReader(reader));
            }
            for (int i = 0; i < mesh.Header.OffMeshConCount; i++)
            {
                mesh.OffMeshConnections.Add(MmapMeshOffMeshConnection.LoadBinaryReader(reader));
            }

            return mesh;
        }

        #endregion Public Methods
    }
}