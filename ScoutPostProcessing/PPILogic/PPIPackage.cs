using Digestor;
using MemoryPack;
using ProtoBuf;
using ScoutPostProcessing.CSMLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScoutPostProcessing.PPILogic
{
    [MemoryPackable]
    [ProtoContract]
    public partial class PPIPackage : FDRPackage
    {
        [MemoryPackIgnore]
        public List<PPI> AllPPIs { get => AllElements.Cast<PPI>().ToList(); }

        [MemoryPackIgnore]
        public List<PPI> FilteredPPIs { get => FilteredElements.Cast<PPI>().ToList(); }


        public PPIPackage(List<PPI> ppis, FDRModes fdrMode, double fdr) : base(ppis.Cast<IFDRElement>().ToList(), fdrMode, fdr)
        {

        }

        [MemoryPackConstructor]
        public PPIPackage(List<IFDRElement> allElements, FDRModes fDRMode, double thresholdScore_Combined, double measuredFDR_Combined, double thresholdScore_Separate_Interlink, double thresholdScore_Separate_Intralink, double measuredFDR_Separate_Interlink, double measuredFDR_Separate_Intralink) : base(allElements, fDRMode, thresholdScore_Combined, measuredFDR_Combined, thresholdScore_Separate_Interlink, thresholdScore_Separate_Intralink, measuredFDR_Separate_Interlink, measuredFDR_Separate_Intralink)
        {
        }
    }
}
