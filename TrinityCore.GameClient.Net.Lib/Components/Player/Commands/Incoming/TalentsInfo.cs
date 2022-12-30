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

        #endregion Private Fields

        #region Internal Methods

        internal override void LoadData()
        {
            Talents = new List<Talent>();
            Glyphs = new List<Glyph>();
            IsPet = ReadSByte() != 0;
            if (IsPet)
            {
                LoadPetTalents();
            }
            else
            {
                LoadPlayerTalents();
            }
        }

        #endregion Internal Methods

        #region Private Methods

        private void LoadGlyphs(int group, sbyte glyphsToLoad)
        {
            for (int j = 0; j < glyphsToLoad; j++)
                Glyphs.Add(new Glyph
                {
                    Group = group,
                    GlyphId = ReadUInt16()
                });
        }

        private void LoadPetTalents()
        {
            UnSpendPoints = ReadUInt32();
            sbyte count = ReadSByte();
            for (int i = 0; i < count; i++)
            {
                Talents.Add(new Talent
                {
                    TalentId = ReadUInt32(),
                    TalentRank = ReadSByte()
                });
            }
        }

        private void LoadPlayerTalents()
        {
            UnSpendPoints = ReadUInt32();
            sbyte talentGroupCount = ReadSByte();
            ReadSByte();

            if (talentGroupCount == 0) return;
            if (talentGroupCount > MAX_TALENT_SPECS)
                talentGroupCount = MAX_TALENT_SPECS;

            for (int group = 0; group < talentGroupCount; group++)
            {
                sbyte talentsToLoad = ReadSByte();
                LoadTalents(group, talentsToLoad);

                sbyte glyphsToLoad = ReadSByte();
                LoadGlyphs(group, glyphsToLoad);
            }
        }

        private void LoadTalents(int group, sbyte talentsToLoad)
        {
            for (int j = 0; j < talentsToLoad; j++)
                Talents.Add(new Talent
                {
                    Group = group,
                    TalentId = ReadUInt32(),
                    TalentRank = ReadSByte()
                });
        }

        #endregion Private Methods
    }
}