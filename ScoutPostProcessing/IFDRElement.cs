using MemoryPack;
using ScoutCore.QueryLogic;
using ScoutPostProcessing.CSMLogic;
using ScoutPostProcessing.PPILogic;
using ScoutPostProcessing.ResiduePairLogic;
using System.Collections.Generic;
using static ScoutPostProcessing.CSMLogic.ScoredCSM;

namespace ScoutPostProcessing
{

    [MemoryPackable]
    [MemoryPackUnion(0, typeof(ScoredCSM))]
    [MemoryPackUnion(1, typeof(ResPair))]
    [MemoryPackUnion(2, typeof(PPI))]
    public partial interface IFDRElement
    {
        string ID { get; }
        Dictionary<string, double?> Scores { get; set; }
        
        public IdentificationType _Class { get; set; }
        bool IsDecoy { get; }
        bool IsInterLink { get; }
        double ClassificationScore { get; set; }
    }
}