namespace TrinityCore.GameClient.Net.Lib.Network.World.Models
{
    internal class MovementPosition
    {
        #region Internal Properties

        internal Position Position { get; set; }
        internal bool Transport { get; set; }
        internal ulong? TransportGuid { get; set; }
        internal Position TransportPosition { get; set; }

        #endregion Internal Properties
    }
}