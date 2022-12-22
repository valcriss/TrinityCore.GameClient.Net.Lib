using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Components.Player.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Models
{
    public class Proficiency
    {
        internal ItemClass ItemClass { get; set; }
        protected uint SubItemClass { get; set; }

        internal Proficiency(ItemClass itemClass, uint subItemClass)
        {
            ItemClass = itemClass;
            SubItemClass = subItemClass;
        }
    }
}
