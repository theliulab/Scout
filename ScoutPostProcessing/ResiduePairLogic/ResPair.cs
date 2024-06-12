using Accord.Math;
using Digestor;
using MemoryPack;
using ProtoBuf;
using ScoutCore.PSMEngines;
using ScoutPostProcessing.CSMLogic;
using ScoutPostProcessing.PPILogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ScoutPostProcessing.CSMLogic.ScoredCSM;

namespace ScoutPostProcessing.ResiduePairLogic
{
    [MemoryPackable]
    [ProtoContract]
    public partial class ResPair : IFDRElement
    {
        [MemoryPackIgnore]
        [ProtoIgnore]
        public string ID { get => ResiduePair_ID; set => ResiduePair_ID = value; }

        [ProtoMember(1)]
        public string ResiduePair_ID { get; set; }
        [ProtoMember(8)]
        public string PPI_ID { get; set; }

        [ProtoMember(2)]
        [MemoryPackAllowSerialize]
        public List<ScoredCSM> CSMs { get; set; }
        [ProtoMember(3)]
        public IdentificationType _Class { get; set; }
        [ProtoMember(4)]
        public bool IsDecoy { get; set; }
        [ProtoMember(5)]
        public double ClassificationScore { get; set; }
        [ProtoMember(6)]
        public Dictionary<string, double?> Scores { get; set; }
        [ProtoMember(7)]
        public bool IsInterLink { get; set; }
        [MemoryPackIgnore]
        public ScoredCSM LongestAlphaCSM { get => GetCSMWithLongestPeptideSequence(true); }
        [MemoryPackIgnore]
        public ScoredCSM LongestBetaCSM { get => GetCSMWithLongestPeptideSequence(false); }
        [MemoryPackIgnore]
        public int AlphaPeptPosition { get => LongestAlphaCSM.AlphaPSM.ReagentPosition1 + 1; }
        [MemoryPackIgnore]
        public int BetaPeptPosition { get => LongestBetaCSM != null ? LongestBetaCSM.BetaPSM.ReagentPosition1 + 1 : -1; }
        [MemoryPackIgnore]
        public string AlphaProtPosition { get => String.Join(',', GetXLProteinPosition(LongestAlphaCSM.AlphaMappings, LongestAlphaCSM.AlphaPSM.ReagentPosition1).ToList()); }
        [MemoryPackIgnore]
        public string BetaProtPosition { get => LongestBetaCSM != null ? String.Join(',', GetXLProteinPosition(LongestBetaCSM.BetaMappings, LongestBetaCSM.BetaPSM.ReagentPosition1).ToList()) : string.Empty; }
        [MemoryPackIgnore]
        public string ResiduePairString { get => GetResiduePairWithLinks(); set { } }
        [MemoryPackIgnore]
        public string AlphaPeptideString { get => GetPeptideWithLinkPosition(LongestAlphaCSM.AlphaPSM.Peptide.AsCleanString, LongestAlphaCSM.AlphaPSM.ReagentPosition1); set { } }
        [MemoryPackIgnore]
        public string BetaPeptideString { get => LongestBetaCSM != null ? GetPeptideWithLinkPosition(LongestBetaCSM.BetaPSM.Peptide.AsCleanString, LongestBetaCSM.BetaPSM.ReagentPosition1) : string.Empty; set { } }
        [MemoryPackIgnore]
        public string AlphaProteins { get => String.Join(", ", LongestAlphaCSM.AlphaMappings.Select(a => "sp|" + a.Locus + "|" + a.Gene).ToList()); }
        [MemoryPackIgnore]
        public string BetaProteins { get => LongestBetaCSM != null ? String.Join(", ", LongestBetaCSM.BetaMappings.Select(a => "sp|" + a.Locus + "|" + a.Gene).ToList()) : string.Empty; }
        [MemoryPackIgnore]
        public string AlphaGenes { get => String.Join(", ", LongestAlphaCSM.AlphaMappings.Select(a => a.Gene).ToList()); }
        [MemoryPackIgnore]
        public string BetaGenes { get => LongestBetaCSM != null ? String.Join(", ", LongestBetaCSM.BetaMappings.Select(a => a.Gene).ToList()) : string.Empty; }
        [MemoryPackIgnore]
        public string LinkType { get => GetLinkType(); private set { } }
        [MemoryPackIgnore]
        public string ReactionSitesProbability { get; private set; }

        private string GetLinkType()
        {
            if (this.IsInterLink)
                return "Heteromeric-inter";
            else
            {
                if (this.AlphaProteins.Equals(this.BetaProteins) && this.AlphaProtPosition.Equals(this.BetaProtPosition))
                    return "Homomeric-inter";
                else
                    return "Intra";
            }
        }


