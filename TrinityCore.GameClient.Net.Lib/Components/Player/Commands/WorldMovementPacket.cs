using System;
using System.Diagnostics;
using TrinityCore.GameClient.Net.Lib.World;
using TrinityCore.GameClient.Net.Lib.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands
{
    public class WorldMovementPacket : WorldSendablePacket
    {
        public ulong Guid
        {
            get;
            set;
        }

        public MovementFlags Flags
        {
            get;
            set;
        }

        public MovementFlags2 Flags2
        {
            get;
            set;
        }

        public uint Time
        {
            get;
            set;
        }

        public float X
        {
            get;
            set;
        }

        public float Y
        {
            get;
            set;
        }

        public float Z
        {
            get;
            set;
        }

        public float O
        {
            get;
            set;
        }

        public uint FallTime
        {
            get;
            set;
        }

        public WorldMovementPacket(WorldSocket worldSocket, WorldCommand command) : base(worldSocket, command)
        {
            Time = (uint)(DateTime.Now - Process.GetCurrentProcess().StartTime).TotalMilliseconds;
        }

        public override byte[] GetBuffer()
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
