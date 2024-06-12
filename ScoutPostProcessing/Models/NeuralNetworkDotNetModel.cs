using NeuralNetworkNET.APIs;
using NeuralNetworkNET.APIs.Enums;
using NeuralNetworkNET.APIs.Interfaces;
using NeuralNetworkNET.APIs.Interfaces.Data;
using NeuralNetworkNET.APIs.Results;
using NeuralNetworkNET.APIs.Structs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ScoutPostProcessing.Models
{
    internal class NeuralNetworkDotNetModel : ModelBase
    {
        public INeuralNetwork TrainedNetwork { get; private set; }

        public override double[] Score(double[][] x)
        {
            IEnumerable<float[]> predictions = x.Select(
                a => TrainedNetwork.Forward(
                    a.Select(b => (float)b).ToArray()  ));

            return predictions.Select(a => (double)a[0]).ToArray();
        }

        public override void Train(double[][] input, bool[] output)
        {
            if (input.Length == 0) return;

            INeuralNetwork network = NetworkManager.NewSequential(
                TensorInfo.Linear(input[0].Length),
                NetworkLayers.FullyConnected(20, ActivationType.Sigmoid),
                NetworkLayers.Softmax(2));

            List<(float[] x, float[] u)> data = input
                .Zip(output)
                .Select(a => (a.First.Select(b => (float)b).ToArray(), BoolToFloatArray(a.Second)))
                .ToList();

            ITrainingDataset dataset = DatasetLoader.Training(
                data, 
                input.Length);

            TrainingSessionResult result = NetworkManager.TrainNetwork(
                    network,                                // The network instance to train
                    dataset,                                // The ITrainingDataset instance   
                    TrainingAlgorithms.StochasticGradientDescent(0.1f, 0.1f),          // The training algorithm to use
                    500,                                     // The expected number of training epochs to run
                    0,                                   // Dropout probability
                    null,                               // Optional training epoch progress callback
                    null,                                   // Optional callback to monitor the training dataset accuracy
                    null,                                   // Optional validation dataset
                    null,                                   // Test dataset
                    default);                                 // Cancellation token for the training

            TrainedNetwork = network;

            //var p1 = network.Forward(data[0].x);
        }
    }
}