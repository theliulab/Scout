using MS.WindowsAPICodePack.Internal;
using ScoutCore;
using ScoutPostProcessing;
using ScoutPostProcessing.CSMLogic;
using ScoutPostProcessing.Output;
using ScoutPostProcessing.PPILogic;
using ScoutPostProcessing.ResiduePairLogic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using static Scout.TruthEvaluation.ControlTruthByGroup;
using static System.Net.WebRequestMethods;

namespace Scout.TruthEvaluation
{
    public static class Standardizer
    {


        private static (string alphaProteins, string betaProteins) GetProts(IFDRElement el, Type t)
        {
            if (t == typeof(ScoredCSM))
            {
                return (((ScoredCSM)el).AlphaPSM.Peptide.ProteinsString, ((ScoredCSM)el).BetaPSM.Peptide.ProteinsString);
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


       
        private static string GetTruthString(IFDRElement element, Dictionary<string, HashSet<string>> truth)
        {
            //results.append('BAD: Not allowed')
            //    bad_count += 1
            //    bad_not_allowed_count += 1
            //elif in_groups is False:
            //    results.append('BAD: Not in truth')


            var t = element.GetType();

            var (alphaProteins, betaProteins) = GetProts(element, t);

            string[] allAlpha = alphaProteins.Split(';');
            string[] allBeta = betaProteins.Split(';');

            if (allAlpha.All(a => !truth.ContainsKey(a)))
                return "BAD: Not in truth";

            if (allBeta.All(a => !truth.ContainsKey(a)))
                return "BAD: Not in truth";


            foreach (var alpha in allAlpha)
            {
                if (!truth.ContainsKey(alpha))
                    continue;

                foreach (var beta in allBeta)
                {
                    if (!truth.ContainsKey(beta))
                        continue;

                    if (truth[alpha].Contains(beta))
                        return "OK";
                }
            }

            return "BAD: Not allowed";
        }

        public static void SaveWithTruthColumns(
            string savePath,
            Dictionary<string, HashSet<string>> truth,
            List<IFDRElement> elements,
            ScoutParameters searchParams,
            PostProcessingParameters postParams)
        {
            var t = elements.First().GetType();

            var truthColumn = elements.Select(a => GetTruthString(a, truth)).ToList();

            if (t == typeof(ScoredCSM))
            {
                var csms = elements.Cast<ScoredCSM>().ToList();
                XLFilteredResultsSaver.SaveCSMsCSV(savePath, csms, searchParams, postParams);
            }
            else if (t == typeof(ResPair))
            {
                var rps = elements.Cast<ResPair>().ToList();
                XLFilteredResultsSaver.SaveResiduePairsCSV(savePath, rps, postParams);
            }
            else if (t == typeof(PPI))
            {
                var ppis = elements.Cast<PPI>().ToList();
                XLFilteredResultsSaver.SavePPIsCSV(savePath, ppis, postParams);
            }


            string[] lines = System.IO.File.ReadAllLines(savePath);
            lines[0] = lines[0] + ",Truth";
            for (int i = 1; i < lines.Length; i++)
            {
                lines[i] = lines[i] + $",{truthColumn[i - 1]}";
            }
            System.IO.File.WriteAllLines(savePath, lines);
        }
    }
}
