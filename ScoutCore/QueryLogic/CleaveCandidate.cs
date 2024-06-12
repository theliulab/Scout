using ScoutCore.IonPairLogic;
using ScoutCore.PSMEngines;
using ScoutCore.SpectraOperations;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using MemoryPack;

namespace ScoutCore.QueryLogic
{
    [MemoryPackable]
    [ProtoContract]
    public partial class CleaveCandidate : ICleaveCandidate
    {
        [ProtoMember(1)]
        [MemoryPackAllowSerialize]
        public ((IPSM psm, int index) light, (IPSM psm, int index) heavy) SelectedPSMs { get; set; }


        [ProtoMember(2)]
        [MemoryPackAllowSerialize]
        public IonPairLogic.IonPair LightPair { get; set; }
        [ProtoMember(3)]
        [MemoryPackAllowSerialize]
        public IonPairLogic.IonPair HeavyPair { get; set; }

        [ProtoMember(4)]
        [MemoryPackAllowSerialize]
        public List<PSMEngines.IPSM> AllLightPSMs { get; set; }
        [ProtoMember(5)]
        [MemoryPackAllowSerialize]
        public List<PSMEngines.IPSM> AllHeavyPSMs { get; set; }

        [ProtoMember(6)]
        public bool IsSymmetric { get; set; }
        [ProtoMember(11)]
        public int TotalPairIsotopes { get; private set; }
        [ProtoMember(7)]
        public int ScanNumber { get; set; }

        [ProtoMember(8)]
        public double XLScore { get; set; }
        [ProtoMember(9)]
        public int PeaksMatched { get; set; }
        [ProtoMember(10)]
        public double? XLDeltaCN { get; set; }
        [ProtoMember(12)]
        public bool IsLoopLink { get; set; } = false;
        [MemoryPackIgnore]
        public double LightPairMH { get => LightPair.SearchMass; }
        [MemoryPackIgnore]
        public double HeavyPairMH { get => HeavyPair.SearchMass; }
        [MemoryPackIgnore]
        public double LightPairIntensity { get => LightPair.Intensity; }
        [MemoryPackIgnore]
        public double HeavyPairIntensity { get => HeavyPair.Intensity; }
        [MemoryPackIgnore]

        public bool isFullCandidate
        {
            get
            {
                if (AllLightPSMs == null || AllHeavyPSMs == null
                    || AllLightPSMs.Count == 0 || AllHeavyPSMs.Count == 0)
                    return false;
                else
                    return true;


            }
        }
        [MemoryPackIgnore]
        private bool? isHeavyCSMAlpha
        {
            get
            {
                if (isFullCandidate == false)
                {
                    return null;
                }
                return AllHeavyPSMs[0].Peptide.Length >= AllLightPSMs[0].Peptide.Length;
            }

        }

        [MemoryPackConstructor]
        public CleaveCandidate(
            ((IPSM psm, int index) light,
            (IPSM psm, int index) heavy) selectedPSMs,
            IonPair lightPair,
            IonPair heavyPair,
            List<IPSM> allLightPSMs,
            List<IPSM> allHeavyPSMs,
            bool isSymmetric,
            int totalPairIsotopes,
            int scanNumber,
            double xLScore,
            int peaksMatched,
            double? xLDeltaCN,
            bool isLoopLink)
        {
            SelectedPSMs = selectedPSMs;
            LightPair = lightPair;
            HeavyPair = heavyPair;
            AllLightPSMs = allLightPSMs;
            AllHeavyPSMs = allHeavyPSMs;
            IsSymmetric = isSymmetric;
            TotalPairIsotopes = totalPairIsotopes;
            ScanNumber = scanNumber;
            XLScore = xLScore;
            PeaksMatched = peaksMatched;
            XLDeltaCN = xLDeltaCN;
            IsLoopLink = isLoopLink;
        }

        public CleaveCandidate() { }

        public CleaveCandidate(int scanNumber,
                               IonPair pair,
                               IonPair complementPair,
                               bool isSymmetric,
                               int maxQuery,
                               int pairIsotopes,
                               bool isLoopLink)
        {
            ScanNumber = scanNumber;
            IsLoopLink = isLoopLink;

            if (!IsLoopLink && complementPair.SearchMass < pair.SearchMass)
            {
                var aux = complementPair;
                complementPair = pair;
                pair = aux;
            }

            LightPair = pair;
            HeavyPair = complementPair;
            IsSymmetric = isSymmetric;
            TotalPairIsotopes = pairIsotopes;

            AllLightPSMs = new List<PSMEngines.IPSM>(maxQuery);

            if (isSymmetric == true)
            {
                AllHeavyPSMs = AllLightPSMs;
            }
            else
            {
                AllHeavyPSMs = new List<PSMEngines.IPSM>(maxQuery);
            }

        }

        public (IPSM alpha, IPSM beta) GetXLPeptides()
        {
            var psm1 = AllLightPSMs.First();
            var psm2 = AllHeavyPSMs.First();

            if (psm1.Peptide.Length < psm2.Peptide.Length)
            {
                return (psm2, psm1);
            }
            else
            {
                return (psm1, psm2);
            }
        }

        public (double mh, double intensity) GetAlphaPair()
        {
            if (isHeavyCSMAlpha == false)
            {
                return (LightPairMH, LightPairIntensity);
            }
            else
            {
                return (HeavyPairMH, HeavyPairIntensity);
            }
        }

        public (double mh, double intensity) GetBetaPair()
        {
            if (isHeavyCSMAlpha == false)
            {
                return (HeavyPairMH, HeavyPairIntensity);
            }
            else
            {
                return (LightPairMH, LightPairIntensity);
            }
        }


        public override string ToString()
        {
            string XL = "";
            if (AllLightPSMs != null && AllLightPSMs.Count != 0)
                XL += $"XL: {AllLightPSMs.First().Peptide.AsString}";
            if (AllHeavyPSMs != null && AllHeavyPSMs.Count != 0)
                XL += $"-{AllHeavyPSMs.First().Peptide.AsString}";

            string pairs = $"Pep1 MH: {LightPair.SearchMass} - ";
            if (!IsSymmetric)
                pairs += $"Pep2 MH: {HeavyPair.SearchMass}";

            return
                $"Scan {ScanNumber}. " +
                $"Precursor: {IsSymmetric}. " +
                $"{pairs}. " +
                $"{XL}.";
        }

        public List<CleavePSM> GetAlphaPSMs()
        {
            if (isHeavyCSMAlpha == false)
            {
                return AllLightPSMs?.Cast<CleavePSM>().ToList();
            }
            else
            {
                return AllHeavyPSMs?.Cast<CleavePSM>().ToList();
            }
        }

        public List<CleavePSM> GetBetaPSMs()
        {
            if (isHeavyCSMAlpha == false)
            {
                return AllHeavyPSMs?.Cast<CleavePSM>().ToList();
            }
            else
            {
                return AllLightPSMs?.Cast<CleavePSM>().ToList();
            }
        }
    }
}