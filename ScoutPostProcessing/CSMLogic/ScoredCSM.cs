using ScoutCore.PSMEngines;
using System;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;
using Digestor;
using ScoutPostProcessing.ResiduePairLogic;
using ScoutPostProcessing.PPILogic;
using SpectrumWizard.Predictors.CleavableXL;
using MemoryPack;
using SpectrumWizard.Utils;
using ScoutCore.SpectraOperations;
using ScoutCore.QueryLogic;

namespace ScoutPostProcessing.CSMLogic
{
    [MemoryPackable]
    [GenerateTypeScript]
    [ProtoContract]
    public partial class ScoredCSM : IFDRElement
    {
        [MemoryPackIgnore]
        [ProtoIgnore]
        public string ID { get => CSM_ID; set => CSM_ID = value; }
        [ProtoMember(22)]
        public string CSM_ID { get; set; }
        [ProtoMember(26)]
        public string ResPair_ID { get; set; }
        [ProtoMember(27)]
        public string PPI_ID { get; set; }
        [ProtoMember(1)]
        public string FileName { get; set; }
        [ProtoMember(2)]
        public int FileIndex { get; set; }
        [ProtoMember(3)]
        public int ScanNumber { get; set; }
        [ProtoMember(4)]
        public double PrecursorMZ { get; set; }
        [ProtoMember(5)]
        public short PrecursorCharge { get; set; }
        [ProtoMember(6)]
        public double RetentionTime { get; set; }
        [ProtoMember(7)]
        public double AlphaPairMH { get; set; }
        [ProtoMember(8)]
        public double BetaPairMH { get; set; }
        [ProtoMember(9)]
        public double AlphaPairIntensity { get; set; }
        [ProtoMember(10)]
        public double BetaPairIntensity { get; set; }
        [ProtoMember(11)]
        public int Rank { get; set; }
        [ProtoMember(12)]
        [MemoryPackAllowSerialize]
        public CleavePSM AlphaPSM { get; set; }
        [ProtoMember(13)]
        [MemoryPackAllowSerialize]
        public CleavePSM BetaPSM { get; set; }
        [ProtoMember(14)]
        [MemoryPackAllowSerialize]
        public List<PeptideMapping> AlphaMappings { get; set; }
        [ProtoMember(15)]
        [MemoryPackAllowSerialize]
        public List<PeptideMapping> BetaMappings { get; set; }
        [ProtoMember(16)]
        public Dictionary<string, double?> Scores { get; set; }
        [ProtoMember(17)]
        [MemoryPackAllowSerialize]
        public IdentificationType _Class { get; set; }
        [ProtoMember(19)]
        public double ClassificationScore { get; set; }
        [ProtoMember(20)]
        public bool IsInterLink { get; set; }
        [ProtoMember(28)]
        public bool IsLoopLink { get; set; } = false;
        [ProtoMember(21)]
        public string Spectrum { get; set; }
        [MemoryPackIgnore]
        public string QuantitationTag { get => internal_method2(); private set { } }
        [ProtoMember(23)]
        public double TheoreticalMZ { get; set; }
        [ProtoMember(24)]
        public double PrecursorPPM { get; set; }
        [ProtoMember(25)]
        public int TotalPairIsotopes { get; set; }
        [ProtoMember(29)]
        public List<(string reaction_site, int position, double score, bool isAlpha)> ReactionSitesProbabilities { get; set; }
        [ProtoMember(30)]
        public List<(string reaction_site1, string reaction_site2, int position1, int position2, double score)> ReactionSitesProbabilitiesLoopLink { get; set; }

        [MemoryPackIgnore]
        public double AlphaPPM
        {
            get => ScoringFunctions.GetPPM(
                            AlphaPSM.Peptide.MH,
                            AlphaPairMH,
                            AlphaPSM.IsotopeNumber);
        }

        [MemoryPackIgnore]
        public double BetaPPM
        {
            get => BetaPSM != null ? ScoringFunctions.GetPPM(
                            BetaPSM.Peptide.MH,
                            BetaPairMH,
                            BetaPSM.IsotopeNumber) : -1;
        }

        [MemoryPackIgnore]
        public bool IsDecoy { get => _Class != IdentificationType.FullTarget ? true : false; }

        [MemoryPackIgnore]
        /// <summary>
        /// Class numerical identifier.
        /// 0 - FullTarget
        /// 1 - SemiDecoy
        /// 2 - FullDecoy
        /// </summary>
        public int ClassNum
        {
            get
            {
                return _Class switch
                {
                    IdentificationType.FullTarget => 0,
                    IdentificationType.AlphaTarget => 1,
                    IdentificationType.BetaTarget => 1,
                    IdentificationType.FullDecoy => 2,
                    _ => -1,
                };
            }
        }

        public enum IdentificationType
        {
            FullTarget,
            AlphaTarget,
            BetaTarget,
            FullDecoy
        }

