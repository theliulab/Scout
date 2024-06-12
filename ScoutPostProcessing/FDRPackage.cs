using MemoryPack;
using ScoutPostProcessing.CSMLogic;
using ScoutPostProcessing.PPILogic;
using ScoutPostProcessing.ResiduePairLogic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ScoutPostProcessing
{
    [MemoryPackable]
    [MemoryPackUnion(0, typeof(CSMPackage))]
    [MemoryPackUnion(1, typeof(ResiduePairPackage))]
    [MemoryPackUnion(2, typeof(PPIPackage))]
    public abstract partial class FDRPackage
    {
        [MemoryPackAllowSerialize]
        public List<IFDRElement> AllElements { get; set; }
        [MemoryPackAllowSerialize]
        public FDRModes FDRMode { get; set; }

        
        public double ThresholdScore_Combined { get; set; }
        public double MeasuredFDR_Combined { get; private set; }

        public double ThresholdScore_Separate_Interlink { get; set; }
        public double ThresholdScore_Separate_Intralink { get; set; }
        public double MeasuredFDR_Separate_Interlink { get; private set; }
        public double MeasuredFDR_Separate_Intralink { get; private set; }

        public List<IFDRElement> FilteredElements
        {
            get
            {
                if (FDRMode == FDRModes.CombinedIntraInter)
                {
                    return AllElements.Where(a => a.ClassificationScore >= ThresholdScore_Combined).ToList();
                }
                else if (FDRMode == FDRModes.SeparateIntraInter)
                {
                    var decoys = AllElements
                        .Where(a => a.IsDecoy)
                        .Where(a => a.ClassificationScore >= Math.Min(ThresholdScore_Separate_Interlink, ThresholdScore_Separate_Intralink) )
                        .ToList();
                    var inters = AllElements
                        .Where(a => a.IsInterLink && !a.IsDecoy)
                        .Where(a => a.ClassificationScore >= ThresholdScore_Separate_Interlink)
                        .ToList();
                    var intras = AllElements
                        .Where(a => !a.IsInterLink && !a.IsDecoy)
                        .Where(a => a.ClassificationScore >= ThresholdScore_Separate_Intralink)
                        .ToList();

                    return inters.Concat(intras).Concat(decoys).OrderByDescending(a => a.ClassificationScore).ToList();
                        
                }

                return null;
            }
        }

        [MemoryPackConstructor]
        public FDRPackage(
            List<IFDRElement> allElements,
            FDRModes fDRMode,
            double thresholdScore_Combined,
            double measuredFDR_Combined,
            double thresholdScore_Separate_Interlink,
            double thresholdScore_Separate_Intralink,
            double measuredFDR_Separate_Interlink,
            double measuredFDR_Separate_Intralink)
        {
            AllElements = allElements;
            FDRMode = fDRMode;
            ThresholdScore_Combined = thresholdScore_Combined;
            MeasuredFDR_Combined = measuredFDR_Combined;
            ThresholdScore_Separate_Interlink = thresholdScore_Separate_Interlink;
            ThresholdScore_Separate_Intralink = thresholdScore_Separate_Intralink;
            MeasuredFDR_Separate_Interlink = measuredFDR_Separate_Interlink;
            MeasuredFDR_Separate_Intralink = measuredFDR_Separate_Intralink;
        }

        public FDRPackage(List<IFDRElement> allElements, FDRModes fdrMode, double fdr)
        {
            AllElements = allElements;
            FDRMode = fdrMode;

            RefreshFDRValues(allElements, fdr);
        }

        public void RefreshFDRValues(List<IFDRElement> allElements, double fdr)
        {
            var decoys = allElements.Where(a => a.IsDecoy).ToList();
            var inters = allElements.Where(a => a.IsInterLink && !a.IsDecoy).ToList();
            var intras = allElements.Where(a => !a.IsInterLink && !a.IsDecoy).ToList();

            #region Combined FDR

            var filteredElementsCombined = Utils.Filter(
                allElements,
                fdr,
                out double measuredFDR_Combined,
                out double thresholdCombined);

            MeasuredFDR_Combined = measuredFDR_Combined;
            ThresholdScore_Combined = thresholdCombined;

            #endregion

            #region Separate FDR

            var filteredElementsSeparated_Inter = Utils.Filter(
                inters.Concat(decoys).ToList(),
                fdr,
                out double measuredFDR_SeparateInter,
                out double thresholdSeparate_Inter);

            MeasuredFDR_Separate_Interlink = measuredFDR_SeparateInter;
            ThresholdScore_Separate_Interlink = thresholdSeparate_Inter;

            var filteredElementsSeparated_Intra = Utils.Filter(
               intras.Concat(decoys).ToList(),
               fdr,
               out double measuredFDR_SeparateIntra,
               out double thresholdSeparate_Intra);

            MeasuredFDR_Separate_Intralink = measuredFDR_SeparateIntra;
            ThresholdScore_Separate_Intralink = thresholdSeparate_Intra;

            #endregion
        }
    }
}