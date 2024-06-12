using ScoutPostProcessing.CSMLogic;
using ScoutPostProcessing.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ScoutPostProcessing
{
    public abstract class BaseFilter
    {
        public List<string> Features { get; set; }
        public ModelBase Model { get; set; }
        public double FDR_Real { get; set; }
        public double ThresholdScore { get; set; }
        public int DecoyCount { get; set; }

        public BaseFilter(List<string> features = null, ModelBase model = null)
        {
            if (features == null)
                Features = GetDefaultFeatures();
            else
                Features = features;

            if (model == null)
                Model = GetDefaultModel();
            else
                Model = model;
        }

        public abstract ModelBase GetDefaultModel();

        public abstract List<string> GetDefaultFeatures();

        public abstract List<IFDRElement> ApplyThresholds(List<IFDRElement> elements);

        public (double realFDR, double thresholdScore, int decoyCount) ScoreElements(List<IFDRElement> allElements, double fdrThresh)
        {
            (double realFDR, double thresholdScore, int decoyCount) = ScoreAllElements(
                allElements,
                fdrThresh);

            int old_decoyCount;
            int ctt = 0;
            do
            {
                old_decoyCount = decoyCount;
                Model = GetDefaultModel() as ModelBase;
                (realFDR, thresholdScore, decoyCount) = ScoreAllElements(allElements, fdrThresh);
                ctt++;
            }
            while (ctt < 10 && old_decoyCount > 0 && (decoyCount >= old_decoyCount * 1.05 || decoyCount <= old_decoyCount * 0.95));

            return (realFDR, thresholdScore, decoyCount);
        }

        private (double realFDR, double thresholdScore, int decoyCount) ScoreAllElements(List<IFDRElement> allElements, double fdrThresh)
        {
            List<IFDRElement> remainingCSMs = ApplyThresholds(allElements);

            double[][] X = Utils.GetAccordMatrix(remainingCSMs, Features);
            X = Utils.MinMaxScale(X);
            bool[] y = remainingCSMs.Select(a => !a.IsDecoy).ToArray();

            if (X.Length != y.Length)
            {
                throw new Exception($"X and y sizes are different. X: {X.Length}, y: {y.Length}");
            }

            ModelBase modelToUse = null;

            if (X.Length < 10)
            {
                Console.WriteLine($"Number of elements to train model too low. Defaulting to Naive Bayes model");
                modelToUse = new GaussianNBAccord();
            }
            else
            {
                modelToUse = Model;
            }

            #region Scores applied to sets that contain only target or decoys
            double[] scores = new double[y.Length];
            if (y.Where(a => a == true).Count() == y.Length)// There are only target candidates.
            {
                for (int i = 0; i < y.Length; i++)
                    scores[i] = X[i][0];
            }
            else if (y.Where(a => a == false).Count() == y.Length)// There are only decoy candidates.
            {
                for (int i = 0; i < y.Length; i++)
                    scores[i] = 0;
            }
            else
            {
                try
                {
                    modelToUse.Train(X, y);
                    scores = modelToUse.Score(X);
                }
                catch (Exception) { }
            }
            #endregion

            for (int i = 0; i < remainingCSMs.Count; i++)
            {
                remainingCSMs[i].ClassificationScore = scores[i];
            }
            //Take out all poor results
            remainingCSMs.RemoveAll(a => a.IsDecoy && a.ClassificationScore == 0);

            List<IFDRElement> ids = Utils.Filter(
                remainingCSMs,
                fdrThresh,
                out double realFDR,
                out double thresholdScore);

            int decoyCount = ids.Where(a => a.IsDecoy).Count();
            (double realFDR, double thresholdScore, int decoyCount) r = (realFDR, thresholdScore, decoyCount);

            return r;
        }
    }
}