        [MemoryPackConstructor]
        public ScoredCSM(
            string cSM_ID,
            string resPair_ID,
            string pPI_ID,
            string fileName,
            int fileIndex,
            int scanNumber,
            double precursorMZ,
            short precursorCharge,
            double retentionTime,
            double alphaPairMH,
            double betaPairMH,
            double alphaPairIntensity,
            double betaPairIntensity,
            int rank,
            CleavePSM alphaPSM,
            CleavePSM betaPSM,
            List<PeptideMapping> alphaMappings,
            List<PeptideMapping> betaMappings,
            Dictionary<string, double?> scores,
            IdentificationType _class,
            double classificationScore,
            bool isInterLink,
            bool isLoopLink,
            string spectrum,
            double theoreticalMZ,
            double precursorPPM,
            int totalPairIsotopes,
            List<(string reaction_site, int position, double score, bool isAlpha)> reactionSitesProbabilities,
            List<(string reaction_site1, string reaction_site2, int position1, int position2, double score)> reactionSitesProbabilitiesLoopLink)
        {
            CSM_ID = cSM_ID;
            ResPair_ID = resPair_ID;
            PPI_ID = pPI_ID;
            FileName = fileName;
            FileIndex = fileIndex;
            ScanNumber = scanNumber;
            PrecursorMZ = precursorMZ;
            PrecursorCharge = precursorCharge;
            RetentionTime = retentionTime;
            AlphaPairMH = alphaPairMH;
            BetaPairMH = betaPairMH;
            AlphaPairIntensity = alphaPairIntensity;
            BetaPairIntensity = betaPairIntensity;
            Rank = rank;
            AlphaPSM = alphaPSM;
            BetaPSM = betaPSM;
            AlphaMappings = alphaMappings;
            BetaMappings = betaMappings;
            Scores = scores;
            _Class = _class;
            ClassificationScore = classificationScore;
            IsInterLink = isInterLink;
            IsLoopLink = isLoopLink;
            Spectrum = spectrum;
            TheoreticalMZ = theoreticalMZ;
            PrecursorPPM = precursorPPM;
            TotalPairIsotopes = totalPairIsotopes;
            ReactionSitesProbabilities = reactionSitesProbabilities;
            ReactionSitesProbabilitiesLoopLink = reactionSitesProbabilitiesLoopLink;
        }

        private ScoredCSM() { }

        /// <summary>
        /// Constructor for looplinks
        /// </summary>
        /// <param name="alphaPSM"></param>
        /// <param name="fileName"></param>
        /// <param name="rawFileIndex"></param>
        /// <param name="scan"></param>
        /// <param name="alphaPair"></param>
        /// <param name="cXLReagent"></param>
        /// <param name="rank"></param>
        /// <param name="totalPairIsotopes"></param>
        /// <param name="groupPPIByGene"></param>
        public ScoredCSM(
           CleavePSM alphaPSM,
           string fileName,
           int rawFileIndex,
           ScanResults scan,
           (double mh, double intensity) alphaPair,
           CleaveReagent cXLReagent,
           int rank,
           int totalPairIsotopes,
           List<(string reaction_site1, string reaction_site2, int position1, int position2, double score)> reactionSitesProbabilitiesLoopLink,
           bool groupPPIByGene = false)
        {
            AlphaPSM = alphaPSM;

            AlphaPairMH = alphaPair.mh;
            AlphaPairIntensity = alphaPair.intensity;
            AlphaMappings = AlphaPSM.Peptide.Mappings;

            FileName = fileName;
            FileIndex = rawFileIndex;
            ScanNumber = scan.ScanNumber;
            PrecursorMZ = scan.PrecursorMZ;
            PrecursorCharge = scan.PrecursorCharge;
            RetentionTime = scan.RetentionTime;
            ReactionSitesProbabilitiesLoopLink = reactionSitesProbabilitiesLoopLink;

            Rank = rank;
            IsInterLink = false;
            IsLoopLink = true;

            RefreshMappingsAndDecoyClass(groupPPIByGene);

            TheoreticalMZ = internal_method1(cXLReagent);

            var thMH = AlphaPSM.Peptide.MH + cXLReagent.WholeMass;
            var exMH = ScoutCore.SpectraOperations.ChargeOperations.ToMH(PrecursorMZ, PrecursorCharge);
            var mhPPM = ((thMH - exMH) * 1000000) / exMH;
            PrecursorPPM = mhPPM;

            TotalPairIsotopes = totalPairIsotopes;
        }

