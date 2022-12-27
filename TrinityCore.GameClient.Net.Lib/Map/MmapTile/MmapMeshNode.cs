using TrinityCore.GameClient.Net.Lib.Map.Tools;

namespace TrinityCore.GameClient.Net.Lib.Map.MmapTile
{
    public class MmapMeshNode
    {
        #region Public Properties

        public ushort[] Bmax { get; set; }
        public ushort[] Bmin { get; set; }
        public int I { get; set; }

        #endregion Public Properties

        #region Public Methods

        public static MmapMeshNode LoadBinaryReader(BinaryReader reader)
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