        private ScoredCSM GetCSMWithLongestPeptideSequence(bool isAlpha)
        {
            if (isAlpha)
                return CSMs.OrderByDescending(a => a.AlphaPSM.Peptide.AsCleanString.Length).FirstOrDefault();
            else
            {
                if (CSMs[0].BetaPSM == null) return null;
                return CSMs.OrderByDescending(a => a.BetaPSM.Peptide.AsCleanString.Length).FirstOrDefault();
            }
        }
        private List<int> GetXLProteinPosition(List<PeptideMapping> peptideMappings, int reagent_position)
        {
            List<int> positions = new List<int>();

            foreach (var bestMapping in peptideMappings)
            {
                positions.Add(bestMapping.ProteinPosition + reagent_position + 1);
            }
            return positions;
        }

        private string GetPeptideWithLinkPosition(string peptide, int position)
        {
            List<char> new_peptide = peptide.ToCharArray().ToList();

            new_peptide.Insert(position, ' ');
            new_peptide.Insert(position + 1, '[');
            new_peptide.Insert(position + 3, ']');
            new_peptide.Insert(position + 4, ' ');

            return new String(new_peptide.ToArray());
        }

        private string GetResiduePairWithLinks()
        {
            List<char> alpha = LongestAlphaCSM.AlphaPSM.Peptide.AsCleanString.ToCharArray().ToList();

            if (LongestBetaCSM != null)
            {
                alpha.Insert(LongestAlphaCSM.AlphaPSM.ReagentPosition1, ' ');
                alpha.Insert(LongestAlphaCSM.AlphaPSM.ReagentPosition1 + 1, '[');
                alpha.Insert(LongestAlphaCSM.AlphaPSM.ReagentPosition1 + 3, ']');
                alpha.Insert(LongestAlphaCSM.AlphaPSM.ReagentPosition1 + 4, ' ');


                List<char> beta = LongestBetaCSM.BetaPSM.Peptide.AsCleanString.ToCharArray().ToList();

                beta.Insert(LongestBetaCSM.BetaPSM.ReagentPosition1, ' ');
                beta.Insert(LongestBetaCSM.BetaPSM.ReagentPosition1 + 1, '[');
                beta.Insert(LongestBetaCSM.BetaPSM.ReagentPosition1 + 3, ']');
                beta.Insert(LongestBetaCSM.BetaPSM.ReagentPosition1 + 4, ' ');
                return new String(alpha.ToArray()) + " \u2194 " + new String(beta.ToArray());
            }
            else
            {
                alpha.Insert(LongestAlphaCSM.AlphaPSM.ReagentPosition1, ' ');
                alpha.Insert(LongestAlphaCSM.AlphaPSM.ReagentPosition1 + 1, '[');
                alpha.Insert(LongestAlphaCSM.AlphaPSM.ReagentPosition1 + 3, ']');
                alpha.Insert(LongestAlphaCSM.AlphaPSM.ReagentPosition1 + 4, ' ');

                alpha.Insert(LongestAlphaCSM.AlphaPSM.ReagentPosition2 + 4, ' ');
                alpha.Insert(LongestAlphaCSM.AlphaPSM.ReagentPosition2 + 5, '[');
                alpha.Insert(LongestAlphaCSM.AlphaPSM.ReagentPosition2 + 7, ']');
                alpha.Insert(LongestAlphaCSM.AlphaPSM.ReagentPosition2 + 8, ' ');
                return new String(alpha.ToArray());
            }
        }

        private ResPair() { }

        [MemoryPackConstructor]
        public ResPair(
            string residuePair_ID,
            string pPI_ID,
            List<ScoredCSM> cSMs,
            IdentificationType _class,
            bool isDecoy,
            double classificationScore,
            Dictionary<string, double?> scores,
            bool isInterLink)
        {
            ResiduePair_ID = residuePair_ID;
            PPI_ID = pPI_ID;
            CSMs = cSMs;
            _Class = _class;
            IsDecoy = isDecoy;
            ClassificationScore = classificationScore;
            Scores = scores;
            IsInterLink = isInterLink;
        }

        public ResPair(string id, List<ScoredCSM> csms)
        {
            ResiduePair_ID = id;
            PPI_ID = csms.First().PPI_ID;
            CSMs = csms;
            IsDecoy = csms.First().IsDecoy;
            _Class = csms.First()._Class;
            IsInterLink = csms.First().IsInterLink;
        }
    }
}
