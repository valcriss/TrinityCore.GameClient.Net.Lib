using System;
using System.Diagnostics;
using TrinityCore.GameClient.Net.Lib.World;
using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands
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

        internal WorldMovementPacket(WorldSocket worldSocket, WorldCommand command) : base(worldSocket, command)
        {
            Time = (uint)(DateTime.Now - Process.GetCurrentProcess().StartTime).TotalMilliseconds;
            Flags2 = MovementFlags2.MOVEMENTFLAG2_NONE;
        }

        internal override byte[] GetBuffer()
        {
            AppendPacketGuid(Guid);
            Append((uint)Flags);
            Append((ushort)Flags2);
            Append(Time);
            Append(X);
            Append(Y);
            Append(Z);
            Append(O);
            Append(FallTime);
            return base.GetBuffer();
        }
    }
}
