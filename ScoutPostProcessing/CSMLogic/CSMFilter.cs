using ScoutPostProcessing.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScoutPostProcessing.CSMLogic
{
    public class CSMFilter : BaseFilter
    {
        private const double _DIFF_PPM_THRESHOLD = 45;

        public CSMFilter(List<string> features = null, ModelBase model = null) : base(features, model)
        {

        }

        public override ModelBase GetDefaultModel()
        {
            return new Models.NeuralNetworkAccord();
        }

        public override List<string> GetDefaultFeatures()
        {
            return new List<string>()
            {
                "XlinkxGroupedScore",
                "MinDeltaCN",
                "DiffPPM",
                "MinDDPScore",
            };
        }

        public CSMPackage Filter(List<ScoredCSM> allCSMs, double fdr, FDRModes fdrMode)
        {
            (double realFDR, double thresholdScore, int decoyCount) = ScoreElements(
                allCSMs.Select(a => (IFDRElement) a).ToList(),
                fdr);

            FDR_Real = realFDR;
            ThresholdScore = thresholdScore;
            DecoyCount = decoyCount;
            
            var r = new CSMPackage(
                allCSMs.OrderByDescending(a => a.ClassificationScore).ToList(),
                fdrMode,
                fdr);

            return r;

        }

        public override List<IFDRElement> ApplyThresholds(List<IFDRElement> cSMs)
        {
            return cSMs
                .Where(a => a.Scores["DiffPPM"] < _DIFF_PPM_THRESHOLD)
                .ToList();
        }
    }
}
