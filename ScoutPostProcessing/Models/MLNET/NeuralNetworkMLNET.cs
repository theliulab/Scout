using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScoutPostProcessing.Models.MLNET
{
    internal class NeuralNetworkMLNET : ModelBase
    {

        public NeuralNetworkMLNET()
        {

        }

        public override double[] Score(double[][] x)
        {
            throw new NotImplementedException();
        }

        public override void Train(double[][] x, bool[] y)
        {
            throw new NotImplementedException();
        }
    }
}
