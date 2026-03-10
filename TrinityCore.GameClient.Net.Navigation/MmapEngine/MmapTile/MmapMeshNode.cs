using TrinityCore.GameClient.Net.Navigation.MmapEngine.Tools;

namespace TrinityCore.GameClient.Net.Navigation.MmapEngine.MmapTile
{
    public class MmapMeshNode
    {
        #region Public Properties

        public ushort[] Bmax { get; set; }
        public ushort[] Bmin { get; set; }
        public int I { get; set; }

        #endregion Public Properties

        #region Public Methods

        public static MmapMeshNode LoadMapBinaryReader(MapBinaryReader reader)
        {
            MmapMeshNode node = new MmapMeshNode();
            node.Bmin = reader.ReadUShorts(3);
            node.Bmax = reader.ReadUShorts(3);
            node.I = reader.ReadInt();
            return node;
        }

        #endregion Public Methods
    }
}

