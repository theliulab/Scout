using ScoutPostProcessing.CSMLogic;
using ScoutPostProcessing.PPILogic;
using ScoutPostProcessing.ResiduePairLogic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ScoutPostProcessing.Scoring
{
    internal static class PostScoresHelper
    {
        private const string m_B000 = "MinUniqueRPsInInterProteins";
        private const string m_B001 = "MinUniqueRPsInIntraProteins";
        internal enum ThresholdTypes
        {
            Lower,
            LowerOrEqual,
            Higher,
            HigherOrEqual
        }
        public static void AddCountMinUniqueRPsInPtnScore(
            List<ScoredCSM> m_A001,
            List<(string, double)> m_A002,
            bool m_A003 = true,
            bool m_A004 = true)
        {
            if (m_A004 == true) internal_method2(m_A001, m_A002);
            if (m_A003 == true) internal_method3(m_A001, m_A002);
        }

        public static Dictionary<string, double?> ScorePPI(
           PPI m_A00A1,
           Dictionary<string, Dictionary<string, HashSet<string>>> m_A00A2,
           PostProcessingParameters m_A00A3)
        {
            var dict000 = new Dictionary<string, double?>
            {
                { "ResPairCount", m_A00A1.ResPairs.Count() },
                { "BestClassificationScore", m_A00A1.ResPairs.Max(a => a.ClassificationScore) }
            };
            if (m_A00A3.MinUniqueRPsInInterProteins == true) dict000.Add(m_B000, m_A00A1.ResPairs.First().CSMs.First().Scores[m_B000]);
            if (m_A00A3.MinUniqueRPsInIntraProteins == true) dict000.Add(m_B001, m_A00A1.ResPairs.First().CSMs.First().Scores[m_B001]);
            float m_A00A4 = 0;
            float m_A00A5 = 0;
            float m_A00A6 = 0;
            float m_A00A7 = 0;
            string m_A00A8 = m_A00A1.ResPairs.First().CSMs.First().AlphaPSM.Peptide.ProteinsString;
            string m_A00A9 = m_A00A1.ResPairs.First().CSMs.First().BetaPSM.Peptide.ProteinsString;
            if (m_A00A8 != m_A00A9)
            {
                m_A00A6 = m_A00A2[m_A00A8].Count();
                m_A00A7 = m_A00A2[m_A00A9].Count();
            }
            else
            {
                m_A00A6 = m_A00A2[m_A00A8].Count() / 2f;
                m_A00A7 = m_A00A2[m_A00A9].Count() / 2f;
            }
            foreach (var m_A00A10 in m_A00A2[m_A00A8].Keys)
            {
                if (m_A00A2[m_A00A8][m_A00A10].Contains(m_A00A9))
                {
                    if (m_A00A8 != m_A00A9) m_A00A4 += 1;
                    else m_A00A4 += 0.5f;
                }
            }
            foreach (var m_A00A10 in m_A00A2[m_A00A9].Keys)
            {
                if (m_A00A2[m_A00A9][m_A00A10].Contains(m_A00A8))
                {
                    if (m_A00A8 != m_A00A9) m_A00A5 += 1;
                    else m_A00A5 += 0.5f;
                }
            }
            dict000.Add("UniquePepsA_B", m_A00A4);
            dict000.Add("UniquePepsB_A", m_A00A5);
            dict000.Add("UniquePepsA_Any", m_A00A6);
            dict000.Add("UniquePepsB_Any", m_A00A7);
            return dict000;
        }

        public static Dictionary<string, double?> ScoreResPair(ResPair m_A001, PostProcessingParameters m_A002)
        {
            var dict000 = new Dictionary<string, double?>();
            dict000.Add("TopCSMScore", internal_method1(m_A001.CSMs.Select(a => (double)a.ClassificationScore).ToList()));
            dict000.Add("BestMinScore", internal_method1(m_A001.CSMs.Select(a => (double)a.Scores["MinScore"]).ToList()));
            dict000.Add("BestPoisson", internal_method1(m_A001.CSMs.Select(a => (double)a.Scores["PoissonScore"]).ToList()));
            if (m_A002.MinUniqueRPsInInterProteins == true) dict000.Add(m_B000, internal_method1(m_A001.CSMs.Select(a => (double)a.Scores[m_B000]).ToList()));
            if (m_A002.MinUniqueRPsInIntraProteins == true) dict000.Add(m_B001, internal_method1(m_A001.CSMs.Select(a => (double)a.Scores[m_B001]).ToList()));

            return dict000;
        }

        public static List<ScoredCSM> ApplyThresholdFilters(
            List<ScoredCSM> m_A002,
            ScoutCore.ScoutParameters m_A003,
            PostProcessingParameters m_A004)
        {
            List<ScoredCSM> m_A000 = internal_method0(
                    m_A002.Where(a => !a.IsLoopLink).ToList(),
                    new List<(string score, double value, ThresholdTypes thresholdType)>()
                    {
                    ("PrecursorPPM", m_A003.PPMMS1Tolerance, ThresholdTypes.LowerOrEqual),
                    });
            m_A002 = m_A000.Concat(m_A002.Where(a => a.IsLoopLink)).ToList();
            if (m_A004.ApplyPostProcessingFilters)
            {
                List<(string, double, ThresholdTypes)> m_A001 = new();
                if (m_A004.Apply_DiffPPM_Threshold == true)
                    m_A001.Add(("DiffPPM", m_A004.DiffPPM_Threshold, ThresholdTypes.LowerOrEqual));
                m_A001.Add(("PoissonScore", m_A004.PoissonScore_Threshold, ThresholdTypes.HigherOrEqual));
                m_A001.Add(("MinDDPScore", m_A004.MinDDPScore_Threshold, ThresholdTypes.HigherOrEqual));
                m_A000 = internal_method0(
                    m_A002.Where(a => !a.IsLoopLink).ToList(),
                    m_A001);

                m_A002 = m_A000.Concat(m_A002.Where(a => a.IsLoopLink)).ToList();
            }
            return m_A002;
        }

        private static List<ScoredCSM> internal_method0(List<ScoredCSM> m_A001, List<(string, double, ThresholdTypes)> m_A002)
        {
            var m_A003 = m_A001.Where(a => a.IsDecoy == false).ToList();
            var m_A004 = m_A001.Where(a => a.IsDecoy == true).ToList();
            foreach (var m_A005 in m_A002)
            {
                m_A003 = m_A005.Item3 switch
                {
                    ThresholdTypes.Lower => m_A003.Where(a => a.Scores[m_A005.Item1] < m_A005.Item2).ToList(),
                    ThresholdTypes.LowerOrEqual => m_A003.Where(a => a.Scores[m_A005.Item1] <= m_A005.Item2).ToList(),
                    ThresholdTypes.Higher => m_A003.Where(a => a.Scores[m_A005.Item1] > m_A005.Item2).ToList(),
                    ThresholdTypes.HigherOrEqual => m_A003.Where(a => a.Scores[m_A005.Item1] >= m_A005.Item2).ToList(),
                    _ => throw new Exception("Unknown ThresholdType"),
                };
            }
            var r = m_A003.Concat(m_A004).ToList();
            r = r.OrderBy(a => a.FileName).ThenBy(a => a.ScanNumber).ToList();
            return r;
        }
        private static double internal_method1(List<double> m_A001)
        {
            return Math.Sqrt(((from score in m_A001.AsParallel()
                               select Math.Pow((double)score, 2)).Sum() / m_A001.Count));
        }
        private static void internal_method2(List<ScoredCSM> m_A001, List<(string, double)> m_A002)
        {
            Dictionary<string, Dictionary<string, int>> dict000 = new();
            foreach (ScoredCSM m_A003 in m_A001)
            {
                if (m_A003.IsLoopLink)
                {
                    m_A003.Scores[m_B001] = 2;
                    continue;
                }
                string m_A004 = m_A003.PPI_ID;
                if (dict000.ContainsKey(m_A004)) continue;
                string m_A005 = PPIHelper.GetProteinString(m_A003.AlphaPSM, false);
                string m_A006 = string.Empty;
                if (!m_A003.IsLoopLink) m_A006 = PPIHelper.GetProteinString(m_A003.BetaPSM, false);
                dict000.Add(m_A004, new Dictionary<string, int>());
                if (m_A005 != m_A006)
                {
                    dict000[m_A004].Add(m_A005, 0);
                    dict000[m_A004].Add(m_A006, 0);
                }
                else dict000[m_A004].Add(m_A005, 0);
            }
            foreach (ScoredCSM m_A007 in m_A001)
            {
                string m_A008 = m_A007.PPI_ID;
                if (!dict000.ContainsKey(m_A008)) continue;
                string m_A009 = PPIHelper.GetProteinString(m_A007.AlphaPSM, false);
                string m_A0010 = string.Empty;
                if (!m_A007.IsLoopLink)
                    m_A0010 = PPIHelper.GetProteinString(m_A007.BetaPSM, false);
                if (m_A009 != m_A0010)
                    if (m_A0010 != "") continue;
                string m_A0011 = ResiduePairHelper.GetResidueFromPSM(m_A007.AlphaPSM);
                string m_A0012 = string.Empty;
                if (!m_A007.IsLoopLink) m_A0012 = ResiduePairHelper.GetResidueFromPSM(m_A007.BetaPSM);
                bool m_A013 = false;
                foreach ((string score, double minValue) in m_A002)
                {
                    if (m_A007.Scores[score] < minValue)
                    {
                        m_A013 = true;
                        break;
                    }
                }
                if (m_A013) continue;
                if (m_A0011 == m_A0012) continue;
                dict000[m_A008][m_A009]++;
                if (!m_A007.IsLoopLink) dict000[m_A008][m_A0010]++;
            }
            foreach (ScoredCSM m_A0015 in m_A001)
            {
                string m_A0016 = m_A0015.PPI_ID;
                if (!dict000.ContainsKey(m_A0016)) continue;
                string m_AA0017 = PPIHelper.GetProteinString(m_A0015.AlphaPSM, false);
                string m_A0018 = string.Empty;
                if (!m_A0015.IsLoopLink) m_A0018 = PPIHelper.GetProteinString(m_A0015.BetaPSM, false);
                double m_A0019 = dict000[m_A0016][m_AA0017];
                double m_A0020 = m_A0019;
                if (!m_A0015.IsLoopLink) m_A0020 = dict000[m_A0016][m_A0018];
                m_A0015.Scores[m_B001] = Math.Min(m_A0019, m_A0020);
            }
        }
        private static void internal_method3(List<ScoredCSM> m_A001, List<(string, double)> m_A002)
        {
            Dictionary<string, Dictionary<string, HashSet<string>>> dict000 = new();
            foreach (ScoredCSM m_A003 in m_A001)
            {
                if (m_A003.IsLoopLink)
                {
                    m_A003.Scores[m_B000] = 2;
                    continue;
                }
                string m_A004 = m_A003.PPI_ID;
                if (dict000.ContainsKey(m_A004)) continue;
                string m_A005 = PPIHelper.GetProteinString(m_A003.AlphaPSM, false);
                string m_A006 = string.Empty;
                if (!m_A003.IsLoopLink) m_A006 = PPIHelper.GetProteinString(m_A003.BetaPSM, false);
                dict000.Add(m_A004, new Dictionary<string, HashSet<string>>());
                if (m_A005 != m_A006)
                {
                    dict000[m_A004].Add(m_A005, new HashSet<string>());
                    dict000[m_A004].Add(m_A006, new HashSet<string>());
                }
                else dict000[m_A004].Add(m_A005, new HashSet<string>());
            }
            foreach (ScoredCSM m_A007 in m_A001)
            {
                string m_A008 = m_A007.PPI_ID;
                if (!dict000.ContainsKey(m_A008)) continue;
                string m_A009 = PPIHelper.GetProteinString(m_A007.AlphaPSM, false);
                string m_A0010 = string.Empty;
                if (!m_A007.IsLoopLink) m_A0010 = PPIHelper.GetProteinString(m_A007.BetaPSM, false);
                string m_A0011 = ResiduePairHelper.GetResidueFromPSM(m_A007.AlphaPSM);
                string m_A0012 = string.Empty;
                if (!m_A007.IsLoopLink) m_A0012 = ResiduePairHelper.GetResidueFromPSM(m_A007.BetaPSM);
                bool m_A0013 = false;
                foreach ((string, double) v in m_A002)
                {
                    if (m_A007.Scores[v.Item1] < v.Item2)
                    {
                        m_A0013 = true;
                        break;
                    }
                }
                if (m_A0013) continue;
                if (m_A0011 == m_A0012) continue;
                dict000[m_A008][m_A009].Add(m_A0011);
                if (!m_A007.IsLoopLink) dict000[m_A008][m_A0010].Add(m_A0012);
            }
            foreach (ScoredCSM m_A0013 in m_A001)
            {
                string m_A0014 = m_A0013.PPI_ID;
                if (!dict000.ContainsKey(m_A0014)) continue;
                string m_A0015 = PPIHelper.GetProteinString(m_A0013.AlphaPSM, false);
                string m_A0016 = string.Empty;
                if (!m_A0013.IsLoopLink) m_A0016 = PPIHelper.GetProteinString(m_A0013.BetaPSM, false);
                double m_A0017 = dict000[m_A0014][m_A0015].Count();
                double m_A0018 = m_A0017;
                if (!m_A0013.IsLoopLink) m_A0018 = dict000[m_A0014][m_A0016].Count();
                double m_A0019 = Math.Min(m_A0017, m_A0018);
                if (m_A0015 == m_A0016) m_A0019 = Math.Floor(m_A0019 / 2);
                m_A0013.Scores[m_B000] = m_A0019;
            }
        }
    }
}
