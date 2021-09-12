using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.Network.Entities;

namespace TrinityCore.GameClient.Net.Lib.Auth.Commands
{
    internal class RealmListResponse : AuthReceivablePacket
    {
        internal List<WorldServerInfo> Realms { get; set; }

        internal RealmListResponse(ReceivablePacket receivable) : base(receivable)
        {
            Realms = new List<WorldServerInfo>();
            ReadUInt16();
            ReadUInt32();
            ushort count = ReadUInt16();
            for (int i = 0; i < count; ++i)
            {
                byte type = ReadByte();
                byte locked = ReadByte();
                byte flags = ReadByte();
                string name = ReadCString();
                string fullAddress = ReadCString();
                string[] tokens = fullAddress.Split(':');
                string address = tokens[0];
                int port = tokens.Length > 1 ? int.Parse(tokens[1]) : 8085;
                float population = ReadSingle();
                byte load = ReadByte();
                byte timezone = ReadByte();
                byte id = ReadByte();

                byte versionMajor = 0;
                byte versionMinor = 0;
                byte versionBugFix = 0;
                ushort build = 0;

                if ((flags & 4) != 0)
                {
                    versionMajor = ReadByte();
                    versionMinor = ReadByte();
                    versionBugFix = ReadByte();
                    build = ReadUInt16();
                }

                Realms.Add(new WorldServerInfo
                {
                    Id = id,
                    Type = type,
                    Locked = locked,
                    Flags = flags,
                    Name = name,
                    Address = address,
                    Port = port,
                    Population = population,
                    Load = load,
                    Timezone = timezone,
                    VersionMajor = versionMajor,
                    VersionMinor = versionMinor,
                    VersionBugFix = versionBugFix,
                    Build = build
                });
            }
        }
    }
}