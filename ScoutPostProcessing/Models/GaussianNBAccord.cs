using Accord.MachineLearning.Bayes;
using Accord.Statistics.Distributions.Univariate;
using System.Linq;

namespace ScoutPostProcessing.Models
{
    public class GaussianNBAccord : ModelBase
    {
        private NaiveBayes<NormalDistribution> _model;

        public GaussianNBAccord()
        {
        }

        public override double[] Score(double[][] x)
        {
            double[][] p = _model.Probabilities(x);

            return p.Select(a => a[1]).ToArray();
        }

        public override void Train(double[][] x, bool[] y)
        {
            var learner  = new NaiveBayesLearning<NormalDistribution>();
            _model = learner.Learn(x, y.ToList().Select(a => a ? 1 : 0).ToArray());
        }

        public override string ToString()
        {
            return $"Gaussian Naive Bayes";
        }
    }
}