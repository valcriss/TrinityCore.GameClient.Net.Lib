using TrinityCore.GameClient.Net.Lib.Map.Tools;

namespace TrinityCore.GameClient.Net.Lib.Map.MmapTile
{
    public class MmapMeshPolyDetail
    {
        #region Public Properties

        public byte[] Padding { get; set; }
        public uint TriBase { get; set; }
        public byte TriCount { get; set; }
        public uint VertBase { get; set; }
        public byte VertCount { get; set; }

        #endregion Public Properties

        #region Public Methods

        public static MmapMeshPolyDetail LoadBinaryReader(BinaryReader reader)
        {
            MmapMeshPolyDetail polyDetail = new MmapMeshPolyDetail();
            polyDetail.VertBase = reader.ReadUint();
            polyDetail.TriBase = reader.ReadUint();
            polyDetail.VertCount = reader.ReadByte();

            polyDetail.TriCount = reader.ReadByte();
            polyDetail.Padding = reader.ReadBytes(2);

            return polyDetail;
        }

        #endregion Public Methods
    }
}