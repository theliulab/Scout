using MemoryPack;
using ProtoBuf;
using ScoutPostProcessing.CSMLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScoutPostProcessing.ResiduePairLogic
{
    [MemoryPackable]
    [ProtoContract]
    public partial class ResiduePairPackage : FDRPackage
    {
        [MemoryPackIgnore]
        public List<ResPair> AllResPairs { get => AllElements.Cast<ResPair>().ToList(); }

        [MemoryPackIgnore]
        public List<ResPair> FilteredResPairs { get => FilteredElements.Cast<ResPair>().ToList(); }


        public ResiduePairPackage(List<ResPair> respairs, FDRModes fdrMode, double fdr) : base(respairs.Cast<IFDRElement>().ToList(), fdrMode, fdr)
        {

        }

        [MemoryPackConstructor]
        public ResiduePairPackage(List<IFDRElement> allElements, FDRModes fDRMode, double thresholdScore_Combined, double measuredFDR_Combined, double thresholdScore_Separate_Interlink, double thresholdScore_Separate_Intralink, double measuredFDR_Separate_Interlink, double measuredFDR_Separate_Intralink) : base(allElements, fDRMode, thresholdScore_Combined, measuredFDR_Combined, thresholdScore_Separate_Interlink, thresholdScore_Separate_Intralink, measuredFDR_Separate_Interlink, measuredFDR_Separate_Intralink)
        {
        }
    }
}
