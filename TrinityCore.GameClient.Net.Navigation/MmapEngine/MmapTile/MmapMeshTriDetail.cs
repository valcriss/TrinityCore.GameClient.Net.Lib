using TrinityCore.GameClient.Net.Navigation.MmapEngine.Tools;

namespace TrinityCore.GameClient.Net.Navigation.MmapEngine.MmapTile
{
    public class MmapMeshTriDetail
    {
        #region Public Properties

        public byte[] Tri { get; set; }

        #endregion Public Properties

        #region Public Methods

        public static MmapMeshTriDetail LoadMapBinaryReader(MapBinaryReader reader)
        {
            MmapMeshTriDetail triDetail = new MmapMeshTriDetail();
            triDetail.Tri = reader.ReadBytes(4);
            return triDetail;
        }

        #endregion Public Methods
    }
}

