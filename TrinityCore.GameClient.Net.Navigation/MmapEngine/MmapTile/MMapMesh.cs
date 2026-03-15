using System.Collections.Generic;
using TrinityCore.GameClient.Net.Navigation.MmapEngine.Tools;

namespace TrinityCore.GameClient.Net.Navigation.MmapEngine.MmapTile
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

        public static MmapMesh FromMapBinaryReader(MmapTileFile mmapTileFile, MapBinaryReader reader)
        {
            MmapMesh mesh = new MmapMesh();
            mesh.MmapTileFile = mmapTileFile;
            mesh.Header = MmapMeshHeader.LoadFromMapBinaryReader(reader);

            for (int i = 0; i < mesh.Header.VertCount; i++)
            {
                mesh.Verts.Add(MmapMeshVert.LoadMapBinaryReader(reader));
            }
            for (int i = 0; i < mesh.Header.PolyCount; i++)
            {
                mesh.Polys.Add(MmapMeshPoly.LoadMapBinaryReader(mesh, reader));
            }
            for (int i = 0; i < mesh.Header.MaxLinkCount; i++)
            {
                mesh.Links.Add(MmapMeshLink.LoadMapBinaryReader(reader));
            }
            for (int i = 0; i < mesh.Header.DetailMeshCount; i++)
            {
                mesh.PolyDetails.Add(MmapMeshPolyDetail.LoadMapBinaryReader(reader));
            }
            for (int i = 0; i < mesh.Header.DetailVertCount; i++)
            {
                mesh.VertDetails.Add(MmapMeshVert.LoadMapBinaryReader(reader));
            }
            for (int i = 0; i < mesh.Header.DetailTriCount; i++)
            {
                mesh.TriDetails.Add(MmapMeshTriDetail.LoadMapBinaryReader(reader));
            }
            for (int i = 0; i < mesh.Header.NodeCount; i++)
            {
                mesh.Tree.Add(MmapMeshNode.LoadMapBinaryReader(reader));
            }
            for (int i = 0; i < mesh.Header.OffMeshConCount; i++)
            {
                mesh.OffMeshConnections.Add(MmapMeshOffMeshConnection.LoadMapBinaryReader(reader));
            }

            return mesh;
        }

        #endregion Public Methods
    }
}

