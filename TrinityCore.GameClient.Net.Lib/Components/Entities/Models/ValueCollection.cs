using System;
using System.Collections.Generic;
using System.Linq;

namespace TrinityCore.GameClient.Net.Lib.Components.Entities.Models
{
    public class ValueCollection<T>
    {
        #region Protected Properties

        protected Dictionary<ulong, T> Items { get; set; }

        #endregion Protected Properties

        #region Public Constructors

        public ValueCollection()
        {
            Items = new Dictionary<ulong, T>();
        }

        #endregion Public Constructors

        #region Public Methods

        public void Add(ulong guid, T value)
        {
            lock (Items)
            {
                if (!Items.ContainsKey(guid))
                {
                    Items.Add(guid, value);
                }
            }
        }

        public bool Contains(ulong guid)
        {
            return Items.ContainsKey(guid);
        }

        public T FirstOrDefault(Func<T, bool> predicate = null)
        {
            lock (Items)
            {
                if (predicate != null)
                {
                    return Items.Values.FirstOrDefault(predicate);
                }
                return Items.Values.FirstOrDefault();
            }
        }

        public T Get(ulong guid)
        {
            lock (Items)
            {
                if (!Items.ContainsKey(guid)) return default;
                return Items[guid];
            }
        }

        public void Remove(ulong guid)
        {
            lock (Items)
            {
                if (Items.ContainsKey(guid))
                {
                    Items.Remove(guid);
                }
            }
        }

        public void Set(ulong guid, T value)
        {
            lock (Items)
            {
                if (Items.ContainsKey(guid))
                {
                    Items[guid] = value;
                }
            }
        }

        #endregion Public Methods
    }
}