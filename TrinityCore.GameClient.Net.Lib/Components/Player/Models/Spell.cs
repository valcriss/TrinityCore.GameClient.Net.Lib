namespace TrinityCore.GameClient.Net.Lib.Components.Player.Models
{
    public class Spell
    {
        #region Public Properties

        public uint Id { get; set; }

        #endregion Public Properties

        #region Public Methods

        public override string ToString()
        {
            return Id.ToString();
        }

        #endregion Public Methods
    }
}