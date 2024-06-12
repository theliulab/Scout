using ScoutCore.IonPairLogicOmega;
using ScoutCore.PSMEngines;
using ScoutCore.SpectraOperations;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MemoryPack;

namespace ScoutCore.QueryLogic
{
    [MemoryPackable]
    [ProtoContract]
    public partial class CleaveCandidateOmega : ICleaveCandidate
    {
        [ProtoMember(0)]
        [MemoryPackAllowSerialize]
        public CleaveDoublet CleavePairs { get; set; }

        [ProtoMember(4)]
        [MemoryPackAllowSerialize]
        public List<PSMEngines.IPSM> AllLightPSMs { get; set; }
        [ProtoMember(5)]
        [MemoryPackAllowSerialize]
        public List<PSMEngines.IPSM> AllHeavyPSMs { get; set; }

        [ProtoMember(6)]
        public bool IsSymmetric { get; set; }

        [ProtoMember(7)]
        public int ScanNumber { get; set; }
        [ProtoMember(8)]
        public double XLScore { get; set; }
        [ProtoMember(9)]
        public int PeaksMatched { get; set; }
        [ProtoMember(10)]
        public bool IsLoopLink { get; set; } = false;

        [MemoryPackIgnore]
        public double LightPairMH => (float)CleavePairs.LightPairGroup.SearchMass;
        [MemoryPackIgnore]
        public double HeavyPairMH => (float)CleavePairs.HeavyPairGroup.SearchMass;

        [MemoryPackIgnore]
        public double LightPairIntensity => throw new NotImplementedException();
        [MemoryPackIgnore]
        public double HeavyPairIntensity => throw new NotImplementedException();
        [MemoryPackIgnore]
        public ((IPSM psm, int index) light, (IPSM psm, int index) heavy) SelectedPSMs { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        [MemoryPackIgnore]
        public double? XLDeltaCN { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        [MemoryPackConstructor]
        public CleaveCandidateOmega(
            CleaveDoublet cleavePairs,
            List<PSMEngines.IPSM> allLightPSMs,
            List<PSMEngines.IPSM> allHeavyPSMs,
            bool isSymmetric,
            int scanNumber,
            double xLScore,
            int peaksMatched)
        {
            CleavePairs = cleavePairs;
            AllLightPSMs = allLightPSMs;
            AllHeavyPSMs = allHeavyPSMs;
            IsSymmetric = isSymmetric;
            ScanNumber = scanNumber;
            XLScore = xLScore;
            PeaksMatched = peaksMatched;
        }

        public CleaveCandidateOmega(
            int scanNumber,
            CleaveDoublet cd,
            int maxQuery)
        {
            ScanNumber = scanNumber;
            CleavePairs = cd;

            AllLightPSMs = new List<PSMEngines.IPSM>(maxQuery);

            if (IsSymmetric == true)
            {
                AllHeavyPSMs = AllLightPSMs;
            }
            else
            {
                AllHeavyPSMs = new List<PSMEngines.IPSM>(maxQuery);
            }
        }

        public List<CleavePSM> GetAlphaPSMs()
        {
            throw new NotImplementedException();
        }

        public List<CleavePSM> GetBetaPSMs()
        {
            throw new NotImplementedException();
        }

        public (double, double) GetAlphaPair()
        {
            throw new NotImplementedException();
        }

        public (double, double) GetBetaPair()
        {
            throw new NotImplementedException();
        }
    }
}
