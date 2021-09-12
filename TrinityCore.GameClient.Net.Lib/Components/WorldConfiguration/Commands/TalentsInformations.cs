using System.Collections.Generic;
using TrinityCore.GameClient.Net.Lib.Components.Player.Entities;
using TrinityCore.GameClient.Net.Lib.Network.Core;
using TrinityCore.GameClient.Net.Lib.World;

namespace TrinityCore.GameClient.Net.Lib.Components.WorldConfiguration.Commands
{
    internal class TalentsInformations : WorldReceivablePacket
    {
        private const sbyte MAX_TALENT_SPECS = 2;
        private const sbyte MAX_TALENT_TABS = 2;
        internal List<GlyphInfo> GlyphInfos { get; set; }
        internal bool IsPet { get; set; }
        internal List<TalentInfo> TalentInfos { get; set; }
        internal uint UnSpendTalentPoints { get; set; }

        internal TalentsInformations(ReceivablePacket receivablePacket) : base(receivablePacket)
        {
            TalentInfos = new List<TalentInfo>();
            GlyphInfos = new List<GlyphInfo>();
            IsPet = ReadSByte() != 0;
            if (IsPet)
            {
                UnSpendTalentPoints = ReadUInt32();
                sbyte count = ReadSByte();
                for (int i = 0; i < count; i++)
                    TalentInfos.Add(new TalentInfo
                    {
                        TalentId = ReadUInt32(),
                        TalentRank = ReadSByte()
                    });
            }
            else
            {
                UnSpendTalentPoints = ReadUInt32();
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
                            TalentInfos.Add(new TalentInfo
                            {
                                Group = i,
                                TalentId = ReadUInt32(),
                                TalentRank = ReadSByte()
                            });

                        sbyte maxGlyphCount = ReadSByte();
                        for (int j = 0; j < maxGlyphCount; j++)
                            GlyphInfos.Add(new GlyphInfo
                            {
                                Group = i,
                                GlyphId = ReadUInt16()
                            });
                    }
                }
            }
        }
    }
}
