using System.Collections.Generic;
using System.Linq;
using TrinityCore.GameClient.Net.Lib.Logging;

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

        #region Internal Methods

        internal bool UpdateFriend(Friend update)
        {
            lock (Friends)
            {
                Friend friend = Friends.FirstOrDefault(c => c.Guid == update.Guid);
                if (friend == null)
                {
                    Logger.Append(Logging.Enums.LogCategory.SOCIAL, Logging.Enums.LogLevel.ERROR, $"Unable to update friend {update.Guid} status, friend not found");
                    return false;
                }
                friend.Level = update.Level;
                friend.Note = update.Note;
                friend.Status = update.Status;
                friend.Class = update.Class;
                friend.AreaId = update.AreaId;
                return true;
            }
        }

        #endregion Internal Methods
    }
}