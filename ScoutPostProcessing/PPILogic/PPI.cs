using Digestor;
using MemoryPack;
using Newtonsoft.Json.Linq;
using ProtoBuf;
using ScoutCore.Scoring;
using ScoutPostProcessing.CSMLogic;
using ScoutPostProcessing.ResiduePairLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static ScoutPostProcessing.CSMLogic.ScoredCSM;

namespace ScoutPostProcessing.PPILogic
{
    [MemoryPackable]
    [ProtoContract]
    public partial class PPI : IFDRElement
    {
        [MemoryPackIgnore]
        [ProtoIgnore]
        public string ID { get => PPI_ID; set => PPI_ID = value; }

        [ProtoMember(1)]
        public string PPI_ID { get; set; }

        [ProtoMember(2)]
        [MemoryPackAllowSerialize]
        public List<ResPair> ResPairs { get; set; }

        [ProtoMember(3)]
        public List<string> AlphaProteins { get; set; }
        [ProtoMember(4)]
        public List<string> BetaProteins { get; set; }

        [ProtoMember(5)]
        public IdentificationType _Class { get; set; }
        [ProtoMember(6)]
        public bool IsDecoy { get; set; }
        [ProtoMember(7)]
        public double ClassificationScore { get; set; }

        [ProtoMember(8)]
        public Dictionary<string, double?> Scores { get; set; }
        [ProtoMember(9)]
        public bool IsUnique { get; internal set; }
        [ProtoMember(10)]
        public bool IsInterLink { get; set; }

        [MemoryPackIgnore]
        public int ResPairCount { get => ResPairs.Count; }
        [MemoryPackIgnore]
        public int CSMCount { get => ResPairs.SelectMany(a => a.CSMs).Count(); }
        [MemoryPackIgnore]
        public string ProteinOneString { get => GetProteins(true); private set { } }
        [MemoryPackIgnore]
        public string ProteinTwoString { get => GetProteins(false); private set { } }
        [MemoryPackIgnore]
        public string GeneOneString { get => GetGenes(true); private set { } }
        [MemoryPackIgnore]
        public string GeneTwoString { get => GetGenes(false); private set { } }
        [MemoryPackIgnore]
        public string PPIString { get => this.ID.Replace("+", " \u2194 "); set { } }
        [MemoryPackIgnore]
        public string LinkType { get => GetLinkType(); private set { } }

        private string GetLinkType()
        {
            if (this.IsInterLink)
                return "Inter";
            else
                return "Intra";
        }

        private Dictionary<string, int> GetProteinScores(List<ScoredCSM> filteredCSMs)
        {
            var scoresDict = new Dictionary<string, int>();

            foreach (var csm in filteredCSMs)
            {
                var proteinList = csm.BetaMappings != null ? csm.AlphaMappings.Concat(csm.BetaMappings).Distinct() : csm.AlphaMappings;//BetaMapping is null if csm is looplink
                foreach (var protein in proteinList)
                {
                    if (scoresDict.ContainsKey(protein.Locus))
                    {
                        scoresDict[protein.Locus] += 1;
                    }
                    else
                    {
                        scoresDict.Add(protein.Locus, 1);
                    }
                }
            }

            return scoresDict;
        }

        private string GetProteins(bool isFirst)
        {
            Peptide peptide = null;
            if (isFirst)
            {
                if (IsGeneOne())
                    peptide = ResPairs[0].LongestAlphaCSM.AlphaPSM.Peptide;
                else
                    peptide = ResPairs[0].LongestBetaCSM.BetaPSM.Peptide;
            }
            else
            {
                if (IsGeneOne())
                    peptide = ResPairs[0].LongestBetaCSM != null ? ResPairs[0].LongestBetaCSM.BetaPSM.Peptide : ResPairs[0].LongestAlphaCSM.AlphaPSM.Peptide;//If BetaPeptide is null then there is looplink peptide
                else
                    peptide = ResPairs[0].LongestAlphaCSM.AlphaPSM.Peptide;
            }
            List<string> proteins = peptide.Mappings.Select(a => "sp|" + a.Locus + "|" + a.Gene).Distinct().ToList();
            return string.Join(", ", proteins);
        }
        private bool IsGeneOne()
        {
            //PPI_ID can be composed by Gene or Locus
            string[] genes = PPI_ID.Split('+');
            if (ResPairs != null && ResPairs.Count > 0 && ResPairs[0].LongestAlphaCSM.AlphaPSM.Peptide.Mappings.Any(a => Regex.Split(genes[0], ";").Any(b => a.Gene.Equals(b))))
                return true;
            else
            {
                if (ResPairs != null && ResPairs.Count > 0 && ResPairs[0].LongestAlphaCSM.AlphaPSM.Peptide.Mappings.Any(a => Regex.Split(genes[0], ";").Any(b => a.Locus.Equals(b))))
                    return true;
                else return false;
            }
        }

        private string GetGenes(bool isFirst)
        {
            Peptide peptide = null;
            if (isFirst)
            {
                if (IsGeneOne())
                    peptide = ResPairs[0].LongestAlphaCSM.AlphaPSM.Peptide;
                else
                    peptide = ResPairs[0].LongestBetaCSM.BetaPSM.Peptide;
            }
            else
            {
                if (IsGeneOne())
                    peptide = ResPairs[0].LongestBetaCSM != null ? ResPairs[0].LongestBetaCSM.BetaPSM.Peptide : ResPairs[0].LongestAlphaCSM.AlphaPSM.Peptide;//If BetaPeptide is null then there is looplink peptide
                else
                    peptide = ResPairs[0].LongestAlphaCSM.AlphaPSM.Peptide;
            }

            List<string> genes = peptide.Mappings.Select(a => a.Gene).Distinct().ToList();
            return string.Join(", ", genes);
        }

        private bool CheckIfUnique()
        {
            var alphaCount = ResPairs.First().CSMs.First().AlphaMappings.Count();
            var betaCount = ResPairs.First().CSMs.First().BetaMappings != null ? ResPairs.First().CSMs.First().BetaMappings.Count() : 1;

            if (alphaCount == 1 && betaCount == 1)
                return true;
            else
                return false;
        }

        public List<ScoredCSM> GetAllCSMs()
        {
            var allCSMs = ResPairs.SelectMany(a => a.CSMs).ToList();

            return allCSMs;
        }
        private PPI() { }

        [MemoryPackConstructor]
        public PPI(
            string pPI_ID,
            List<ResPair> resPairs,
            List<string> alphaProteins,
            List<string> betaProteins,
            IdentificationType _class,
            bool isDecoy,
            double classificationScore,
            Dictionary<string, double?> scores,
            bool isUnique,
            bool isInterLink)
        {
            PPI_ID = pPI_ID;
            ResPairs = resPairs;
            AlphaProteins = alphaProteins;
            BetaProteins = betaProteins;
            _Class = _class;
            IsDecoy = isDecoy;
            ClassificationScore = classificationScore;
            Scores = scores;
            IsUnique = isUnique;
            IsInterLink = isInterLink;
        }

        public PPI(string ppi_ID, List<ResPair> resPairs)
        {
            PPI_ID = ppi_ID;
            ResPairs = resPairs;
            IsDecoy = resPairs.First().IsDecoy;
            _Class = resPairs.First()._Class;
            IsUnique = CheckIfUnique();
            IsInterLink = resPairs.First().IsInterLink;
        }
    }
}