namespace TrinityCore.GameClient.Net.Lib.Sample.Models
{
    internal abstract class Command
    {
        #region Public Methods

        public abstract bool Handle(string command);

        #endregion Public Methods
    }
}