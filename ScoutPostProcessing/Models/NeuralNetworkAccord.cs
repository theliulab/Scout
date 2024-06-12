using Accord.Neuro;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScoutPostProcessing.Models
{
    internal class NeuralNetworkAccord : ModelBase

    {
        private ActivationNetwork Network;

        int Epochs { get; set; } = 80;

        public NeuralNetworkAccord(int epochs = 80)
        {
            Epochs = epochs;
        }

        public override double[] Score(double[][] x)
        {
            double[] predictions = new double[x.Length];
            if (Network == null)
                return predictions;

            Parallel.For(0, x.Length, i =>
            {
                double[] input = x[i];
                double[] output = Network.Compute(input);

                predictions[i] = output.Max();
            });
            return predictions;
        }

        public override void Train(double[][] x, bool[] y)
        {
            double[][] input, output;
            input = x;
            output = (from element in y
                      select new double[] { Convert.ToDouble(element) }).ToArray();

            // create neural network
            Network = new ActivationNetwork(
                new SigmoidFunction(2),
                input[0].Length,
                2, // 2 neurons in the first layer
                1); // 1 neuron in the second layer
                     // create teacher
            Accord.Neuro.Learning.BackPropagationLearning Model = new Accord.Neuro.Learning.BackPropagationLearning(Network);
            Model.LearningRate = 10;
            Model.Momentum = 0.5;

            // Run supervised learning.
            for (int i = 0; i < Epochs; i++)
            {
                Model.RunEpoch(input, output);
            }

        }
    }
}
