using System.Collections.Generic;

namespace TrinityCore.GameClient.Net.Lib.Components.Social.Models
{
    public class Contacts
    {
        #region Public Properties

        public List<Friend> Friends { get; set; }
        public List<Contact> Ignored { get; set; }
        public List<Contact> Muted { get; set; }

        #endregion Public Properties

        #region Public Constructors

        public Contacts()
        {
            Muted = new List<Contact>();
            Ignored = new List<Contact>();
            Friends = new List<Friend>();
        }

        #endregion Public Constructors
    }
}