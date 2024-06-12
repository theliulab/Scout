using ScoutCore;
using ScoutCore.PSMEngines;
using ScoutCore.QueryLogic;
using ScoutCore.Results;
using ScoutPostProcessing.CSMLogic;
using ScoutPostProcessing.PPILogic;
using ScoutPostProcessing.ResiduePairLogic;
using ScoutPostProcessing.Scoring;
using PatternTools.MSParserLight;
using SpectrumWizard.Predictors.CleavableXL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Digestor;
using System.Reflection;
using ScoutCore.Scoring;
using ScoutPostProcessing.Output;
using System.Threading;

namespace ScoutPostProcessing
{
    public class XLPostProcessingFactory
    {
        private const string m_B000 = "MinUniqueRPsInInterProteins";
        private const string m_B001 = "MinUniqueRPsInIntraProteins";
        private ScoutParameters m_B002 { get; set; }
        private PostProcessingParameters m_B003 { get; set; }
        private bool m_B004;

        public string WarningMsg { get; set; }

        public XLPostProcessingFactory(
            ScoutParameters scoutParams,
            PostProcessingParameters parametersPost)
        {
            m_B002 = scoutParams;
            m_B003 = parametersPost;
            m_B004 = true;
            try
            {
                Console.Write("");
            }
            catch
            {
                m_B004 = false;
            }
        }

        public XLFilteredResults AssemblePostProcessingResults(
            CancellationToken token,
            string fastaPath,
            params FileInfo[] bufFiles)
        {
            string rawsLocation = bufFiles[0].DirectoryName;
            var sw = Stopwatch.StartNew();
            double m_A005;
            List<ScoredCSM> m_A001 = internal_method8(token, bufFiles, out m_A005);
            XLFilteredResults m_A004 = null;
            Console.WriteLine("INFO: Recomputing auxiliar scores...");
            internal_method1(m_A001);
            internal_method2(token, m_A001.Where(a => a.IsInterLink == true).ToList(), fastaPath);
            if (m_B003.MinUniqueRPsInInterProteins == true || m_B003.MinUniqueRPsInIntraProteins == true)
            {
                PostScoresHelper.AddCountMinUniqueRPsInPtnScore(m_A001, new List<(string, double)>() { ("PoissonScore", 2) }, m_B003.MinUniqueRPsInInterProteins, m_B003.MinUniqueRPsInIntraProteins);
                if (m_B003.MinUniqueRPsInInterProteins == false)
                {
                    m_B003.defaultCSMInterParams.Remove(m_B000);
                    m_B003.defaultCSMLooplinkParams.Remove(m_B000);
                    m_B003.ResPair_Features.Remove(m_B000);
                }
                if (m_B003.MinUniqueRPsInIntraProteins == false)
                {
                    m_B003.defaultCSMLooplinkParams.Remove(m_B001);
                    m_B003.defaultCSMInterParams.Remove(m_B001);
                    m_B003.ResPair_Features.Remove(m_B001);
                }
            }
            else
            {
                m_B003.defaultCSMInterParams = m_B003.defaultCSMInterParams.Where(a => a != m_B000 && a != m_B001).ToList();
                m_B003.defaultCSMLooplinkParams = m_B003.defaultCSMLooplinkParams.Where(a => a != m_B000 && a != m_B001).ToList();
                m_B003.ResPair_Features = m_B003.ResPair_Features.Where(a => a != m_B000 && a != m_B001).ToList();
            }
            if (m_B002.DontShowContaminants)
            {
                Console.WriteLine("INFO: Removing contaminants...");
                int removedContaminants = m_A001.Count();
                m_A001 = m_A001.Where(a => !a.PPI_ID.Contains("contaminant")).ToList();
                removedContaminants = removedContaminants - m_A001.Count();
            }
            m_A001 = internal_method0(token, m_A001);
            List<string> m_A006 = new();
            if (m_B002.SilacSearch == true && m_B002.SilacHybridMode == false) m_A001.RemoveAll(a => !String.IsNullOrEmpty(a.QuantitationTag) && a.QuantitationTag.StartsWith("SILAC-Hybrid"));
            var countLoopLinks = m_A001.Count(a => a.IsLoopLink == true);
            int m_A002 = m_A001.Count(a => a.IsDecoy == true);
            int m_A003 = m_A001.Count(a => a.IsDecoy == false);
            if (m_A002 == 0 || m_A003 == 0)
            {
                if (m_A002 == 0) Console.WriteLine(@"WARN: No decoy CSMs! Showing unfiltered results.");
                else Console.WriteLine(@"WARN: No target CSMs! Showing unfiltered results.");
                m_A004 = internal_method4(m_A001, fastaPath, bufFiles);
                m_A004.RawsFolder = rawsLocation;
                m_A005 += sw.Elapsed.TotalSeconds;
                m_A004.TotalProcessingTime = m_A005;
                return m_A004;
            }
            if (m_B003.UsePythonModels == true)
            {
                try
                {
                    m_A004 = internal_method5(token, m_A001, fastaPath, bufFiles);
                }
                catch (Exception ex)
                {
                    m_A006.Add("Python error! Results might differ!\nDefaulting to C# neural networks!");
                    Console.WriteLine("ERROR: Could not run Python pipeline!\n" + ex.Message + "\n" + ex.StackTrace);
                    Console.WriteLine($"********* Python error! Defaulting to C# neural networks *********");
                    m_B003.UsePythonModels = false;
                }
            }
            if (m_B003.UsePythonModels == false) m_A004 = internal_method7(token, m_A001, fastaPath, bufFiles);
            m_A004.RawsFolder = rawsLocation;
            m_A005 += sw.Elapsed.TotalSeconds;
            m_A004.TotalProcessingTime = m_A005;
            m_A006 = m_A006.Distinct().ToList();
            WarningMsg = String.Join("\n", m_A006);
            return m_A004;
        }

