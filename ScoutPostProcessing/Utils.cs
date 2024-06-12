using PatternTools.FastaTools;
using ScoutCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ScoutPostProcessing
{
    public static class Utils
    {
        public static double[][] GetAccordMatrix(IEnumerable<IFDRElement> elements, List<string> features)
        {
            double[][] x = elements.Select(el =>
            {

                var d = new double[features.Count];

                for (int i = 0; i < features.Count; i++)
                {
                    double? v = el.Scores[features[i]];
                    if (v == null)
                        d[i] = 0;
                    else
                        d[i] = (double)v;
                }

                return d;
            }
            ).ToArray();

            return x;
        }

        public static double[][] MinMaxScale(double[][] old)
        {
            if (old.Length == 0)
                return old;

            var newX = old.ToList();

            for (int col = 0; col < newX[0].Length; col++)
            {
                double min = newX.Min(a => a[col]);
                double max = newX.Max(a => a[col]);

                if (min == max) continue;

                newX.ForEach(a => a[col] = (a[col] - min) / (max - min));
            }
            return newX.ToArray();
        }

        public static string getAccessionNumber(string protein)
        {
            string[] ptn = Regex.Split(protein, "\\|");
            string accNumber = "";

            if (ptn.Length > 1)
                accNumber = ptn[1];
            else
                accNumber = ptn[0];

            return accNumber;
        }
        public static string getGene(string protein)
        {
            string[] ptn = Regex.Split(protein, "\\|");
            string gene = "";

            if (ptn.Length > 1)
                gene = ptn[2];
            else
                gene = ptn[0];

            return gene;
        }
        public static List<T> Filter<T>(
            List<T> elements,
            double fdr_threshold,
            out double FDR,
            out double thresholdScore) where T : IFDRElement
        {
            if (elements == null || elements.Count == 0)
            {
                FDR = 0;
                thresholdScore = 0;
                return elements;
            }

            var newEls = elements.OrderBy(a => a.ClassificationScore).ToList();

            int numberOfDecoys = newEls.Where(a => a.IsDecoy == true).Count();
            int numberOfPositives = newEls.Where(a => a.IsDecoy == false).Count();

            int decoy_ctt = numberOfDecoys;
            FDR = 0;

            int cutoffIndex = 0;

            for (int i = 0; i < newEls.Count; i++)
            {
                FDR = (double)decoy_ctt / ((double)newEls.Count - i);

                if (FDR < fdr_threshold)
                {
                    cutoffIndex = i;
                    break;
                }

                if (newEls[i].IsDecoy == true)
                    decoy_ctt--;
            }
            thresholdScore = newEls[cutoffIndex].ClassificationScore;
            return newEls.Skip(cutoffIndex).ToList();
        }

        public static FastaFileParser GetSearchFasta(string fastaPath, ScoutParameters Params)
        {
            var digestionParams = Params.AssembleDigestionParemeters();

            var fp = Digestor.Digestor.AssembleFasta(fastaPath, digestionParams);
            fp.IncludeByteSequences();

            return fp;
        }

        internal static bool CheckIfInter(List<string> prots1, List<string> prots2)
        {
            foreach (string p1 in prots1)
            {
                foreach (string p2 in prots2)
                {
                    if (p1 == p2)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
