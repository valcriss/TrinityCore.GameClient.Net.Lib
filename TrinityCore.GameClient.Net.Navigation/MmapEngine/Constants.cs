namespace TrinityCore.GameClient.Net.Navigation.MmapEngine
{
    public static class Constants
    {
        #region Public Fields

        public const string MMAP_FILE_CHECK_REGEXP = "^[0-9]{3}$";
        public const int MMAP_FILE_EXPECTED_LENGTH = 28;
        public const string MMAP_FILE_EXTENSION = "mmap";
        public const string MMAP_TILE_FILE_CHECK_REGEXP = "^[0-9]{3}[0-9]{2}[0-9]{2}$";
        public const string MMAP_TILE_FILE_EXTENSION = "mmtile";
        public const int MMAP_TILE_FILE_HEADER_SIZE = 20;

        #endregion Public Fields
    }
}

