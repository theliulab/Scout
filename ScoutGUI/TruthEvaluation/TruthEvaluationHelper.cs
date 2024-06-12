using MS.WindowsAPICodePack.Internal;
using ScoutPostProcessing;
using ScoutPostProcessing.CSMLogic;
using ScoutPostProcessing.PPILogic;
using ScoutPostProcessing.ResiduePairLogic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scout.TruthEvaluation
{
    public class TruthEvaluationHelper
    {
        public static (int csm_errors_inter, int csm_errors_intra, int rp_errors_inter, int rp_errors_intra, int ppi_errors_inter, int ppi_errors_intra) CheckTruth(XLFilteredResults results,
            string truthFileName)
        {
            var truth = GetTruth(truthFileName);

            int csm_errors_inter = CountWrongs(
                results.PackageCSMs.FilteredCSMs
                    .Where(a => a.IsInterLink == true)
                    .Where(a => a.IsDecoy == false && a.IsLoopLink == false)
                    .Select(a => (a.AlphaPSM.Peptide.ProteinsString, a.BetaPSM.Peptide.ProteinsString)).ToList(),
                truth);
            int csm_errors_intra = CountWrongs(
                results.PackageCSMs.FilteredCSMs
                    .Where(a => a.IsInterLink == false)
                    .Where(a => a.IsDecoy == false && a.IsLoopLink == false)
                    .Select(a => (a.AlphaPSM.Peptide.ProteinsString, a.BetaPSM.Peptide.ProteinsString)).ToList(),
                truth);

            int rp_errors_inter = CountWrongs(
                results.PackageResPairs.FilteredResPairs
                    .Where(a => a.IsInterLink == true)
                    .Where(a => a.IsDecoy == false)
                    .Select(a => (a.LongestAlphaCSM.AlphaPSM.Peptide.ProteinsString, a.LongestBetaCSM.BetaPSM.Peptide.ProteinsString)).ToList(),
                truth);
            int rp_errors_intra = CountWrongs(
                results.PackageResPairs.FilteredResPairs
                    .Where(a => a.IsInterLink == false)
                    .Where(a => a.IsDecoy == false)
                    .Select(a => (a.LongestAlphaCSM.AlphaPSM.Peptide.ProteinsString, a.LongestBetaCSM.BetaPSM.Peptide.ProteinsString)).ToList(),
                truth);

            int ppi_errors_inter = CountWrongs(
               results.PackagePPIs.FilteredPPIs
                   .Where(a => a.IsInterLink == true)
                   .Where(a => a.IsDecoy == false)
                   .Select(a => (a.ResPairs[0].LongestAlphaCSM.AlphaPSM.Peptide.ProteinsString, a.ResPairs[0].LongestBetaCSM.BetaPSM.Peptide.ProteinsString)).ToList(),
               truth);
            int ppi_errors_intra = CountWrongs(
                results.PackagePPIs.FilteredPPIs
                    .Where(a => a.IsInterLink == false)
                    .Where(a => a.IsDecoy == false)
                    .Select(a => (a.ResPairs[0].LongestAlphaCSM.AlphaPSM.Peptide.ProteinsString, a.ResPairs[0].LongestBetaCSM.BetaPSM.Peptide.ProteinsString)).ToList(),
                truth);


            return (csm_errors_inter, csm_errors_intra, rp_errors_inter, rp_errors_intra, ppi_errors_inter, ppi_errors_intra);
        }

        private static int CountWrongs(List<(string alphaProteins, string betaProteins)> csm_prots, Dictionary<string, HashSet<string>> truth)
        {
            int ctt = 0;

            foreach ((string alphaProteins, string betaProteins) in csm_prots)
            {
                string[] allAlpha = alphaProteins.Split(';');
                string[] allBeta = betaProteins.Split(';');

                bool ok = false;

                foreach (var alpha in allAlpha)
                {
                    if (ok == true) break;
                    foreach (var beta in allBeta)
                    {
                        if (truth.ContainsKey(alpha)
                            && truth.ContainsKey(beta)
                            && truth[alpha].Contains(beta))
                        {
                            ok = true;
                            break;
                        }
                    }
                }

                if (ok == false)
                {
                    ctt++;
                }
            }

            return ctt;
        }

        public static Dictionary<string, HashSet<string>> GetTruth(string fileName)
        {
            var r = new Dictionary<string, HashSet<string>>();

            var lines = File.ReadAllLines(fileName);

            foreach (var line in lines)
            {
                var s = line.Split('+');

                var p1 = s[0];
                var p2 = s[1];

                if (r.ContainsKey(p1) == false)
                    r.Add(p1, new HashSet<string>() { p1 });

                if (r.ContainsKey(p2) == false)
                    r.Add(p2, new HashSet<string>() { p2 });

                r[p1].Add(p2);
                r[p2].Add(p1);
            }

            return r;
        }

        public static (List<IFDRElement> correct, List<IFDRElement> incorrect) GetCorrectAndIncorrectElements(List<IFDRElement> elements, Dictionary<string, HashSet<string>> truth)
        {
            (string alphaProteins, string betaProteins) GetProts(IFDRElement el, Type t)
            {
                if (t == typeof(ScoredCSM))
                {
                    return (((ScoredCSM)el).AlphaPSM.Peptide.ProteinsString, ((ScoredCSM)el).BetaPSM != null ? ((ScoredCSM)el).BetaPSM.Peptide.ProteinsString : "");
                }
                else if (t == typeof(ResPair))
                {
                    return (((ResPair)el).CSMs[0].AlphaPSM.Peptide.ProteinsString, ((ResPair)el).CSMs[0].BetaPSM.Peptide.ProteinsString);
                }
                else if (t == typeof(PPI))
                {
                    return (((PPI)el).ResPairs[0].CSMs[0].AlphaPSM.Peptide.ProteinsString, ((PPI)el).ResPairs[0].CSMs[0].BetaPSM.Peptide.ProteinsString);
                }
                throw new Exception("Unknown IFDRElement type");
            }


            Type t = elements.First().GetType();

            var correct = elements.Where(a =>
            {
                var (alphaProteins, betaProteins) = GetProts(a, t);

                string[] allAlpha = alphaProteins.Split(';');
                if (String.IsNullOrEmpty(betaProteins))//Looplink
                {
                    foreach (var alpha in allAlpha)
                    {
                        if (truth.ContainsKey(alpha))
                            return true;
                    }
                    return false;
                }
                else //Crosslink
                {
                    string[] allBeta = betaProteins.Split(';');

                    foreach (var alpha in allAlpha)
                    {
                        foreach (var beta in allBeta)
                        {
                            if (truth.ContainsKey(alpha)
                                && truth.ContainsKey(beta)
                                && truth[alpha].Contains(beta))
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                }
            }).ToList();

            var incorrect = elements.Except(correct).ToList();

            return (correct, incorrect);
        }



    }
}
