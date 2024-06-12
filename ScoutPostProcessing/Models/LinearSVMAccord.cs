using Accord.MachineLearning.VectorMachines;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Statistics.Kernels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScoutPostProcessing.Models
{
    public class LinearSVMAccord : ModelBase
    {
        //private SequentialMinimalOptimization<Linear> _model;
        private SupportVectorMachine<Linear> _model;
        private double _complexity;
        private double _epsilon;
        private double _tolerance;

        public LinearSVMAccord(double complexity = 1.0, double epsilon = 1.0e-3, double tolerance = 0.001)
        {
            _complexity = complexity;
            _epsilon = epsilon;
            _tolerance = tolerance;
        }

        public override void Train(double[][] x, bool[] y)
        {
            Console.WriteLine("Creating and training Poly kernel SVM");
            var learner = new SequentialMinimalOptimization<Linear>
            {
                Complexity = _complexity,
                Kernel = new Linear(),
                Epsilon = _epsilon,
                Tolerance = _tolerance
            };
            
            this._model = learner.Learn(x, y);
        }

        public override double[] Score(double[][] x)
        {
            double[][] p = _model.Probabilities(x);

            return p.Select(a => a[1]).ToArray();
        }

        public override string ToString()
        {
            return $"Linear SVM (c: {_complexity}, e: {_epsilon:N2}, t: {_tolerance:N2})";
        }
    }
}
