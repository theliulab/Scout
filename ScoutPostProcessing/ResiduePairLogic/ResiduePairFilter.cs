using ScoutPostProcessing.CSMLogic;
using ScoutPostProcessing.Models;
using ScoutPostProcessing.Scoring;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScoutPostProcessing.ResiduePairLogic
{
    public class ResiduePairFilter : BaseFilter
    {
        
        public ResiduePairFilter(List<string> features = null, ModelBase model = null) : base(features, model)
        {

        }

        public override ModelBase GetDefaultModel()
        {
            return new Models.NeuralNetworkAccord();
        }

     
        public override List<string> GetDefaultFeatures()
        {
            return new() {
                "MaxXlinkxScore",
                "BestClassificationScore"
            };
        }

        internal ResiduePairPackage Filter(List<ResPair> respairs, double resPair_FDR, FDRModes fdrMode)
        {
            (double realFDR, double thresholdScore, int decoyCount) = ScoreElements(
                respairs.Select(a => (IFDRElement)a).ToList(),
                resPair_FDR);

            FDR_Real = realFDR;
            ThresholdScore = thresholdScore;
            DecoyCount = decoyCount;

            var pack = new ResiduePairPackage(
                respairs,
                fdrMode, 
                resPair_FDR);

            return pack;
        }

     
        public override List<IFDRElement> ApplyThresholds(List<IFDRElement> elements)
        {
            return elements;
        }
    }
}
