using MemoryPack;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScoutPostProcessing.CSMLogic
{
    [MemoryPackable]
    [ProtoContract]
    public partial class CSMPackage : FDRPackage
    {
        [MemoryPackIgnore]
        public List<ScoredCSM> AllCSMs { get => AllElements.Cast<ScoredCSM>().ToList(); }

        [MemoryPackIgnore]
        public List<ScoredCSM> FilteredCSMs { get => FilteredElements.Cast<ScoredCSM>().ToList(); }


        
        public CSMPackage(List<ScoredCSM> cSMs, FDRModes fdrMode, double fdr) : base(cSMs.Cast<IFDRElement>().ToList(), fdrMode, fdr)
        {
            
        }

        [MemoryPackConstructor]
        public CSMPackage(List<IFDRElement> allElements, FDRModes fDRMode, double thresholdScore_Combined, double measuredFDR_Combined, double thresholdScore_Separate_Interlink, double thresholdScore_Separate_Intralink, double measuredFDR_Separate_Interlink, double measuredFDR_Separate_Intralink) : base(allElements, fDRMode, thresholdScore_Combined, measuredFDR_Combined, thresholdScore_Separate_Interlink, thresholdScore_Separate_Intralink, measuredFDR_Separate_Interlink, measuredFDR_Separate_Intralink)
        {
        }
    }
}
