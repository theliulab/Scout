using ScoutPostProcessing.Models;
using ScoutPostProcessing.ResiduePairLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScoutPostProcessing.PPILogic
{
    public class PPIFilter : BaseFilter
    {
        private bool uniquePPIsOnly;


        public PPIFilter(List<string> features = null, ModelBase model = null, bool uniquePPIsOnly = true) : base(features, model)
        {
            this.uniquePPIsOnly = uniquePPIsOnly;
        }

        public override ModelBase GetDefaultModel()
        {
            return new Models.NeuralNetworkAccord();
        }

        public override List<string> GetDefaultFeatures()
        {
            return new() 
            {
                "BestClassificationScore",
            };
        }

        internal PPIPackage Filter(List<PPI> ppis, double fdrThresh, FDRModes fdrMode)
        {
            if (uniquePPIsOnly == false)
                ppis = ppis.Where(a => a.IsUnique).ToList();

            (double realFDR, double thresholdScore, int decoyCount) = ScoreElements(
                ppis.Select(a => (IFDRElement)a).ToList(),
                fdrThresh);

            FDR_Real = realFDR;
            ThresholdScore = thresholdScore;
            DecoyCount = decoyCount;
            
            var pack = new PPIPackage(
                ppis,
                fdrMode,
                fdrThresh);

            return pack;
        }

        public override List<IFDRElement> ApplyThresholds(List<IFDRElement> elements)
        {
            return elements;
        }
    }
}
