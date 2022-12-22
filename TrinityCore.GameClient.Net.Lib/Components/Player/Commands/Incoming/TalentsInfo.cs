using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Components.Player.Models;
using TrinityCore.GameClient.Net.Lib.Network.Core;

namespace TrinityCore.GameClient.Net.Lib.Components.Player.Commands.Incoming
{
    internal class TalentsInfo : ReceivablePacket<Network.World.Enums.WorldCommand>
    {
        #region Public Properties

        public List<Glyph> Glyphs { get; set; }
        public bool IsPet { get; set; }
        public List<Talent> Talents { get; set; }
        public uint UnSpendPoints { get; set; }

        #endregion Public Properties

        #region Private Fields

        private const sbyte MAX_TALENT_SPECS = 2;
        private const sbyte MAX_TALENT_TABS = 2;

        #endregion Private Fields

        #region Internal Methods

        internal override void LoadData()
        {
            Talents = new List<Talent>();
            Glyphs = new List<Glyph>();
            IsPet = ReadSByte() != 0;
            if (IsPet)
            {
                UnSpendPoints = ReadUInt32();
                sbyte count = ReadSByte();
                for (int i = 0; i < count; i++)
                    Talents.Add(new Talent
                    {
                        TalentId = ReadUInt32(),
                        TalentRank = ReadSByte()
                    });
            }
            else
            {
                UnSpendPoints = ReadUInt32();
                sbyte talentGroupCount = ReadSByte();
                sbyte talentGroupIndex = ReadSByte();

                if (talentGroupCount > 0)
                {
                    if (talentGroupCount > MAX_TALENT_SPECS)
                        talentGroupCount = MAX_TALENT_SPECS;

                    for (int i = 0; i < talentGroupCount; i++)
                    {
                        sbyte talentIdCount = ReadSByte();
                        for (int j = 0; j < talentIdCount; j++)
                            Talents.Add(new Talent
                            {
                                Group = i,
                                TalentId = ReadUInt32(),
                                TalentRank = ReadSByte()
                            });

                        sbyte maxGlyphCount = ReadSByte();
                        for (int j = 0; j < maxGlyphCount; j++)
                            Glyphs.Add(new Glyph
                            {
                                Group = i,
                                GlyphId = ReadUInt16()
                            });
                    }
                }
            }
        }

        #endregion Internal Methods
    }
}