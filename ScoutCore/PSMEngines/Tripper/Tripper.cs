using Digestor;
using ScoutCore.QueryLogic;
using ScoutCore.Results;
using ScoutCore.SpectraOperations;
using PatternTools.MSParserLight;
using PatternTools.PTMMods;
using SpectrumWizard;
using SpectrumWizard.Predictors.CleavableXL;
using SpectrumWizard.Predictors.Linear;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ScoutCore.Scoring;
using CSMSL.IO;
using PatternTools.FastaTools;
using System.Xml.XPath;
using Newtonsoft.Json.Linq;
using SpectrumWizard.Utils;

namespace ScoutCore.PSMEngines.Tripper
{
    public class Tripper : IEngine
    {
        private bool printToConsole = true;
        private bool _hasConsole = true;

        private ScoutParameters m_A000A { get; set; }
        private LinearPredictor m_A000AB { get; set; }
        private CleavePredictor m_A000AC { get; set; }
        private BatchDigestor m_A000AD { get; set; }

        public Tripper(ScoutParameters @params)
        {
            m_A000A = @params;
            m_A000BA();

            try
            {
                Console.Write("");
                Console.GetCursorPosition();
            }
            catch
            {
                _hasConsole = false;
            }
        }
        public BatchDigestor PrepareDatabase(string fastaFile)
        {
            return m__A000(fastaFile);
        }
        public ScoutRawResults PerformSearch(BatchDigestor m_A000AD, string m_A000AAE, CancellationToken m_A001AAE)
        {
            if (this.m_A000AD == null)
                this.m_A000AD = m_A000AD;
            this.m_A000AD.Reset();

            SearchPackFactory m_A000AE = new SearchPackFactory(m_A000A);
            SearchPack m_A000AF = null;

            try
            {
                m_A000AF = m_A000AE.AssembleSearchPack(m_A000AAE, m_A000A, m_A001AAE);
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR - PerformSearch method: It's not possible to assemble the search pack.\n" + e.StackTrace + "\n" + e.Message);
                throw;
            }

            List<Query> m_A000AG = m_A000AF.GetAllQueries();
            m_A000AG = m_A000AG
               .Where(a => a.SearchMH > m_A000A.MinPepMass)
               .Where(a => a.SearchMH < m_A000A.MaxPepMass)
               .OrderBy(a => a.SearchMH)
               .ToList();

            if (m_A001AAE.IsCancellationRequested)
                m_A001AAE.ThrowIfCancellationRequested();

            Console.WriteLine("INFO: Preparing peptides batch...");
            List<Peptide> m_A000AA = null;
            int batchesDone = 0;
            m_A000A.FastaBatchSize = m_A000A.FastaBatchSize == 0 ? 1 : m_A000A.FastaBatchSize;
            int numberOfBatches = (int)Math.Ceiling(((double)this.m_A000AD.ProteinCount / m_A000A.FastaBatchSize));
            m_A000AA = this.m_A000AD.GetCleanPeptidesBatch(m_A001AAE);
            m_A000AA = m_A000BB(m_A000AA, m_A001AAE);

            if (printToConsole)
            {
                int cleaveQueryCount = 0;
                if (m_A000AF.CleaveQueries != null)
                    cleaveQueryCount = m_A000AF.CleaveQueries.Count;

                int shotgunQueryCount = 0;
                if (m_A000AF.ShotgunQueries != null)
                    shotgunQueryCount = m_A000AF.ShotgunQueries.Count;

                Console.WriteLine(
                    $"INFO: Starting PSMs. " +
                    $"  Total Shotgun Queries: {shotgunQueryCount}" +
                    $"  Total Cleave Queries: {cleaveQueryCount}" +
                    $"  Total Proteins: {this.m_A000AD.ProteinCount}");
            }

            while (m_A000AA != null)
            {
                if (m_A001AAE.IsCancellationRequested) m_A001AAE.ThrowIfCancellationRequested();
                m_A000AA = m_A000AA.OrderBy(a => a.MH).ToList();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.WaitForFullGCComplete();
                GC.Collect();
                if (printToConsole) Console.WriteLine($"INFO: Starting batch {batchesDone + 1}/{numberOfBatches}                        ");
                try
                {
                    if (m_A000A.ParallelPSMs == false) m_A002BB(m_A000AG.ToArray(), m_A000AA, m_A001AAE);
                    else m_A002BC(m_A000AG, m_A000AA, m_A001AAE);
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR - PerformSearch method: It's not possible to run the search.\n" + e.StackTrace + "\n" + e.Message);
                    throw new Exception(e.StackTrace + "\n" + e.Message);
                }
                batchesDone++;
                Console.WriteLine("INFO: Preparing peptides batch...");
                m_A000AA = this.m_A000AD.GetCleanPeptidesBatch(m_A001AAE);
                if (m_A000AA != null) m_A000AA = m_A000BB(m_A000AA, m_A001AAE);
            }
            if (m_A001AAE.IsCancellationRequested) m_A001AAE.ThrowIfCancellationRequested();
            m_A003BA(m_A000AF.ScanResults, m_A000AG);
            var m_A0001AG = m_A000AF.ScanResults.GroupBy(a => a.ScanNumber)
                .Select(g => g.OrderByDescending(item => item.CleaveCandidates != null && item.CleaveCandidates.Count > 0 ? item.CleaveCandidates[0].XLScore : 0));
            m_A000AF.ScanResults = (from m_A001AG in m_A0001AG.AsParallel()
                                    select m_A001AG.First()).ToList();
            var m_A0001AH = new ScoutRawResults(m_A000AAE, m_A000A.FastaFile, m_A000AF.ScanResults, m_A000A);
            if (m_A001AAE.IsCancellationRequested) m_A001AAE.ThrowIfCancellationRequested();
            m_A00BAC(m_A001AAE, m_A000AAE, m_A0001AH);
            if (printToConsole) Console.WriteLine($"INFO: Finished!");
            return m_A0001AH;
        }

