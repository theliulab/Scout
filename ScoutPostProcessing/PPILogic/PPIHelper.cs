using Newtonsoft.Json;
using ScoutCore.PSMEngines;
using ScoutPostProcessing.CSMLogic;
using ScoutPostProcessing.ResiduePairLogic;
using ScoutPostProcessing.Scoring;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ScoutPostProcessing.PPILogic
{
    internal class PPIHelper
    {
        static PostProcessingParameters _postParams { get; set; }

        internal static List<PPI> ResPairsToPPI
            (List<ResPair> respairs,
            PostProcessingParameters postParams)
        {
            List<PPI> ppis = new();
            _postParams = postParams;

            var grouped = respairs.GroupBy(a => a.PPI_ID);
            var relationalScores = internal_method(respairs);

            foreach (IGrouping<string, ResPair> group in grouped)
            {
                var newPPI = new PPI(group.Key, group.ToList());
                newPPI.Scores = PostScoresHelper.ScorePPI(
                    newPPI,
                    relationalScores,
                    postParams);
                ppis.Add(newPPI);
            }

            return ppis;
        }

        private static Dictionary<string, Dictionary<string, HashSet<string>>> internal_method(List<ResPair> respairs)
        {
            var dict000 = new Dictionary<string, Dictionary<string, HashSet<string>>>();

            foreach (var rp in respairs)
            {
                string pep1 = rp.CSMs.First().AlphaPSM.Peptide.AsCleanString;
                string pep2 = rp.CSMs.First().BetaPSM.Peptide.AsCleanString;

                string prot1 = rp.CSMs.First().AlphaPSM.Peptide.ProteinsString;
                string prot2 = rp.CSMs.First().BetaPSM.Peptide.ProteinsString;

                if (!dict000.ContainsKey(prot1))
                    dict000.Add(prot1, new() { { pep1 , new() { prot2 } } } );
                else
                {
                    if (!dict000[prot1].ContainsKey(pep1))
                        dict000[prot1].Add(pep1, new() { prot2 });
                    else
                        dict000[prot1][pep1].Add(prot2);
                }

                if (!dict000.ContainsKey(prot2))
                    dict000.Add(prot2, new() { { pep2, new() { prot1 } } });
                else
                {
                    if (!dict000[prot2].ContainsKey(pep2))
                        dict000[prot2].Add(pep2, new() { prot1 });
                    else
                        dict000[prot2][pep2].Add(prot1);
                }
            }
            return dict000;
        }

        internal static string GetPPIIdentifier(CleavePSM psm1, CleavePSM psm2, bool groupByGene = false)
        {
            var l = new List<string>()
            {
                GetProteinString(psm1, groupByGene),
                GetProteinString(psm2, groupByGene),
            };

            l = l.OrderBy(a => a).ToList();
            return $"{l[0]}+{l[1]}";
        }

        internal static string GetProteinString(CleavePSM psm, bool groupByGene = false)
        {
            return String.Join(";", psm.Peptide.Mappings.OrderBy(a => a.Locus).Select(a => groupByGene ? a.Gene : a.Locus));
        }
    }
}