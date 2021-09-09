using System;
using System.Collections.Generic;
using System.Text;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World.Entities;

namespace TrinityCore.GameClient.Net.Lib.World.Commands
{
    public class CharactersListResponse : WorldReceivablePacket
    {
        public Character[] Characters { get; set; }

        public CharactersListResponse(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            byte count = ReadByte();
            if (count == 0)
            {
                Characters = new Character[0];
            }
            else
            {
                Characters = new Character[count];
                for (byte i = 0; i < count; ++i)
                {
                    Characters[i] = new Character(Buffer, ReadIndex);
                    ReadIndex = Characters[i].ReadIndex;
                }
            }
        }
    }
}