        public ScoredCSM(
            CleavePSM alphaPSM,
            CleavePSM betaPSM,
            string fileName,
            int rawFileIndex,
            ScanResults scan,
            (double mh, double intensity) alphaPair,
            (double mh, double intensity) betaPair,
            CleaveReagent cXLReagent,
            int rank,
            int totalPairIsotopes,
            List<(string reaction_site, int position, double score, bool isAlpha)> reactionSitesProbabilities,
            bool groupPPIByGene = false)
        {
            AlphaPSM = alphaPSM;
            BetaPSM = betaPSM;

            AlphaPairMH = alphaPair.mh;
            BetaPairMH = betaPair.mh;

            AlphaPairIntensity = alphaPair.intensity;
            BetaPairIntensity = betaPair.intensity;

            AlphaMappings = AlphaPSM.Peptide.Mappings;
            BetaMappings = BetaPSM.Peptide.Mappings;

            FileName = fileName;
            FileIndex = rawFileIndex;
            ScanNumber = scan.ScanNumber;
            PrecursorMZ = scan.PrecursorMZ;
            PrecursorCharge = scan.PrecursorCharge;
            RetentionTime = scan.RetentionTime;
            ReactionSitesProbabilities = reactionSitesProbabilities;

            Rank = rank;

            RefreshMappingsAndDecoyClass(groupPPIByGene);

            IsInterLink =
                Utils.CheckIfInter(
                    AlphaMappings.Select(a => a.Locus).ToList(),
                    BetaMappings.Select(a => a.Locus).ToList());


            TheoreticalMZ = internal_method0(cXLReagent);

            var thMH = AlphaPSM.Peptide.MH + BetaPSM.Peptide.MH + cXLReagent.WholeMass - Chemistry.Hydrogen;
            var exMH = ScoutCore.SpectraOperations.ChargeOperations.ToMH(PrecursorMZ, PrecursorCharge);

            var mhPPM = ((thMH - exMH) * 1000000) / exMH;

            PrecursorPPM = mhPPM;
            TotalPairIsotopes = totalPairIsotopes;
        }

        public void RefreshMappingsAndDecoyClass(bool groupByGene)
        {
            _Class = internal_method3();
            if (!IsLoopLink)
            {

                CSM_ID = $"{FileIndex}_{ScanNumber}";
                ResPair_ID = ResiduePairHelper.GetResiduePairIdentifier(AlphaPSM, BetaPSM);
                PPI_ID = PPIHelper.GetPPIIdentifier(AlphaPSM, BetaPSM, groupByGene);

            }
            else
            {
                CSM_ID = $"{FileIndex}_{ScanNumber}";
                ResPair_ID = ResiduePairHelper.GetResiduePairIdentifier(AlphaPSM, AlphaPSM);
                PPI_ID = PPIHelper.GetPPIIdentifier(AlphaPSM, AlphaPSM, groupByGene);

            }
        }
        public override string ToString()
        {
            return $"{ScanNumber} Alpha: {AlphaPSM.Peptide.AsString}, Beta: {BetaPSM.Peptide.AsString}";
        }


        private double internal_method0(CleaveReagent reagent)
        {
            double MH = AlphaPSM.Peptide.MH + BetaPSM.Peptide.MH + reagent.WholeMass - Chemistry.Hydrogen;
            return ScoutCore.SpectraOperations.ChargeOperations.ToMZ(MH, PrecursorCharge);
        }
        private double internal_method1(CleaveReagent reagent)
        {
            double MH = AlphaPSM.Peptide.MH + reagent.WholeMass;
            return ScoutCore.SpectraOperations.ChargeOperations.ToMZ(MH, PrecursorCharge);
        }
        private string internal_method2()
        {
            if (!IsLoopLink)
            {
                if (!String.IsNullOrEmpty(AlphaPSM.Peptide.Tag) &&
                    !String.IsNullOrEmpty(BetaPSM.Peptide.Tag))
                {
                    if (AlphaPSM.Peptide.Tag.Equals(BetaPSM.Peptide.Tag))
                        return AlphaPSM.Peptide.Tag;
                    else if (AlphaPSM.Peptide.Tag.StartsWith($"SILAC-"))
                        return $"SILAC-Hybrid_Alpha";
                    else
                        return $"SILAC-Hybrid_Beta";
                }
                else
                {
                    if (!String.IsNullOrEmpty(AlphaPSM.Peptide.Tag))
                        return $"SILAC-Hybrid_Alpha";
                    else
                    {
                        if (BetaPSM.Peptide.AsString.Contains("SILAC"))
                            return $"SILAC-Hybrid_Beta";
                        else
                            return "";
                    }
                }
            }
            else if (IsLoopLink)
            {
                if (!String.IsNullOrEmpty(AlphaPSM.Peptide.Tag))
                    return AlphaPSM.Peptide.Tag;
                else
                    return "";
            }
            else
                return "SILAC-Hybrid";
        }
        private IdentificationType internal_method3()
        {
            if (!IsLoopLink)
            {
                if (AlphaPSM.Peptide.IsDecoy == false && BetaPSM.Peptide.IsDecoy == false)
                    return IdentificationType.FullTarget;
                else if (AlphaPSM.Peptide.IsDecoy == false && BetaPSM.Peptide.IsDecoy == true)
                    return IdentificationType.AlphaTarget;
                else if (AlphaPSM.Peptide.IsDecoy == true && BetaPSM.Peptide.IsDecoy == false)
                    return IdentificationType.BetaTarget;
                else if (AlphaPSM.Peptide.IsDecoy == true && BetaPSM.Peptide.IsDecoy == true)
                    return IdentificationType.FullDecoy;
                else
                    throw new Exception();
            }
            else
            {
                if (AlphaPSM.Peptide.IsDecoy == false)
                    return IdentificationType.FullTarget;
                else if (AlphaPSM.Peptide.IsDecoy == true)
                    return IdentificationType.FullDecoy;
                else
                    throw new Exception();
            }
        }
    }
}