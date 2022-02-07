namespace TrinityCore.GameClient.Net.Lib.Network.Auth.Models
{
    public class WorldServerInfo
    {
        #region Public Properties

        public string Address { get; set; }
        public ushort Build { get; set; }
        public byte Flags { get; set; }
        public uint Id { get; set; }
        public byte Load { get; set; }
        public string Name { get; set; }
        public float Population { get; set; }
        public int Port { get; set; }
        public byte Timezone { get; set; }
        public byte Type { get; set; }
        public byte VersionBugFix { get; set; }
        public byte VersionMajor { get; set; }
        public byte VersionMinor { get; set; }

        #endregion Public Properties

        #region Public Fields

        public byte Locked;

        #endregion Public Fields

        #region Public Methods

        public override string ToString()
        {
            return Name;
        }

        #endregion Public Methods
    }
}