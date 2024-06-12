using Digestor;
using PatternTools.FastaTools;
using ScoutCore.PSMEngines;
using ScoutPostProcessing.CSMLogic;
using ScoutPostProcessing.Scoring;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScoutPostProcessing.ResiduePairLogic
{
    internal static class ResiduePairHelper
    {
        internal static string GetResidueFromPSM(CleavePSM psm)
        {
            string r = string.Join(";",
                psm.Peptide.Mappings.Select(mapping =>
                {
                    int proteinPosition = psm.ReagentPosition1 + mapping.ProteinPosition;

                    return $"{mapping.Locus}-{psm.Peptide.AsCleanString[psm.ReagentPosition1]}{proteinPosition}";
                }));

            return r;
        }

        internal static string GetResiduePairIdentifier(
            CleavePSM psm1,
            CleavePSM psm2)
        {
            string residue1 = psm1.Peptide.AsCleanString;
            string residue2 = psm2.Peptide.AsCleanString;

            var s = (new List<string>()
            {
                residue1,
                residue2
            })
            .OrderBy(a => a)
            .ToList();

            return $"{s[0]}+{s[1]}";

        }

        internal static List<ResPair> CSMsToRespairs(
            List<ScoredCSM> csms, 
            PostProcessingParameters postParams)
        {
            List<ResPair> rps = new();

            var grouped = csms.GroupBy(a => a.ResPair_ID);

            foreach (IGrouping<string, ScoredCSM> group in grouped)
            {

                var newResPair = new ResPair(
                    group.Key,
                    group.ToList());

                newResPair.Scores = PostScoresHelper.ScoreResPair(newResPair, postParams);

                rps.Add(newResPair);
            }

            return rps;
        }
    }
}