        private List<int> m_A00BAD(params double[] mhs)
        {
            List<int> m_A005 = new(m_A000A.MaxChargeInTheoreticalMS * m_A000A.IsotopesInTheoreticalMS * mhs.Length);
            for (int m = 0; m < mhs.Length; m++)
            {
                for (int m_A003 = 1; m_A003 <= m_A000A.MaxChargeInTheoreticalMS; m_A003++)
                {
                    for (int m_A002 = 0; m_A002 <= m_A000A.IsotopesInTheoreticalMS; m_A002++)
                    {
                        double m_A001 = ScoutCore.SpectraOperations.ChargeOperations.ToMZ(mhs[m], m_A003);
                        m_A001 += m_A002 * m_A000A.CarbonIsotopeShift;
                        m_A005.Add(Transformer.MassToBinIndex(m_A001, m_A000A.BinSize, m_A000A.MinBinMZ, m_A000A.Offset));
                    }
                }
            }

            return m_A005.Distinct().ToList();
        }
        private void m_A000BA()
        {
            var linearParameters = m_A000A.GetLinearPredictorParameters();
            var cleavePredParams = m_A000A.GetCleavePredictorParameters();

            m_A000AB = new LinearPredictor(linearParameters);
            m_A000AC = new CleavePredictor(cleavePredParams);
        }
        private List<Peptide> m_A000BB(List<Peptide> m_A000, CancellationToken token)
        {
            var m_A001 = Digestor.Modification.ModificationHelper.ModifyPeptides(m_A000, m_A000AD.Params, token);

            m_A001 = m_A001
                .Where(pep =>
                {
                    if (token.IsCancellationRequested)
                        token.ThrowIfCancellationRequested();

                    if (m_A000A.CXLReagent.TargetNTerm == true
                        &&
                        pep.Mappings.Any(a => a.ProteinPosition == 0)
                        &&
                        pep.N_TerminalModificationIndex == null)
                        return true;

                    HashSet<int> mod_sites = new();
                    for (int i = 0; i < pep.Modifications.Count; i++) mod_sites.Add(pep.Modifications[i].position);
                    for (int i = 0; i < pep.ResidueTuples.Length - 1; i++)
                    {
                        var aa = pep.ResidueTuples[i];
                        if (m_A000A.CXLReagent.Targets.Contains(aa.aminoacid)
                            &&
                            !mod_sites.Contains(i))
                            return true;
                    }

                    return false;
                })
                .ToList();

            return m_A001;
        }
        private void m_A000BC(XLScoresHelper m_A000, ScanResults m_A001, Dictionary<(int, List<(double, short)>), MSUltraLight> m_A003, int m_A004, List<(string, string)> m_A005)
        {
            try
            {
                m_A004++;
                if (m_A001.CleaveCandidates == null || m_A001.CleaveCandidates.Count == 0) return;
                List<ICleaveCandidate> m_A006 = m_A001.GetBestCleaveCandidates(1);
                if (m_A006 == null || m_A006.Count == 0) return;
                var m_A007 = m_A006.MaxBy(a => a.XLScore);
                if (m_A007.AllLightPSMs == null || m_A007.AllLightPSMs.Count == 0) return;
                if (!m_A007.IsLoopLink && (m_A007.AllHeavyPSMs == null || m_A007.AllHeavyPSMs.Count == 0)) return;
                if (!m_A007.IsLoopLink)
                {
                    var m_A008 = (CleavePSM)m_A007.SelectedPSMs.heavy.psm;
                    if (m_A008 == null) return;
                    var m_A009 = m_A007.AllHeavyPSMs.Cast<CleavePSM>().ToList();
                    var m_A0010 = (mh: m_A007.HeavyPairMH, intensity: m_A007.HeavyPairIntensity);
                    var m_A0011 = (CleavePSM)m_A007.SelectedPSMs.light.psm;
                    if (m_A0011 == null) return;
                    var m_A0012 = m_A007.AllLightPSMs.Cast<CleavePSM>().ToList();
                    var m_A0013 = (mh: m_A007.LightPairMH, intensity: m_A007.LightPairIntensity);
                    bool m_A0014 = false;
                    if (m_A0011.Peptide.Length > m_A008.Peptide.Length) m_A0014 = true;
                    else if (m_A0011.Peptide.Length == m_A008.Peptide.Length)
                    {
                        var m_A0015 = new List<string>() {
                            m_A008.Peptide.AsCleanString,
                            m_A0011.Peptide.AsCleanString
                        };
                        var m_A0016 = m_A0015.OrderBy(a => a).ToList();
                        if (m_A0016[0] != m_A0015[0]) m_A0014 = true;
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

                    lock (m_A001.Scores)
                    {
                        MSUltraLight m_A0099 = m_A000CB(m_A003, m_A001.PrecursorCharge, m_A001.PrecursorMZ, m_A001.ScanNumber);
                        m_A001.Scores = m_A000.ScoreCSM(
                             m_A008,
                             m_A0011,
                             m_A000A.CXLReagent,
                             ((CleaveCandidate)m_A007).TotalPairIsotopes,
                             m_A009,
                             m_A0012,
                             m_A0010,
                             m_A0013,
                             m_A0099,
                             m_A005);

                        m_A001.Scores["XLScore"] = m_A007.XLScore;
                        if (m_A000A.ReplaceNullPoissonToXLScore == true &&
                            m_A001.Scores.ContainsKey("PoissonScore"))
                        {
                            if (m_A001.Scores["PoissonScore"] == 0)
                                m_A001.Scores["PoissonScore"] = m_A001.Scores["XLScore"];
                        }
                        m_A001.Scores["XLDeltaCN"] = m_A007.XLDeltaCN;
                        if (m_A000A.SaveSpectraToResults == true) m_A001.Spectrum = ScoutCore.FileManagement.Serializer.ToJSON(m_A0099, false, false);
                    }
                }
                else
                {
                    var m_A0016 = m_A007.AllLightPSMs.Cast<CleavePSM>().ToList();
                    if (m_A0016 == null) return;
                    var m_A0017 = (CleavePSM)m_A007.SelectedPSMs.light.psm;
                    if (m_A0017 == null) return;
                    var m_A0018 = (mh: m_A007.LightPairMH, intensity: m_A007.LightPairIntensity);
                    lock (m_A001.Scores)
                    {
                        MSUltraLight m_A0019 = m_A000CB(m_A003, m_A001.PrecursorCharge, m_A001.PrecursorMZ, m_A001.ScanNumber);
                        m_A001.Scores = m_A000.ScoreCSM(
                             m_A0017,
                             m_A000A.CXLReagent,
                             ((CleaveCandidate)m_A007).TotalPairIsotopes,
                             m_A0016,
                             m_A0018,
                             m_A0019,
                             m_A005);
                        m_A001.Scores["XLScore"] = m_A007.XLScore;
                        m_A001.Scores["XLDeltaCN"] = m_A007.XLDeltaCN;
                        if (m_A000A.SaveSpectraToResults == true)
                            m_A001.Spectrum = ScoutCore.FileManagement.Serializer.ToJSON(m_A0019, false, false);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: ScoreCSMsFromSearchResults method.\nScan: " + m_A004 + "\n" + e.StackTrace + "\n" + e.Message);
                throw;
            }
        }
        private MSUltraLight m_A000CB(Dictionary<(int, List<(double, short)>), MSUltraLight> m_A00, short m_A01, double m_A02, int m_A03)
        {
            return m_A00.Where(a => a.Key.Item1 == m_A03 && a.Key.Item2.Any(b => b.Item1 == m_A02 && b.Item2 == m_A01)).FirstOrDefault().Value;
        }
        private void m_A00BAC(CancellationToken m_AB001, string m_AB002, ScoutRawResults m_AB003)
        {
            var searchedScanNumbers = m_AB003.GetSearchedScanNumbers(false, true);
            var _spectra = ScoutCore.FileManagement.SpectrumParser.Parse(m_AB001,
                    m_AB002, true, 2, 400, searchedScanNumbers)
                    .ToDictionary(a => (a.ScanNumber, a.Precursors), a => a);

            var xlScoresHelper = new XLScoresHelper(m_A000A);
            FastaFileParser _FASTA = ((BatchDigestor)m_A000AD).FP;

            var proteins =
                _FASTA
                .MyItems
                .Select(a => (a.SequenceIdentifier, a.Sequence))
                .ToList();

            int processed_line = 0;
            int old_progress = 0;
            double lengthFile = m_AB003.ScanResults.Count();

            if (printToConsole)
                Console.WriteLine($"INFO: Assembling and scoring CSMs for {m_AB003.RawFile}...");

            if (m_AB003.ScanResults != null)
            {
                int scanNum = 0;
                if (m_AB003._Params.ParallelPSMs == false)
                {
                    foreach (ScanResults scan in m_AB003.ScanResults.Values)
                    {
                        if (_hasConsole)
                        {
                            processed_line++;
                            int new_progress = (int)((double)processed_line / (lengthFile) * 100);
                            if (new_progress > old_progress)
                            {
                                old_progress = new_progress;
                                Console.Write("Progress: " + old_progress + "%");
                            }
                        }
                        m_A000BC(xlScoresHelper, scan, _spectra, scanNum, proteins);
                    }
                }
                else
                {
                    int threadsToUse = Environment.ProcessorCount - 2;
                    Parallel.ForEach(m_AB003.ScanResults.Values, new ParallelOptions() { MaxDegreeOfParallelism = threadsToUse }, (scan) => m_A000BC(xlScoresHelper, scan, _spectra, scanNum, proteins));
                }
            }
        }
        private void m_A002BB(Query[] m_AB000, List<Peptide> m_AB001, CancellationToken m_AB002)
        {
            var loadedLinearSpectra = new Dictionary<int, List<(int index, double intensity)>>();
            var loadedCleaveSpectra = new Dictionary<int, List<(List<(int index, double intensity)> ms, int xlPos)>>();
            var loadedLoopLinkCleaveSpectra = new Dictionary<int, List<(List<(int index, double intensity)> ms, int xlPos1, int xlPos2)>>();

            int p_start = 0;
            for (int q = 0; q < m_AB000.Length; q++)
            {
                if (m_AB002.IsCancellationRequested)
                    m_AB002.ThrowIfCancellationRequested();

                var query = m_AB000[q];

                if (query.SearchMH < m_A000A.MinPepMass
                    || query.SearchMH > m_A000A.MaxPepMass)
                {
                    continue;
                }

                double ppmStep = query.SearchMH * m_A000A.PPMMS1Tolerance / 1000000;
                double lowerBound = query.SearchMH - ppmStep;
                double upperBound = query.SearchMH + ppmStep;

                for (int p = p_start; p < m_AB001.Count; p++)
                {
                    var peptide = m_AB001[p];
                    if (peptide.MH < lowerBound)
                    {
                        loadedLinearSpectra.Remove(p);
                        loadedCleaveSpectra.Remove(p);
                        loadedLoopLinkCleaveSpectra.Remove(p);
                        p_start = p;
                        continue;
                    }

                    if (peptide.MH > upperBound) break;

                    IPSM previouslySearched = query.PSMs.Find(a => a.Peptide.CompareToPep(peptide));
                    if (previouslySearched != null)
                    {
                        lock (previouslySearched.Peptide.Mappings)
                        {
                            previouslySearched.Peptide.Mappings.AddRange(
                                peptide.Mappings
                                );
                            previouslySearched.Peptide.Mappings = previouslySearched.Peptide.Mappings.Distinct(new PeptideMappingComparer()).ToList();
                        }
                        continue;
                    }
                    #region PSM
                    if (query.IsCleavable)
                    {
                        if (query.IsLoopLink)
                        {
                            List<(List<(int, double)>, int, int)> thSpectra = m_A0011A(loadedLoopLinkCleaveSpectra, p, peptide);
                            if (thSpectra == null) continue;
                            m_A0014B(query, thSpectra, peptide);
                        }
                        else
                        {
                            List<(List<(int, double)>, int)> thSpectra = m_B001A(loadedCleaveSpectra, p, peptide);
                            if (thSpectra == null) continue;
                            m_A002(query, thSpectra, peptide);
                        }
                    }
                    else
                    {
                        var th = m_A0013B(loadedLinearSpectra, p, peptide);
                        m_A001(query, th, peptide);
                    }

                    #endregion
                }
            }
        }
        private void m_A002BC(List<Query> m_AB000, List<Peptide> m_AB001, CancellationToken m_AB002)
        {
            int threadsToUse = Environment.ProcessorCount - 2 > 0 ? Environment.ProcessorCount - 2 : 1;

            Random rng = new Random();
            var byScan = m_AB000
                .GroupBy(a => a.ScanNumber)
                .ToList();
            var chunked = byScan
                .OrderBy(a => rng.Next())
                .Chunk(
                    (int)((float)byScan.Count() / (float)threadsToUse) + 1)
                .ToList();
            List<Query[]> queryBatches =
                chunked
                .Select(a => a.SelectMany(b => b).OrderBy(b => b.SearchMH).ToArray())
                .ToList();
            Parallel.ForEach(queryBatches, new ParallelOptions() { MaxDegreeOfParallelism = threadsToUse }, (queries) => m_A002BB(queries, m_AB001, m_AB002));
        }
        private void m_A003BA(List<ScanResults> m_A001, List<Query> m_A002)
        {
            #region
            foreach (var m_A003 in m_A001)
            {
                if (m_A000A.PerformShotgunSearch)
                {
                    if (m_A003.ShotgunCandidate.PSMs != null)
                    {
                        if (m_A003.ShotgunCandidate.PSMs.Count == 0) m_A003.ShotgunCandidate.PSMs = null;
                        else m_A003.ShotgunCandidate.PSMs = m_A003.ShotgunCandidate.PSMs.OrderByDescending(a => a.Score).ToList();
                    }
                }

                if (m_A000A.PerformCleaveXLSearch)
                {
                    foreach (var m_A004 in m_A003.CleaveCandidates)
                    {
                        if (m_A004.AllLightPSMs != null)
                        {
                            if (m_A004.AllLightPSMs.Count == 0) m_A004.AllLightPSMs = null;
                            else m_A004.AllLightPSMs = m_A004.AllLightPSMs.OrderByDescending(a => a.Score).ToList();
                        }

                        if (m_A004.AllHeavyPSMs != null)
                        {
                            if (m_A004.AllHeavyPSMs.Count == 0) m_A004.AllHeavyPSMs = null;
                            else m_A004.AllHeavyPSMs = m_A004.AllHeavyPSMs.OrderByDescending(a => a.Score).ToList();
                        }

                        if (m_A004.AllLightPSMs != null && m_A004.AllHeavyPSMs != null)
                        {
                            if (m_A004.AllLightPSMs.Count > 1)
                            {
                                var m_A0041 = m_A004.AllLightPSMs.Where(a => a.Score == m_A004.AllLightPSMs[0].Score).ToList();
                                if (m_A0041.Count() > 1)
                                {
                                    var m_A0042 = m_A004.AllHeavyPSMs[0];
                                    m_A004.AllLightPSMs = m_A004.AllLightPSMs
                                        .OrderByDescending(a => a.Score)
                                        .ThenByDescending(a =>
                                            a.Peptide.Mappings.Any(p_light => m_A0042.Peptide.Mappings.Any(p_heavy => p_light.Locus == p_heavy.Locus)))
                                        .ToList();
                                }
                            }

                            if (m_A004.AllHeavyPSMs.Count > 1)
                            {
                                var m_A0043 = m_A004.AllHeavyPSMs.Where(a => a.Score == m_A004.AllHeavyPSMs[0].Score).ToList();
                                if (m_A0043.Count() > 1)
                                {
                                    var m_A0044 = m_A004.AllHeavyPSMs[0];
                                    m_A004.AllHeavyPSMs = m_A004.AllHeavyPSMs
                                        .OrderByDescending(a => a.Score)
                                        .ThenByDescending(a =>
                                            a.Peptide.Mappings.Any(p_heavy => m_A0044.Peptide.Mappings.Any(p_light => p_heavy.Locus == p_light.Locus)))
                                        .ToList();
                                }
                            }
                        }
                    }
                }
            }
            #endregion

            if (m_A000A.ReorderCleaveCandidateScores == true)
            {
                if (m_A000A.ReorderingMethod == ScoutParameters.ReorderMethods.None)
                {
                    foreach (var m_A005 in m_A001)
                    {
                        foreach (ICleaveCandidate m_A006 in m_A005.CleaveCandidates)
                        {
                            if (m_A006.IsLoopLink == false)
                            {
                                if (m_A006.AllLightPSMs == null || m_A006.AllHeavyPSMs == null) continue;
                                IPSM m_A007 = m_A006.AllLightPSMs.First();
                                IPSM m_A008 = m_A006.AllHeavyPSMs.First();
                                m_A006.XLScore = Math.Min(m_A007.Score, m_A008.Score);
                                m_A006.SelectedPSMs = ((m_A007, 0), (m_A008, 0));
                                m_A006.PeaksMatched = Math.Min(m_A007.PeaksMatched, m_A008.PeaksMatched);
                            }
                            else
                            {
                                if (m_A006.AllLightPSMs == null) continue;
                                m_A006.XLScore = m_A006.AllLightPSMs.First().Score;
                                m_A006.SelectedPSMs = ((m_A006.AllLightPSMs[0], 0), (null, 0));
                                m_A006.PeaksMatched = m_A006.AllLightPSMs.First().PeaksMatched;
                            }
                        }
                    }
                }
                else if (m_A000A.ReorderingMethod == ScoutParameters.ReorderMethods.ByPair)
                {
                    Console.WriteLine("INFO: Rescoring Cleave Candidates...");
                    Dictionary<int, (int index, double intensity)[]> m_A009 = m_A002
                            .GroupBy(a => a.ScanNumber)
                            .ToDictionary(a => a.Key, a => a.First().SparseBinnedMS);

                    int progress = 0;
                    foreach (var m_A0010 in m_A001)
                    {
                        progress++;
                        if (_hasConsole && progress % 10 == 0) Console.Write($"Progress: {((float)progress / m_A001.Count):#.##%} sr:{progress}/{m_A001.Count}");
                        foreach (ICleaveCandidate m_A0011 in m_A0010.CleaveCandidates)
                        {
                            double m_A00399 = -1;
                            int m_A00340 = -1;

                            if (!m_A0011.IsLoopLink)
                            {
                                if (m_A0011.AllLightPSMs == null || m_A0011.AllHeavyPSMs == null) continue;

                                IPSM m_A0012 = m_A0011.AllLightPSMs.First();
                                IPSM m_A0013 = m_A0011.AllHeavyPSMs.First();

                                var m_A0014 = m_A000AC.PredictSparseSpectrum(
                                    m_A0012.Peptide.ResidueTuples,
                                    m_A0012.Peptide.N_TerminalModMass,
                                    m_A0012.Peptide.C_TerminalModMass,
                                    m_A000A.CXLReagent,
                                    ((CleavePSM)m_A0012).ReagentPosition1);
                                var m_A0015 = m_A000AC.PredictSparseSpectrum(
                                    m_A0013.Peptide.ResidueTuples,
                                    m_A0013.Peptide.N_TerminalModMass,
                                    m_A0013.Peptide.C_TerminalModMass,
                                    m_A000A.CXLReagent,
                                    ((CleavePSM)m_A0013).ReagentPosition1);

                                var m_A0016 =
                                    SpectraOperations.Transformer.FuseSparseSpectra(
                                        ehhGUguTHdJpphaMaPwjeBBRrSrWwaxZWeklDtWI: false,
                                        m_A0014,
                                        m_A0015);

                                SpectraOperations.Transformer.DivideByNorm(m_A0016);

                                List<int> m_A0017 = null;
                                if (m_A000A.RemoveIonPairsFromExperimentalMS)
                                {
                                    m_A0017 = m_A00BAD(
                                        m_A0011.LightPairMH,
                                        m_A0011.HeavyPairMH);
                                }

                                m_A00399 = SpectraOperations.ScoringFunctions.SparseDotProduct(
                                    m_A0016,
                                    m_A009[m_A0011.ScanNumber],
                                    out m_A00340,
                                    m_A0017);
                            }
                            else
                            {
                                if (m_A0011.AllLightPSMs == null) continue;
                                m_A00399 = m_A0011.AllLightPSMs.First().Score;
                                m_A00340 = m_A0011.AllLightPSMs.First().PeaksMatched;
                            }

                            m_A0011.XLScore = m_A00399;
                            m_A0011.PeaksMatched = m_A00340;
                            m_A0011.XLDeltaCN = null;

                            if (m_A0011.IsLoopLink)
                                m_A0011.SelectedPSMs = ((m_A0011.AllLightPSMs[0], 0), (null, 0));
                            else
                                m_A0011.SelectedPSMs = ((m_A0011.AllLightPSMs[0], 0), (m_A0011.AllHeavyPSMs[0], 0));
                        }

                        m_A0010.CleaveCandidates = m_A0010.CleaveCandidates.OrderByDescending(a => a.XLScore).ToList();
                    }
                }
                else if (m_A000A.ReorderingMethod == ScoutParameters.ReorderMethods.PSMCombinatorial)
                {
                    Console.WriteLine("INFO: Rescoring Cleave Candidates...");
                    Dictionary<int, (int index, double intensity)[]> msByScan = m_A002
                            .GroupBy(a => a.ScanNumber)
                            .ToDictionary(a => a.Key, a => a.First().SparseBinnedMS);

                    int progress = 0;
                    (int Left, int Top) cursor = default;
                    if (_hasConsole) cursor = Console.GetCursorPosition();

                    foreach (var m_A0018 in m_A001)
                    {
                        progress++;
                        if (_hasConsole && progress % 10 == 0)
                        {
                            Console.SetCursorPosition(cursor.Left, cursor.Top);
                            Console.Write($"Progress: {((float)progress / m_A001.Count):#.##%} sr:{progress}/{m_A001.Count}");
                        }

                        foreach (ICleaveCandidate m_A0019 in m_A0018.CleaveCandidates)
                        {
                            if (m_A0019.AllLightPSMs == null || m_A0019.AllHeavyPSMs == null) continue;

                            List<IPSM> m_A00120 = m_A0019
                                .AllLightPSMs
                                .Where(a => a.Score > 0)
                                .Take(m_A000A.ReorderingCombinatorialDepthLight)
                                .ToList();

                            List<IPSM> m_A00121 = m_A0019
                                .AllHeavyPSMs
                                .Where(a => a.Score > 0)
                                .Take(m_A000A.ReorderingCombinatorialDepthHeavy)
                                .ToList();

                            var m_A00122 = new Dictionary<int, List<(int, double)>>();
                            var m_A00123 = new Dictionary<int, List<(int, double)>>();
                            for (int i = 0; i < m_A00120.Count; i++)
                            {
                                var m_A00124 = m_A00120[i];
                                var m_A00125 = m_A000AC.PredictSparseSpectrum(
                                    m_A00124.Peptide.ResidueTuples,
                                    m_A00124.Peptide.N_TerminalModMass,
                                    m_A00124.Peptide.C_TerminalModMass,
                                    m_A000A.CXLReagent,
                                    ((CleavePSM)m_A00124).ReagentPosition1);

                                m_A00122.Add(i, m_A00125);
                            }
                            for (int i = 0; i < m_A00121.Count; i++)
                            {
                                var m_A00126 = m_A00121[i];
                                var m_A00127 = m_A000AC.PredictSparseSpectrum(
                                    m_A00126.Peptide.ResidueTuples,
                                    m_A00126.Peptide.N_TerminalModMass,
                                    m_A00126.Peptide.C_TerminalModMass,
                                    m_A000A.CXLReagent,
                                    ((CleavePSM)m_A00126).ReagentPosition1);

                                m_A00123.Add(i, m_A00127);
                            }


                            List<int> m_A0036 = null;
                            if (m_A000A.RemoveIonPairsFromExperimentalMS)
                            {
                                m_A0036 = m_A00BAD(
                                    m_A0019.LightPairMH,
                                    m_A0019.HeavyPairMH);
                            }

                            var m_A00128 = new List<(int light, int heavy, double score, int peaksMatched)>();
                            for (int m_A00129 = 0; m_A00129 < m_A00120.Count; m_A00129++)
                            {
                                var psmLight = m_A00120[m_A00129];
                                var thMSLight = m_A00122[m_A00129];

                                for (int m_A001230 = 0; m_A001230 < m_A00121.Count; m_A001230++)
                                {
                                    var m_A0031 = m_A00121[m_A001230];
                                    var m_A0032 = m_A00123[m_A001230];

                                    var m_A0033 =
                                        SpectraOperations.Transformer.FuseSparseSpectra(
                                            ehhGUguTHdJpphaMaPwjeBBRrSrWwaxZWeklDtWI: false,
                                            thMSLight,
                                            m_A0032);

                                    SpectraOperations.Transformer.DivideByNorm(m_A0033);
                                    var m_A0034 = msByScan[m_A0019.ScanNumber];

                                    double score = SpectraOperations.ScoringFunctions.SparseDotProduct(
                                       m_A0033,
                                       m_A0034,
                                       out int m_A0035,
                                       m_A0036);

                                    m_A00128.Add((m_A00129, m_A001230, score, m_A0035));
                                }
                            }

                            m_A00128 = m_A00128.OrderByDescending(a => a.score).ToList();
                            var m_A0037 = m_A00128[0];
                            m_A0019.XLScore = m_A0037.score;
                            m_A0019.PeaksMatched = m_A0037.peaksMatched;

                            var m_A0038 = new List<double>()
                            {
                                m_A0037.score
                            };
                            m_A0038.AddRange(
                                m_A00128
                                .Where(
                                    a => a.light != m_A0037.light
                                    && a.heavy != m_A0037.heavy)
                                .Where(a =>
                                (m_A00120[a.light].Peptide.AsCleanString
                                != m_A00120[m_A0037.light].Peptide.AsCleanString)
                                &&
                                (m_A00121[a.heavy].Peptide.AsCleanString
                                != m_A00121[m_A0037.heavy].Peptide.AsCleanString))
                                .Select(a => a.score));

                            if (m_A0038.Count >= 2)
                            {
                                m_A0019.XLDeltaCN = ScoringFunctions.GetDeltaCN(
                                    m_A0038
                                    );
                            }
                            else
                            {
                                m_A0019.XLDeltaCN = null;
                            }

                            m_A0019.SelectedPSMs = (
                                (m_A00120[m_A0037.light], m_A0037.light),
                                (m_A00121[m_A0037.heavy], m_A0037.heavy)
                                );
                        }

                        m_A0018.CleaveCandidates = m_A0018.CleaveCandidates
                            .OrderByDescending(a => a.XLScore).ToList();
                    }
                }
                else
                {
                    throw new Exception("Unkown reordering method.");
                }
            }
        }
        private BatchDigestor m__A000(string m_B001)
        {
            var m__A001 = m_A000A.AssembleDigestionParemeters();

            if (m_A000A.AddXLasVariableMod == true)
            {
                var m_A002 = new List<AminoacidMod>(m_A000A.VariableModifications);
                var m_A003 = new AminoacidMod()
                {
                    Name = m_A000A.CXLReagent.Name + "_Mod",
                    MassShift = m_A000A.CXLReagent.WholeMass + SpectrumWizard.Utils.Chemistry.OH,
                    TargetResidues = m_A000A.CXLReagent.Targets,
                    IsVariable = true,
                    IsNTerm = false,
                    IsCTerm = false
                };
                m_A002.Add(m_A003);
                m__A001.VariableModifications = m_A002;
            }

            return new BatchDigestor(m_B001, m__A001)
            {

            };
        }
        private void m_A001(Query jWquhnMH, List<(int, double)> RJgxHUYo, Peptide dsTHvgjD)
        {
            int fJuqExmI = default(int);
            double num = ScoringFunctions.SparseDotProduct(RJgxHUYo, jWquhnMH.SparseBinnedMS, out fJuqExmI, (List<int>)null);
            if (num != 0.0)
            {
                m_A00B1(jWquhnMH, dsTHvgjD, num, fJuqExmI, RJgxHUYo.Count);
            }
        }
        private void m_A002(Query BErmhBKA, List<(List<(int, double)>, int)> iRExvovB, Peptide KeQQOowk)
        {
            iRExvovB.ForEach(delegate ((List<(int, double)>, int) a)
            {
                int num = default(int);
                double num2 = ScoringFunctions.SparseDotProduct(a.Item1, BErmhBKA.SparseBinnedMS, out num, (List<int>)null);
                if (num2 != 0.0)
                {
                    if (m_A000A.BonusMode) num2 += (double)num * m_A000A.BonusScore;
                    m_A00B1(BErmhBKA, KeQQOowk, num2, num, a.Item1.Count, a.Item2);
                }
            });
        }
        private List<(List<(int, double)>, int)> m_B001A(Dictionary<int, List<(List<(int, double)>, int)>> RYELibPr, int AliPaJCr, Peptide xlnulfmG)
        {
            if (!RYELibPr.TryGetValue(AliPaJCr, out var value))
            {
                lock (xlnulfmG.Mappings)
                {
                    IEnumerable<int> source = m_A0012B(m_A000A.CXLReagent, xlnulfmG);
                    if (!source.Any())
                    {
                        RYELibPr.Add(AliPaJCr, null);
                        return null;
                    }

                    value = source.Select((int xlPos) => m_A002B(xlnulfmG, m_A000A.CXLReagent, xlPos)).ToList();
                    RYELibPr.Add(AliPaJCr, value);
                    return value;
                }
            }

            return value;
        }
        private void m_A0014B(Query oSZHAgoH, List<(List<(int, double)>, int, int)> jgrfStZz, Peptide eDBzSMpW)
        {
            jgrfStZz.ForEach(delegate ((List<(int, double)>, int, int) a)
            {
                int num = default(int);
                double num2 = ScoringFunctions.SparseDotProduct(a.Item1, oSZHAgoH.SparseBinnedMS, out num, (List<int>)null);
                if (num2 != 0.0)
                {
                    if (m_A000A.BonusMode) num2 += (double)num * m_A000A.BonusScore;
                    m_A00B1(oSZHAgoH, eDBzSMpW, num2, num, a.Item1.Count, a.Item2, a.Item3);
                }
            });
        }
        private List<(List<(int, double)>, int, int)> m_A0011A(Dictionary<int, List<(List<(int, double)>, int, int)>> wpQQyJaB, int rXDPfvmo, Peptide KzvyDbYN)
        {
            if (!wpQQyJaB.TryGetValue(rXDPfvmo, out var value))
            {
                lock (KzvyDbYN.Mappings)
                {
                    IEnumerable<int> enumerable = m_A0012B(m_A000A.CXLReagent, KzvyDbYN);
                    if (!enumerable.Any())
                    {
                        wpQQyJaB.Add(rXDPfvmo, null);
                        return null;
                    }

                    value = (from reagentPositions in enumerable.Combinations(2)
                             select m_A001B(KzvyDbYN, m_A000A.CXLReagent, reagentPositions.ToList()[0], reagentPositions.ToList()[1])).ToList();
                    wpQQyJaB.Add(rXDPfvmo, value);
                    return value;
                }
            }

            return value;
        }
        private List<(int, double)> m_A0013B(Dictionary<int, List<(int, double)>> UacWVSkC, int OiInWoys, Peptide tJLDdESL)
        {
            if (!UacWVSkC.TryGetValue(OiInWoys, out var value))
            {
                value = m_A000B(tJLDdESL);
                UacWVSkC.Add(OiInWoys, value);
            }

            return value;
        }
        private IEnumerable<int> m_A0012B(CleaveReagent ZSaAFdkP, Peptide jWJgKCPD)
        {
            for (int i = 0; i < jWJgKCPD.ResidueTuples.Length - 1; i++)
            {
                if (i == 0 && ZSaAFdkP.TargetNTerm && jWJgKCPD.Mappings.Any((PeptideMapping a) => a.ProteinPosition == 0)) yield return i;
                else if ((jWJgKCPD.Modifications == null || !jWJgKCPD.Modifications.Any<(int, int)>(((int position, int modIndex) a) => a.position == i)) && ZSaAFdkP.Targets.Contains(jWJgKCPD.ResidueTuples[i].Item1)) yield return i;
            }
        }
        private List<(int, double)> m_A000B(Peptide ybBYzGTe)
        {
            List<(int, double)> list = m_A000AB.PredictSparseBinnedSpectrum(ybBYzGTe.ResidueTuples, ybBYzGTe.N_TerminalModMass, ybBYzGTe.C_TerminalModMass);
            if ((int)m_A000A.ScoreFunction == 0) return list;
            if ((int)m_A000A.ScoreFunction == 1)
            {
                Transformer.DivideByNorm(list);
                return list;
            }

            throw new Exception("No score function selected.");
        }
        private (List<(int, double)>, int, int) m_A001B(Peptide kPRPJgzS, CleaveReagent ODPYxhgn, int UcxPOcaL, int oqUOMnfd)
        {
            List<(int, double)> list = m_A000AC.PredictSparseSpectrum(kPRPJgzS.ResidueTuples, kPRPJgzS.N_TerminalModMass, kPRPJgzS.C_TerminalModMass, ODPYxhgn, UcxPOcaL, oqUOMnfd);
            if ((int)m_A000A.ScoreFunction == 0) return (list, UcxPOcaL, oqUOMnfd);
            if ((int)m_A000A.ScoreFunction == 1)
            {
                Transformer.DivideByNorm(list);
                return (list, UcxPOcaL, oqUOMnfd);
            }

            throw new Exception("No score function selected.");
        }
        private (List<(int, double)>, int) m_A002B(Peptide mtAReMad, CleaveReagent gTEFToTF, int YTUHWdBd)
        {
            List<(int, double)> list = null;
            list = (m_A000A.BDP_Mode ? m_A000AC.PredictSparseSpectrumBDP(mtAReMad.ResidueTuples, mtAReMad.N_TerminalModMass, mtAReMad.C_TerminalModMass, gTEFToTF, YTUHWdBd) : m_A000AC.PredictSparseSpectrum(mtAReMad.ResidueTuples, mtAReMad.N_TerminalModMass, mtAReMad.C_TerminalModMass, gTEFToTF, YTUHWdBd));
            if ((int)m_A000A.ScoreFunction == 0) return (list, YTUHWdBd);
            if ((int)m_A000A.ScoreFunction == 1)
            {
                Transformer.DivideByNorm(list);
                return (list, YTUHWdBd);
            }

            throw new Exception("No score function selected.");
        }
        private void m_A00B1(Query PsQTJoKS, Peptide OClIUooE, double FsoRrUCK, int FJuqExmI, int ydICfttH, int? VycqceVt = null, int? Yqmgguuo = null)
        {
            if (PsQTJoKS.PSMs.Count < m_A000A.MaxQueryResults)
            {
                if (VycqceVt.HasValue)
                {
                    lock (PsQTJoKS.PSMs)
                    {
                        if (PsQTJoKS.IsLoopLink)
                        {
                            PsQTJoKS.PSMs.Add((IPSM)new CleavePSM(OClIUooE, PsQTJoKS.IsotopeNumber, FJuqExmI, FsoRrUCK, VycqceVt.Value, Yqmgguuo.Value));
                        }
                        else
                        {
                            PsQTJoKS.PSMs.Add((IPSM)new CleavePSM(OClIUooE, PsQTJoKS.IsotopeNumber, FJuqExmI, FsoRrUCK, VycqceVt.Value));
                        }
                    }
                }
                else
                {
                    lock (PsQTJoKS.PSMs)
                    {
                        PsQTJoKS.PSMs.Add((IPSM)new PSM(OClIUooE, FsoRrUCK, FJuqExmI, PsQTJoKS.IsotopeNumber));
                    }
                }
            }
            else
            {
                if (!PsQTJoKS.PSMs.Exists((IPSM a) => a.Score < FsoRrUCK)) return;

                int index = PsQTJoKS.PSMs.IndexOf(PsQTJoKS.PSMs.MinBy((IPSM a) => a.Score));
                if (VycqceVt.HasValue)
                {
                    lock (PsQTJoKS.PSMs)
                    {
                        if (PsQTJoKS.IsLoopLink) PsQTJoKS.PSMs[index] = (IPSM)new CleavePSM(OClIUooE, PsQTJoKS.IsotopeNumber, FJuqExmI, FsoRrUCK, VycqceVt.Value, Yqmgguuo.Value);
                        else PsQTJoKS.PSMs[index] = (IPSM)new CleavePSM(OClIUooE, PsQTJoKS.IsotopeNumber, FJuqExmI, FsoRrUCK, VycqceVt.Value);
                    }
                }
                else
                {
                    lock (PsQTJoKS.PSMs)
                        PsQTJoKS.PSMs[index] = (IPSM)new PSM(OClIUooE, FsoRrUCK, FJuqExmI, PsQTJoKS.IsotopeNumber);
                }
            }
        }
    }
}
