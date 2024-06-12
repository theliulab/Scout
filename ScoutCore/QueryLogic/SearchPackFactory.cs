using ScoutCore.FileManagement;
using ScoutCore.IonPairLogic;
using ScoutCore.PSMEngines;
using PatternTools.MSParserLight;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using YCore;
using ThermoFisher.CommonCore.Data.Business;
using ScoutCore.SpectraOperations;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace ScoutCore.QueryLogic
{
    public class SearchPackFactory
    {
        private bool _hasConsole = true;
        private const int m_AB00 = 4;
        private Core m_A001;

        public ScoutParameters m_A000 { get; set; }
        public SearchPackFactory(ScoutParameters m_A000)
        {
            this.m_A000 = m_A000;

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
        public SearchPack AssembleSearchPack(string rawFile, ScoutParameters parameters, CancellationToken token)
        {
            List<MSUltraLight> spectra = null;

            if (token.IsCancellationRequested)
                token.ThrowIfCancellationRequested();

            try
            {
                spectra = SpectrumParser.Parse(token, rawFile, true, 2);
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: It's not possible to read " + rawFile);
                throw new Exception(e.Message);
            }

            var yParams = new Dictionary<int, YadaParams>();
            yParams.Add(1, YadaParams.GetDefaultsMS1());
            yParams.Add(2, YadaParams.GetDefaultsMS2());
            yParams[2].MinimunPeaksPerCharge[1] = 2;
            yParams[2].MinimunPeaksPerCharge[2] = 2;
            yParams[2].EnvelopeStringency = 0.75;
            yParams[2].MaxChargeState = 4;
            yParams[2].MaxParsimony = false;
            yParams[2].ErrorTolerancePPM = 20;
            yParams[2].RemovePrecursor = true;
            yParams[2].IsotopeModelSteps = 5;

            m_A001 = new Core(yParams);

            if (token.IsCancellationRequested)
                token.ThrowIfCancellationRequested();

            spectra = spectra.Where(a => a.Ions.Count > 10).ToList();

            var scanResults = m_A000A(spectra, token);

            List<Query> shotgunQueries = null;
            List<Query> cleaveQueries = null;

            if (parameters.PerformShotgunSearch)
            {
                shotgunQueries = m_A0004(scanResults);
            }
            if (parameters.PerformCleaveXLSearch)
            {
                cleaveQueries = m_A0005(scanResults);
            }

            return new SearchPack(
                scanResults.Select(a => a.Item1).ToList(),
                shotgunQueries,
                cleaveQueries);
        }
        public static (int, double)[] PrepareSparseExperimentalSpectrum(
            List<(double, double)> m_A00,
            ScoutParameters m_A01)
        {
            List<(double, double)> m_A02 = m_A00.Select(a => (a.Item1, a.Item2)).ToList();

            if (m_A01.ApplyQuantileIntensityThreshold == true)
            {
                m_A02 = m_A02
                    .OrderByDescending(a => a.Item2)
                    .Take((int)Math.Ceiling(m_A00.Count * (1 - m_A01.QuantileIntensityThreshold)))
                    .OrderBy(a => a.Item1)
                    .ToList();
            }

            if (m_A01.MS2NormalizationTypes == SpectraOperations.NormalizationTypes.ByMZWindow)
            {
                m_A02 = SpectraOperations.Transformer.KeepTopPeaks(
                    m_A02,
                    m_A01.NormalizationWindowWidth,
                    m_A01.NormalizationWindowPeaksKept,
                    true);
            }
            else if (m_A01.MS2NormalizationTypes == SpectraOperations.NormalizationTypes.SetTopToOneIntensity)
            {
                m_A02 = SpectraOperations.Transformer.KeepTopPeaks(
                    m_A02,
                    m_A01.NormalizationWindowWidth,
                    m_A01.NormalizationWindowPeaksKept,
                    false);

                m_A02 = m_A02.Select(a => (a.Item1, 1.0)).ToList();
            }
            else if (m_A01.MS2NormalizationTypes == SpectraOperations.NormalizationTypes.None)
            {
                m_A02 = m_A02.Select(a => (a.Item1, a.Item2)).ToList();
            }
            else
            {
                throw new Exception("No score function selected.");
            }

            var m_A03 = SpectraOperations.Transformer.BinSpectrumSparse(
                m_A02,
                m_A01.Offset,
                m_A01.BinSize,
                m_A01.MinBinMZ,
                m_A01.MaxBinMZ);

            if (m_A01.ScoreFunction == SpectraOperations.ScoreFunctionTypes.SpectralAngle)
            {
                SpectraOperations.Transformer.DivideByNorm(m_A03);
            }

            return m_A03;
        }
        private List<(ScanResults, (int, double)[])> m_A000A(List<MSUltraLight> m_A00, CancellationToken m_A01)
        {
            var m_A03 = new List<(ScanResults, (int, double)[])>();
            (int Left, int Top) cursor = default;
            if (_hasConsole)
            {
                cursor = Console.GetCursorPosition();
                Console.WriteLine($"INFO: Preparing spectra for search...");
            }
            int ctt = 0;

            List<MSUltraLight> m_A04 = null;
            if (m_A000.DeconvolutionForMSScoring == true
                ||
                m_A000.DeconvolutionForPairSearching == true)
            {
                m_A04 = m_A001.Deconvolute(m_A00, false, true, false, false, 0, true, m_A01)
                    .Select(a => a.WdwLjojDkJkYrxKfiziXCWheWpDHsbtWNwqsGJWP).ToList();
                if (m_A04.Count != m_A00.Count)
                    throw new Exception("ERROR: YADA deconvolution error. Output spectrum count does not match input.");
            }

            for (int i = 0; i < m_A00.Count; i++)
            {
                MSUltraLight m_A05 = null;
                if (m_A000.DeconvolutionForMSScoring == true)
                    m_A05 = m_A04[i];
                else
                    m_A05 = m_A00[i];

                MSUltraLight m_A06 = null;
                if (m_A000.DeconvolutionForPairSearching == true)
                    m_A06 = m_A04[i];
                else
                    m_A06 = m_A00[i];

                ctt++;
                if (_hasConsole)
                {
                    if (ctt % 10 == 0)
                    {
                        Console.Write($"Progress: {100f * ctt / m_A00.Count}%");
                        Console.SetCursorPosition(cursor.Left, cursor.Top);
                    }
                }

                var m_A07 = PrepareSparseExperimentalSpectrum(m_A05.Ions, m_A000);
                var m_A08 = new ScanResults(
                    m_A05.ScanNumber,
                    m_A05.CromatographyRetentionTime,
                    m_A05.Precursors.First().MZ,
                    m_A05.Precursors.First().Z);

                if (m_A000.PerformShotgunSearch == true) m_A08.ShotgunCandidate = m_A0001(m_A05);
                if (m_A000.PerformCleaveXLSearch == true) m_A08.CleaveCandidates = m_A0002(m_A06);
                if (m_A08.ShotgunCandidate == null && m_A08.CleaveCandidates == null) continue;
                m_A03.Add((m_A08, m_A07));
            }

            return m_A03;
        }
        private ShotgunCandidate m_A0001(MSUltraLight m_A00)
        {
            double m_A01 =
                (m_A00.Precursors[0].Item1 * m_A00.Precursors[0].Item2) -
                (m_A00.Precursors[0].Item2 - 1) * (float)SpectrumWizard.Utils.Chemistry.Proton;

            return new ShotgunCandidate(
                m_A00.ScanNumber,
                (float)m_A01,
                m_A000.MaxQueryResults);
        }
        private List<ICleaveCandidate> m_A0002(MSUltraLight m_A00)
        {
            List<ICleaveCandidate> m_A01 = null;

            if (m_A000.BDP_Mode)
            {
                PairFinderBDP m_A02 =
                    new(
                        m_A000.CXLReagent,
                        m_A000.PairFinderPPM,
                        1,
                        2,
                        m_A000.MaxQueryResults
                        );

                m_A01 = m_A02
                    .FindCleaveCandidates(m_A00)
                    .Cast<ICleaveCandidate>().ToList();
            }
            else
            {
                PairFinder m_A03 =
                new PairFinder(
                    m_A000.CXLReagent,
                    m_A000.PairFinderPPM,
                    m_A000.DeconvolutionForPairSearching ? 1 : m_A000.IonPairMaxCharge,
                    m_A000.IsotopicPossibilitiesPrecursor,
                    m_A000.MaxQueryResults,
                    false,
                    m_A000.CalculatePairIntensities,
                    0.012f,
                    m_A000.DeconvolutionForPairSearching,
                    null,
                    null
                    );

                m_A01 = m_A03
                    .FindCleaveCandidates(m_A00)
                    .Cast<ICleaveCandidate>().ToList();

            }

            //Looplink candidates
            if (m_A000.SearchLoopLinks)
            {
                if (m_A01 == null)
                    m_A01 = new();
                m_A01.AddRange(m_A0003(m_A00).Cast<ICleaveCandidate>().ToList());
            }

            return m_A01;
        }
        private List<CleaveCandidate> m_A0003(MSUltraLight m_A00)
        {
            if (m_A00.Precursors[0].Z > m_AB00) return new();

            var m_A01 = new List<CleaveCandidate>();
            double m_A02 = ChargeOperations.ToMH(m_A00.Precursors[0].MZ, m_A00.Precursors[0].Z) - m_A000.CXLReagent.WholeMass;

            for (int m_A03 = 0; m_A03 <= m_A000.IsotopicPossibilitiesPrecursor; m_A03++)
            {
                double m_A04 = ChargeOperations.ToMZ(m_A02 - m_A03 * 1.00335483778f, m_A00.Precursors[0].Z);

                IonPair m_A05 = new IonPair(
                    (m_A04, 0), m_A00.Precursors[0].Z,
                    null, 0,
                    0, 0);

                m_A01.Add(
                    new CleaveCandidate(
                        m_A00.ScanNumber,
                        m_A05,
                        new IonPair(),
                        false,
                        m_A000.MaxQueryResults,
                        m_A03,
                        true));

            }

            return m_A01;
        }
        private List<Query> m_A0004(List<(ScanResults, (int, double)[])> m_A00)
        {
            var m_A03 =
                m_A00.Select(a =>
                    new Query(
                        a.Item1.ScanNumber,
                        a.Item1.ShotgunCandidate.MH,
                        0,
                        a.Item2,
                        false,
                        false,
                        a.Item1.ShotgunCandidate.PSMs)).ToList();

            var m_A01 = m_A0006(m_A03).ToList();
            m_A03.AddRange(m_A01);

            if (m_A000.AddMinusOneIsotope)
            {
                var m_A04 = m_A0007(m_A03).ToList();
                m_A03.AddRange(m_A04);
            }

            m_A03 = m_A03.OrderBy(a => a.SearchMH).ToList();
            return m_A03;
        }
        private List<Query> m_A0005(List<(ScanResults, (int, double)[])> m_A00)
        {
            var m_A01 = new List<Query>(m_A00.Count);

            foreach (var m_A02 in m_A00)
            {
                foreach (var m_A03 in m_A02.Item1.CleaveCandidates)
                {
                    Query m_A04 = null;
                    if (m_A03.IsLoopLink == true)
                    {
                        m_A04 = new Query(
                            m_A02.Item1.ScanNumber,
                            m_A03.LightPairMH,
                            0,
                            m_A02.Item2,
                            true,
                            true,
                            m_A03.AllLightPSMs);

                    }
                    else
                    {
                        m_A04 = new Query(
                           m_A02.Item1.ScanNumber,
                           m_A03.LightPairMH,
                           0,
                           m_A02.Item2,
                           true,
                           false,
                           m_A03.AllLightPSMs);
                    }
                    m_A01.Add(m_A04);

                    if (m_A03.IsSymmetric == true || m_A03.IsLoopLink == true)
                        continue;

                    var m_A05 = new Query(
                        m_A02.Item1.ScanNumber,
                        m_A03.HeavyPairMH,
                        0,
                        m_A02.Item2,
                        true,
                        false,
                        m_A03.AllHeavyPSMs);
                    m_A01.Add(m_A05);
                }
            }

            var m_A06 = m_A0006(m_A01).ToList();
            m_A01.AddRange(m_A06);

            m_A01 = m_A01.OrderBy(a => a.SearchMH).ToList();

            return m_A01;
        }
        private IEnumerable<Query> m_A0006(IEnumerable<Query> m_A00)
        {
            var newq = (from q in m_A00
                        from i in Enumerable.Range(1, m_A000.IsotopicPossibilitiesPrecursor)
                        select new Query(
                            q.ScanNumber,
                            q.SearchMH - i * m_A000.CarbonIsotopeShift,
                            -1 * i,
                            q.SparseBinnedMS,
                            q.IsCleavable,
                            q.IsLoopLink,
                            q.PSMs));

            return newq;
        }
        private IEnumerable<Query> m_A0007(List<Query> m_A00)
        {
            var newq = (from q in m_A00
                        select new Query(
                            q.ScanNumber,
                            q.SearchMH + 1 * m_A000.CarbonIsotopeShift,
                            1,
                            q.SparseBinnedMS,
                            q.IsCleavable,
                            q.IsLoopLink,
                            q.PSMs));

            return newq;
        }
    }
}
