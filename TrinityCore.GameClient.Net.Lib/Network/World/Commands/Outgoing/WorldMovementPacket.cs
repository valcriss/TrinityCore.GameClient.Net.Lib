using System;
using System.Diagnostics;
using TrinityCore.GameClient.Net.Lib.Network.World.Enums;

namespace TrinityCore.GameClient.Net.Lib.Network.World.Commands.Outgoing
{
    internal class WorldMovementPacket : WorldSendablePacket
    {
        #region Internal Properties

        internal uint FallTime
        {
            get;
            set;
        }

        internal MovementTypes Flags
        {
            get;
            set;
        }

        internal MovementOptions Flags2
        {
            get;
            set;
        }

        internal ulong Guid
        {
            get;
            set;
        }

        internal float O
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

        #endregion Internal Properties

        #region Internal Constructors

        internal WorldMovementPacket(WorldCommand command) : base(command)
        {
        }

        #endregion Internal Constructors

        #region Protected Methods

        protected void ReadData()
        {
            Time = (uint)(DateTime.Now - Process.GetCurrentProcess().StartTime).TotalMilliseconds;
            Flags2 = MovementOptions.None;
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

        #endregion Protected Methods
    }
}