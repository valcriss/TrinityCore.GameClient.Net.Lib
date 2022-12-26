using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Network.World;
using TrinityCore.GameClient.Net.Lib.Network.World.Commands.Outgoing;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;
using TrinityCore.GameClient.Net.Lib.Network.World.Models;
using static ICSharpCode.SharpZipLib.Zip.ExtendedUnixData;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands.Outgoing
{
    internal class FacingMovement : WorldMovementPacket
    {
        internal FacingMovement(ulong guid, Position position, bool moving) : base(WorldCommand.MSG_MOVE_SET_FACING)
        {
            Guid = guid;
            Flags = moving ? MovementFlags.MOVEMENTFLAG_FORWARD : MovementFlags.MOVEMENTFLAG_NONE;
            X = position.X;
            Y = position.Y;
            Z = position.Z;
            O = position.O;
            ReadData();
        }
    }
}
