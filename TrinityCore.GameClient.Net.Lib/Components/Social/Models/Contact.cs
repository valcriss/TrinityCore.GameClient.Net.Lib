using System;

namespace TrinityCore.GameClient.Net.Lib.Components.Social.Models
{
    public class Contact
    {
        #region Public Properties

        public UInt64 Guid { get; set; }
        public string Note { get; set; }

        #endregion Public Properties

        #region Public Constructors

        public Contact(UInt64 guid, string note)
        {
            Guid = guid;
            Note = note;
        }

        #endregion Public Constructors
    }
}