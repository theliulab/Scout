using ScoutCore.PSMEngines;
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
    [MemoryPackUnion(0, typeof(CleaveCandidate))]
    [MemoryPackUnion(1, typeof(CleaveCandidateOmega))]
    
    [ProtoBuf.ProtoContract, ProtoInclude(1, typeof(CleaveCandidate))]
    //[ProtoBuf.ProtoContract, ProtoInclude(2, typeof(CleaveCandidate))]
    public partial interface ICleaveCandidate
    {
        ((PSMEngines.IPSM psm, int index) light, (PSMEngines.IPSM psm, int index) heavy) SelectedPSMs { get; set; }
        

        List<PSMEngines.IPSM> AllLightPSMs { get; set; }
        List<PSMEngines.IPSM> AllHeavyPSMs { get; set; }

        bool IsSymmetric { get; set; }
        bool IsLoopLink { get; set; }

        int ScanNumber { get; set; }
        double LightPairMH { get; }
        double HeavyPairMH { get; }
        double LightPairIntensity { get; }
        double HeavyPairIntensity { get; }
        double XLScore { get; set;  }
        int PeaksMatched { get; set; }
        double? XLDeltaCN { get; set; }

        //double LightPairIntensities { get; set; }
        //double HeavyPairIntensities { get; set; }

        List<CleavePSM> GetAlphaPSMs();
        List<CleavePSM> GetBetaPSMs();
        (double mh, double intensity) GetAlphaPair();
        (double mh, double intensity) GetBetaPair();
    }
}
