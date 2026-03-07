using TrinityCore.GameClient.Net.Lib.Map.Tools;

namespace TrinityCore.GameClient.Net.Lib.Map.MmapTile
{
    public class MmapMeshLink
    {
        #region Public Properties

        public byte Bmax { get; set; }
        public byte Bmin { get; set; }
        public byte Edge { get; set; }
        public uint Next { get; set; }
        public long Reference { get; set; }
        public byte Side { get; set; }

        #endregion Public Properties

        #region Public Methods

        public static MmapMeshLink LoadBinaryReader(BinaryReader reader)
        {
            MmapMeshLink link = new MmapMeshLink();
            link.Reference = reader.ReadInt64();
            link.Next = reader.ReadUint();
            link.Edge = reader.ReadByte();
            link.Side = reader.ReadByte();
            link.Bmin = reader.ReadByte();
            link.Bmax = reader.ReadByte();
            return link;
        }

        #endregion Public Methods
    }
}