        private List<ScoredCSM> internal_method0(CancellationToken m_A000, List<ScoredCSM> m_A001)
        {
            string m_A0010 = m_B002.DecoyTag + "_";
            List<ScoredCSM> m_A008 = new();
            List<ScoredCSM> m_A009 = new();
            foreach (var m_A002 in m_A001)
            {
                if (m_A000.IsCancellationRequested) m_A000.ThrowIfCancellationRequested();
                if (m_A002.IsLoopLink || !m_A002.IsDecoy)
                {
                    m_A009.Add(m_A002);
                    continue;
                }
                var m_A003 = m_A002.AlphaPSM.Peptide.Mappings;
                var m_A004 = m_A002.BetaPSM.Peptide.Mappings;
                bool m_A005 = false;
                foreach (var m_A0011 in m_A003)
                {
                    foreach (var m_A0012 in m_A004)
                    {
                        string m_A006 = m_A0011.Locus.Replace(m_A0010, "");
                        string m_A007 = m_A0012.Locus.Replace(m_A0010, "");

                        if (m_A006 == m_A007)
                            m_A005 = true;
                    }
                }
                if (!m_A005) m_A009.Add(m_A002);
                else m_A008.Add(m_A002);
            }
            return m_A009;
        }
        private void internal_method1(List<ScoredCSM> m_A001)
        {
            if (m_A001 == null || m_A001.Count == 0) return;
            try
            {
                double m_A002 = (double)m_A001.Where(b => b.Scores["AlphaDeltaCN"] != null).Average(a => a.Scores["AlphaDeltaCN"]);
                double m_A003 = (double)m_A001.Where(b => b.Scores["BetaDeltaCN"] != null).Average(a => a.Scores["BetaDeltaCN"]);
                foreach (var m_A004 in m_A001)
                {
                    if (m_A004.IsLoopLink == false &&
                        m_A004.AlphaPSM.Peptide.AsCleanString == m_A004.BetaPSM.Peptide.AsCleanString)
                        m_A004.Scores["MinDDPScore"] = m_A004.Scores["AlphaScore"];

                    if (m_A004.Scores["AlphaDeltaCN"] == null)
                        m_A004.Scores["AlphaDeltaCN"] = m_A002;

                    if (m_A004.IsLoopLink == false &&
                        m_A004.Scores["BetaDeltaCN"] == null)
                        m_A004.Scores["BetaDeltaCN"] = m_A003;
                }
            }
            catch (Exception)
            {
            }
        }
        private void internal_method2(CancellationToken m_A001, List<ScoredCSM> m_A002, string m_A003)
        {
            var m_A004 = Digestor.Digestor.AssembleFasta(m_A003, m_B002.AssembleDigestionParemeters());
            m_A004.MyItems.ForEach(a =>
            {
                a.Sequence = a.Sequence.Replace("I", "#");
                a.Sequence = a.Sequence.Replace("L", "#");
            });
            int ctt = 0;
            Dictionary<string, string> m_A005 = null;
            try
            {
                m_A005 = m_A004.MyItems.ToDictionary(a => a.SequenceIdentifier, a => a.Sequence);
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: " + e.Message + "\n" + e.StackTrace);
                return;
            }
            foreach (var m_A006 in m_A002)
            {
                if (m_A001.IsCancellationRequested) m_A001.ThrowIfCancellationRequested();
                if (m_A006.IsInterLink == false) continue;
                var m_A007 = m_A006.AlphaPSM.Peptide.AsCleanString;
                var m_A008 = m_A006.BetaPSM.Peptide.AsCleanString;
                m_A007 = m_A007.Replace("I", "#");
                m_A007 = m_A007.Replace("L", "#");
                m_A008 = m_A008.Replace("I", "#");
                m_A008 = m_A008.Replace("L", "#");
                var m_A009 = m_A006.AlphaPSM.Peptide.Mappings;
                var m_A0010 = m_A006.BetaPSM.Peptide.Mappings;
                foreach (var m_A0013 in m_A009.ToList())
                {
                    foreach (var m_A0014 in m_A0010.ToList())
                    {
                        string m_A0011 = "";
                        string m_A0012 = "";
                        if (!m_A005.TryGetValue(m_A0013.Locus, out m_A0011)) continue;
                        if (!m_A005.TryGetValue(m_A0014.Locus, out m_A0012)) continue;
                        int m_A0015 = m_A0012.IndexOf(m_A007);
                        int m_A0016 = m_A0011.IndexOf(m_A008);
                        if (m_A0015 != -1 && m_A0016 != -1) { }
                        if (m_A0015 != -1)
                        {
                            internal_method3(m_A006.AlphaPSM, m_A0014, m_A0015);
                            m_A006.RefreshMappingsAndDecoyClass(m_B003.GroupedByGene);
                            m_A006.IsInterLink = false;
                            ctt++;
                        }
                        if (m_A0016 != -1)
                        {
                            internal_method3(m_A006.BetaPSM, m_A0013, m_A0016);
                            m_A006.RefreshMappingsAndDecoyClass(m_B003.GroupedByGene);
                            m_A006.IsInterLink = false;
                            ctt++;
                        }
                    }
                }
            }
        }
        private void internal_method3(CleavePSM m_A001, PeptideMapping m_A002, int m_A003)
        {
            if (m_A001.Peptide.Mappings.Exists(a => a.Locus == m_A002.Locus)) return;
            m_A001.Peptide.Mappings.Add(new PeptideMapping(m_A002.ProteinFastaIndex, m_A003, m_A001.Peptide.Length) { Locus = m_A002.Locus, Gene = m_A002.Gene });
            if (!m_A002.Locus.StartsWith(m_B002.DecoyTag)) m_A001.Peptide.Mappings.RemoveAll(a => a.Locus.StartsWith(m_B002.DecoyTag));
            m_A001.Peptide.IsDecoy = !m_A001.Peptide.Mappings.Any(a => !a.Locus.StartsWith(m_B002.DecoyTag));
        }
        private XLFilteredResults internal_method4(List<ScoredCSM> m_A001, string m_A002, FileInfo[] m_A003)
        {
            var m_A004 = new PythonScout(m_B002, m_B003, m_A003);
            var m_A005 = new CSMPackage(
                m_A001.Where(a => a.IsLoopLink == false).OrderByDescending(a => a.ClassificationScore).ToList(),
                m_B003.FDRMode,
                m_B003.CSM_FDR);
            var m_A006 = new CSMPackage(
                m_A001.Where(a => a.IsLoopLink == true).OrderByDescending(a => a.ClassificationScore).ToList(),
                m_B003.FDRMode,
                m_B003.CSM_FDR);
            Dictionary<string, int> m_A007 = internal_method6(m_A005.FilteredCSMs);
            var m_A008 = ResiduePairHelper.CSMsToRespairs(m_A005.FilteredCSMs, m_B003);
            var m_A009 = new ResiduePairPackage(
                m_A008.OrderByDescending(a => a.ClassificationScore).ToList(),
                m_B003.FDRMode,
                m_B003.ResPair_FDR);
            var m_A0010 = PPIHelper.ResPairsToPPI(m_A009.FilteredResPairs, m_B003);
            var m_A0011 = new PPIPackage(
                m_A0010.OrderByDescending(a => a.ClassificationScore).ToList(),
                m_B003.FDRMode,
                m_B003.PPI_FDR);

            var r = new XLFilteredResults(
                m_A003.Select(a => a.FullName).ToArray(),
                m_A002,
                m_A005,
                m_A006,
                m_A009,
                m_A0011,
                m_A007,
                m_B002,
                m_B003,
                internal_method11());

            return r;
        }
        private XLFilteredResults internal_method5(CancellationToken m_A001, List<ScoredCSM> m_A002, string m_A003, FileInfo[] m_A004)
        {
            var m_A005 = new PythonScout(m_B002, m_B003, m_A004);
            if (m_A001.IsCancellationRequested) m_A001.ThrowIfCancellationRequested();
            Console.WriteLine("INFO: Creating CSM package for crosslinks...");
            CSMPackage m_A006 = m_A005.GetCSMPackage(m_A002.Where(a => a.IsLoopLink == false).ToList());
            if (m_A001.IsCancellationRequested) m_A001.ThrowIfCancellationRequested();
            Console.WriteLine("INFO: Creating CSM package for looplinks...");
            CSMPackage m_A007 = m_A005.GetCSMPackage(m_A002.Where(a => a.IsLoopLink == true).ToList(), true);
            m_A007.FDRMode = FDRModes.CombinedIntraInter;
            Console.WriteLine("INFO: Computing protein scores...");
            Dictionary<string, int> m_A008 = internal_method6(m_A006.FilteredCSMs);
            Console.WriteLine("INFO: Grouping CSMs into residue pairs...");
            List<ScoredCSM> m_A009 = m_B003.Independent_FDR_Control == true ? m_A009 = m_A006.AllCSMs : m_A006.FilteredCSMs;
            if (m_A001.IsCancellationRequested) m_A001.ThrowIfCancellationRequested();
            List<ResPair> m_A0010 = ResiduePairHelper.CSMsToRespairs(m_A009, m_B003);
            Console.WriteLine("INFO: Creating Residue Pair package...");
            ResiduePairPackage m_A0011 = m_A005.GetResPairPackage(m_A0010);
            if (m_A001.IsCancellationRequested) m_A001.ThrowIfCancellationRequested();
            Console.WriteLine("INFO: Grouping residue pairs into PPIs...");
            List<ResPair> m_A0012 = m_B003.Independent_FDR_Control == true ? m_A0012 = m_A0011.AllResPairs : m_A0011.FilteredResPairs;
            List<PPI> m_A0013 = PPIHelper.ResPairsToPPI(m_A0012, m_B003);
            Console.WriteLine("INFO: Creating PPI package...");
            PPIPackage m_A0014 = m_A005.GetPPIPackage(m_A0013, m_B003.UniquePPIsOnly);

            XLFilteredResults r = new XLFilteredResults(
                m_A004.Select(a => a.FullName).ToArray(),
                m_A003,
                m_A006,
                m_A007,
                m_A0011,
                m_A0014,
                m_A008,
                m_B002,
                m_B003,
                internal_method11());

            return r;
        }
        private Dictionary<string, int> internal_method6(List<ScoredCSM> m_A001)
        {
            var dict000 = new Dictionary<string, int>();
            foreach (var m_A002 in m_A001)
            {
                var m_A003 = m_A002.BetaMappings != null ? m_A002.AlphaMappings.Concat(m_A002.BetaMappings).Distinct() : m_A002.AlphaMappings;//BetaMapping is null if csm is looplink
                foreach (var m_A004 in m_A003)
                {
                    if (dict000.ContainsKey(m_A004.Locus)) dict000[m_A004.Locus] += 1;
                    else dict000.Add(m_A004.Locus, 1);
                }
            }
            return dict000;
        }
        private XLFilteredResults internal_method7(CancellationToken m_A001, List<ScoredCSM> m_A002, string m_A003, FileInfo[] m_A004)
        {
            var m_A005 = new CSMFilter(m_B003.CSM_Features);
            if (m_A001.IsCancellationRequested) m_A001.ThrowIfCancellationRequested();
            var m_A006 = m_A005.Filter(
                m_A002.Where(a => a.IsLoopLink == false).ToList(),
                m_B003.CSM_FDR,
                m_B003.FDRMode);
            if (m_A001.IsCancellationRequested) m_A001.ThrowIfCancellationRequested();
            var m_A007 = m_A005.Filter(
               m_A002.Where(a => a.IsLoopLink == true).ToList(),
               m_B003.CSM_FDR,
               m_B003.FDRMode);
            var m_A008 = ResiduePairHelper.CSMsToRespairs(
                m_A006.FilteredCSMs.Where(a => !a.IsLoopLink).ToList(),
                m_B003);
            var respairFilter = new ResiduePairFilter(
                m_B003.ResPair_Features);
            if (m_A001.IsCancellationRequested) m_A001.ThrowIfCancellationRequested();
            var m_A009 = respairFilter.Filter(
                m_A008,
                m_B003.ResPair_FDR,
                m_B003.FDRMode);
            var m_A0010 = PPIHelper.ResPairsToPPI(
                m_A009.FilteredResPairs,
                m_B003);
            var m_A0011 = new PPIFilter(
                m_B003.PPI_Features,
                null,
                m_B003.UniquePPIsOnly);
            if (m_A001.IsCancellationRequested) m_A001.ThrowIfCancellationRequested();
            var m_A0012 = m_A0011.Filter(
                m_A0010,
                m_B003.PPI_FDR,
                m_B003.FDRMode);
            var m_A0013 = internal_method6(m_A006.FilteredCSMs.Where(a => !a.IsLoopLink).ToList());

            var r = new XLFilteredResults(
                m_A004.Select(a => a.FullName).ToArray(),
                m_A003,
                m_A006,
                m_A007,
                m_A009,
                m_A0012,
                m_A0013,
                m_B002,
                m_B003,
                internal_method11());

            return r;
        }
        private List<ScoredCSM> internal_method8(CancellationToken m_A001, FileInfo[] m_A002, out double m_A003)
        {
            double m_A005 = 0;
            List<ScoredCSM> m_A004 = new();
            m_B002 = null;
            int m_A006 = m_A002.Length;
            for (int _index = 0; _index < m_A002.Length; _index++)
            {
                if (m_A001.IsCancellationRequested) m_A001.ThrowIfCancellationRequested();
                Console.WriteLine($"==============\nFiltering {(_index + 1)} / {m_A006} File {m_A002[_index].FullName}\n==============");
                ScoutRawResults m_A007 = ScoutRawResults.Load(m_A001, m_A002[_index].FullName);
                if (m_B002 == null) m_B002 = m_A007._Params.Copy(m_A007._Params);
                m_A005 += m_A007.ProcessingTime;
                if (String.IsNullOrEmpty(m_A007.RawFile)) throw new Exception($"Raw file for scout buf {m_A002[_index].FullName} not found.");
                if (m_A001.IsCancellationRequested) m_A001.ThrowIfCancellationRequested();
                List<ScoredCSM> m_A008 = internal_method10(m_A007, _index);
                m_A004.AddRange(m_A008);
            }
            m_A003 = m_A005;
            return m_A004;
        }
        private void internal_method9(ScanResults m_A000, string m_A001, int m_A002, List<ScoredCSM> m_A003, int m_A004)
        {
            try
            {
                if (m_A000.CleaveCandidates == null || m_A000.CleaveCandidates.Count == 0) return;
                List<ICleaveCandidate> m_A005 = m_A000.GetBestCleaveCandidates(1);
                if (m_A005 == null || m_A005.Count == 0) return;
                var m_A006 = m_A005.MaxBy(a => a.XLScore);
                if (m_A006.AllLightPSMs == null || m_A006.AllLightPSMs.Count == 0) return;
                if (!m_A006.IsLoopLink && (m_A006.AllHeavyPSMs == null || m_A006.AllHeavyPSMs.Count == 0)) return;
                ScoredCSM m_A007 = null;
                if (!m_A006.IsLoopLink)
                {
                    var m_A008 = (CleavePSM)m_A006.SelectedPSMs.heavy.psm;
                    if (m_A008 == null) return;
                    var m_A009 = m_A006.AllHeavyPSMs.Cast<CleavePSM>().ToList();
                    var m_A0010 = (mh: m_A006.HeavyPairMH, intensity: m_A006.HeavyPairIntensity);
                    var m_A0011 = (CleavePSM)m_A006.SelectedPSMs.light.psm;
                    if (m_A0011 == null) return;
                    var m_A0012 = m_A006.AllLightPSMs.Cast<CleavePSM>().ToList();
                    var m_A0013 = (mh: m_A006.LightPairMH, intensity: m_A006.LightPairIntensity);
                    bool m_A0014 = false;
                    if (m_A0011.Peptide.Length > m_A008.Peptide.Length) m_A0014 = true;
                    else if (m_A0011.Peptide.Length == m_A008.Peptide.Length)
                    {
                        var m_A0015 = new List<string>() {
                            m_A008.Peptide.AsCleanString,
                            m_A0011.Peptide.AsCleanString
                        };
                        var m_A0016 = m_A0015.OrderBy(a => a).ToList();
                        if (m_A0016[0] != m_A0015[0])
                            m_A0014 = true;
                    }
                    if (m_A0014)
                    {
                        var aux1 = m_A008;
                        m_A008 = m_A0011;
                        m_A0011 = aux1;
                        var aux2 = m_A0010;
                        m_A0010 = m_A0013;
                        m_A0013 = aux2;
                        var aux3 = m_A009;
                        m_A009 = m_A0012;
                        m_A0012 = aux3;
                    }

                    m_A007 = new ScoredCSM(
                        m_A008,
                        m_A0011,
                        m_A001,
                        m_A002,
                        m_A000,
                        m_A0010,
                        m_A0013,
                        m_B002.CXLReagent,
                        1,
                        ((CleaveCandidate)m_A006).TotalPairIsotopes,
                        null,
                        m_B003.GroupedByGene);
                }
                else
                {
                    var m_A0013 = m_A006.AllLightPSMs.Cast<CleavePSM>().ToList();
                    if (m_A0013 == null) return;
                    var m_A0014 = (CleavePSM)m_A006.SelectedPSMs.light.psm;
                    if (m_A0014 == null) return;
                    var m_A0015 = (mh: m_A006.LightPairMH, intensity: m_A006.LightPairIntensity);

                    m_A007 = new ScoredCSM(
                       m_A0014,
                       m_A001,
                       m_A002,
                       m_A000,
                       m_A0015,
                       m_B002.CXLReagent,
                       1,
                       ((CleaveCandidate)m_A006).TotalPairIsotopes,
                       null,
                       m_B003.GroupedByGene);
                }
                m_A007.Scores = m_A000.Scores;
                m_A007.Spectrum = m_A000.Spectrum;
                lock (m_A003)
                {
                    m_A003.Add(m_A007);
                    m_A004++;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: ScoreCSMsFromSearchResults method.\nScan: " + m_A004 + "\n" + e.StackTrace + "\n" + e.Message);
                throw;
            }
        }
        private List<ScoredCSM> internal_method10(ScoutRawResults m_A000, int m_A001)
        {
            int processed_line = 0;
            int old_progress = 0;
            double lengthFile = m_A000.ScanResults.Count();

            List<ScoredCSM> m_A002 = new();
            var m_A003 = new ScoutCore.Scoring.XLScoresHelper(m_B002);
            if (m_B004) Console.WriteLine($"INFO: Assembling and scoring CSMs for {m_A000.RawFile}...");

            if (m_A000.ScanResults != null)
            {
                int m_A004 = 0;
                if (m_A000._Params.ParallelPSMs == false)
                {
                    foreach (ScanResults m_A005 in m_A000.ScanResults.Values)
                    {
                        if (m_B004)
                        {
                            processed_line++;
                            int new_progress = (int)((double)processed_line / (lengthFile) * 100);
                            if (new_progress > old_progress)
                            {
                                old_progress = new_progress;
                                Console.Write("Progress: " + old_progress + "%");
                            }
                            internal_method9(m_A005, m_A000.RawFile, m_A001, m_A002, m_A004);
                        }
                    }
                }
                else
                {
                    int m_A006 = Environment.ProcessorCount - 2;
                    Parallel.ForEach(m_A000.ScanResults.Values, new ParallelOptions() { MaxDegreeOfParallelism = m_A006 }, (scan) => internal_method9(scan, m_A000.RawFile, m_A001, m_A002, m_A004));
                }
            }
            m_A002 = m_A002.OrderBy(a => a.FileName).ThenBy(a => a.ScanNumber).ToList();
            return m_A002;
        }
        private string internal_method11()
        {
            Version? m_A000 = null;
            try
            {
                m_A000 = Assembly.GetExecutingAssembly()?.GetName()?.Version;
            }
            catch (Exception e)
            {
                //Unable to retrieve version number
                Console.WriteLine("", e);
                return "";
            }
            return m_A000.Major + "." + m_A000.Minor + "." + m_A000.Build;
        }
    }
}
