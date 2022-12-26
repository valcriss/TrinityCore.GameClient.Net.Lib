using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Commands.Outgoing
{
    internal class WorldMovementPacket : WorldSendablePacket
    {
        internal ulong Guid
        {
            get;
            set;
        }

        internal MovementFlags Flags
        {
            get;
            set;
        }

        internal MovementFlags2 Flags2
        {
            get;
            set;
        }

        internal uint Time
        {
            get;
            set;
        }

        internal float X
        {
            get;
            set;
        }

        internal float Y
        {
            get;
            set;
        }

        internal float Z
        {
            get;
            set;
        }

        internal float O
        {
            get;
            set;
        }

        internal uint FallTime
        {
            get;
            set;
        }

        internal WorldMovementPacket(WorldCommand command) : base(command)
        {
            
        }

        protected void ReadData()
        {
            Time = (uint)(DateTime.Now - Process.GetCurrentProcess().StartTime).TotalMilliseconds;
            Flags2 = MovementFlags2.MOVEMENTFLAG2_NONE;
            AppendPacketGuid(Guid);
            Append((uint)Flags);
            Append((ushort)Flags2);
            Append(Time);
            Append(X);
            Append(Y);
            Append(Z);
            Append(O);
            Append(FallTime);
        }
    }
}
