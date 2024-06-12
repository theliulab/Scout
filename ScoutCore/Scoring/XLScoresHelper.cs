using Accord.Statistics.Distributions.Univariate;
using Digestor;
using MoreLinq;
using Mpfr;
using ScoutCore;
using ScoutCore.PSMEngines;
using ScoutCore.QueryLogic;
using ScoutCore.SpectraOperations;
using PatternTools.MSParserLight;
using PatternTools.SpectraPrediction;
using SpectrumWizard;
using SpectrumWizard.Predictors.CleavableXL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using YCore;
using Accord.Collections;
using System.Runtime.Intrinsics.Arm;
using System.CodeDom;
using SpectrumWizard.Utils;
using ThermoFisher.CommonCore.Data.Business;
using PatternTools;
using Accord.Math;
using CSMSL.Spectral;
using System.Drawing;
using Mpfr.Structs;

namespace ScoutCore.Scoring
{
    public class XLScoresHelper
    {
        public ScoutParameters m__00A0 { get; private set; }

        private CleavePredictor m__00A1;
        private CleavePredictor m__00A2;
        private CleavePredictor m__00A3;
        private CleavePredictor m_00A4;

        public XLScoresHelper(ScoutParameters m__00A0)
        {
            this.m__00A0 = m__00A0;
            InitializePredictors();
        }

        private void InitializePredictors()
        {
            m__00A1 = new CleavePredictor(m__00A0.GetCleavePredictorParameters());

            m__00A2 = new CleavePredictor(
                new CleavePredictionParameters()
                {
                    IsotopeMax = m__00A0.DeconvolutionForMSScoring ? 0 : 1,
                    ChargeMax = m__00A0.DeconvolutionForMSScoring ? 1 : 2,

                    MaxMZ = m__00A0.DeconvolutionForMSScoring ? 10000 : 2000,
                    MinMZ = 0,

                    AlwaysAddExtraIons = false,
                    ExtraIonMassThreshold = 1000,

                    AddPrecursor = false,
                    AddIonPairs = false,

                    AddASeries = false,
                    AddBSeries = true,
                    AddCSeries = false,

                    AddXSeries = false,
                    AddYSeries = true,
                    AddZSeries = false,
                });

            m__00A3 = new CleavePredictor(
                m__00A0.GetCleavePredictorParameters()
                );

            m_00A4 = new CleavePredictor(
                new CleavePredictionParameters()
                {
                    NeutralLossH2O = false,
                    NeutralLossNH3 = false,
                    AlwaysAddExtraIons = false,
                    ChargeMax = 1,
                    IsotopeMax = 0,
                    ExtraIonMassThreshold = 1000000,
                    MinMZ = 200,
                    MaxMZ = 6000
                });
        }

        public Dictionary<string, double?> ScoreCSM(CleavePSM DgjAhPuw, CleaveReagent uHSGbzCd, int SYWeicZn, List<CleavePSM> UzXOfNER, (double mh, double intensity) kKHAPNnd, MSUltraLight cDDRKFOX, List<(string SequenceIdentifier, string Sequence)> UNRbFyeV)
        {
            int num = UzXOfNER.FindIndex((CleavePSM a) => a == DgjAhPuw) + 1;
            Dictionary<string, double?> _dict_000 = new Dictionary<string, double?>
            {
                {
                    "AlphaScore",
                    DgjAhPuw.Score
                },
                {
                    "BetaScore",
                    DgjAhPuw.Score
                },
                {
                    "MinScore",
                    DgjAhPuw.Score
                },
                {
                    "AlphaPPM",
                    Math.Abs(ScoringFunctions.GetPPM(DgjAhPuw.Peptide.MH, kKHAPNnd.mh, DgjAhPuw.IsotopeNumber, 1.0033547878265381))
                },
                {
                    "PrecursorPPM",
                    Math.Abs(ScoringFunctions.GetPPM(DgjAhPuw.Peptide.MH + uHSGbzCd.WholeMass + (double)((float)(SYWeicZn - DgjAhPuw.IsotopeNumber) * m__00A0.CarbonIsotopeShift), SpectraOperations.ChargeOperations.ToMH(cDDRKFOX.Precursors[0].Item1, (int)cDDRKFOX.Precursors[0].Item2, 1.00727647), 0, 1.0033547878265381))
                },
                {
                    "TotalIsotope",
                    SYWeicZn - DgjAhPuw.IsotopeNumber
                }
            };
            _dict_000.Add("DiffPPM", _dict_000["AlphaPPM"].Value);
            _dict_000.Add("MaxPPM", _dict_000["AlphaPPM"].Value);
            m__00A1.PredictSparseSpectrum(DgjAhPuw.Peptide.ResidueTuples, DgjAhPuw.Peptide.N_TerminalModMass, DgjAhPuw.Peptide.C_TerminalModMass, m__00A0.CXLReagent, DgjAhPuw.ReagentPosition1, DgjAhPuw.ReagentPosition2);
            List<IGrouping<bool?, CleavePSM>> list = (from x in UzXOfNER
                                                      group x by x.Peptide.IsDecoy).ToList();
            double? num2 = null;
            num2 = ((list.Count != 1) ? ScoringFunctions.GetDeltaCN(list[0].ToList().Skip(num - 1).ToList()) : ScoringFunctions.GetDeltaCN(UzXOfNER.Skip(num - 1).ToList()));
            _dict_000.Add("AlphaDeltaCN", num2);
            _dict_000.Add("BetaDeltaCN", num2);
            _dict_000.Add("MinDeltaCN", _dict_000["AlphaDeltaCN"].HasValue ? _dict_000["AlphaDeltaCN"].Value : 0.0);
            double vrCUdDYa;
            int CAIbMXLc;
            int qOXFbeNZ;
            double value = TagScoring.CalculateTagScore(DgjAhPuw.Peptide, cDDRKFOX.Ions, m__00A2.PredictTagSpectrum(DgjAhPuw.Peptide.ResidueTuples, DgjAhPuw.Peptide.N_TerminalModMass, DgjAhPuw.Peptide.C_TerminalModMass, DgjAhPuw.ReagentPosition1), m__00A0.PairFinderPPM, out vrCUdDYa, out CAIbMXLc, out qOXFbeNZ);
            _dict_000.Add("AlphaTagScore", value);
            _dict_000.Add("AlphaTagIntensity", vrCUdDYa);
            _dict_000.Add("AlphaTagLongest", qOXFbeNZ);
            _dict_000.Add("BetaTagScore", value);
            _dict_000.Add("BetaTagIntensity", vrCUdDYa);
            _dict_000.Add("BetaTagLongest", qOXFbeNZ);
            _dict_000.Add("MinTagScore", _dict_000["AlphaTagScore"].Value);
            _dict_000.Add("MinTagIntensity", _dict_000["AlphaTagIntensity"].Value);
            _dict_000.Add("AlphaDDPScore", DgjAhPuw.Score);
            _dict_000.Add("MinDDPScore", DgjAhPuw.Score);
            double num3 = cDDRKFOX.Ions.Sum<(double, double)>(((double, double) a) => a.Item2);
            _dict_000.Add("AlphaPairIntensity", kKHAPNnd.intensity);
            _dict_000.Add("AlphaPairIntensityNorm", kKHAPNnd.intensity / num3);
            _dict_000.Add("PairIntensityMulNorm", kKHAPNnd.intensity / Math.Pow(num3, 2.0));
            int pPYrTvvc = UNRbFyeV.Where(((string SequenceIdentifier, string Sequence) a) => !a.SequenceIdentifier.Contains(m__00A0.DecoyTag)).Count();
            _dict_000.Add("PoissonScore", CleanPoissonScore(cDDRKFOX, DgjAhPuw.Peptide, DgjAhPuw.ReagentPosition1, DgjAhPuw.ReagentPosition2, m_00A4, pPYrTvvc, m__00A0.CXLReagent, m__00A0.PPMMS2Tolerance));
            return _dict_000;
        }

        public Dictionary<string, double?> ScoreCSM(CleavePSM VmOdrwVK, CleavePSM MMkcJTLY, CleaveReagent BZWXbGYN, int KQhsRcrk, List<CleavePSM> hTCyNyHt, List<CleavePSM> OVTCnUkD, (double mh, double intensity) tVdIkkEa, (double mh, double intensity) UmDfLahS, MSUltraLight kELfcMRh, List<(string SequenceIdentifier, string Sequence)> PceKWJOr)
        {
            int num = hTCyNyHt.FindIndex((CleavePSM a) => a == VmOdrwVK) + 1;
            int num2 = OVTCnUkD.FindIndex((CleavePSM a) => a == MMkcJTLY) + 1;
            Dictionary<string, double?> _dict_001 = new Dictionary<string, double?>
            {
                {
                    "AlphaScore",
                    VmOdrwVK.Score
                },
                {
                    "BetaScore",
                    MMkcJTLY.Score
                },
                {
                    "MinScore",
                    Math.Min(VmOdrwVK.Score, MMkcJTLY.Score)
                },
                {
                    "AlphaPPM",
                    Math.Abs(ScoringFunctions.GetPPM(VmOdrwVK.Peptide.MH, tVdIkkEa.mh, VmOdrwVK.IsotopeNumber, 1.0033547878265381))
                },
                {
                    "BetaPPM",
                    Math.Abs(ScoringFunctions.GetPPM(MMkcJTLY.Peptide.MH, UmDfLahS.mh, MMkcJTLY.IsotopeNumber, 1.0033547878265381))
                },
                {
                    "PrecursorPPM",
                    Math.Abs(ScoringFunctions.GetPPM(VmOdrwVK.Peptide.MH + MMkcJTLY.Peptide.MH + BZWXbGYN.WholeMass - Chemistry.Hydrogen + (double)((float)(KQhsRcrk - VmOdrwVK.IsotopeNumber - MMkcJTLY.IsotopeNumber) * m__00A0.CarbonIsotopeShift), SpectraOperations.ChargeOperations.ToMH(kELfcMRh.Precursors[0].Item1, (int)kELfcMRh.Precursors[0].Item2, 1.00727647), 0, 1.0033547878265381))
                },
                {
                    "TotalIsotope",
                    KQhsRcrk - VmOdrwVK.IsotopeNumber - MMkcJTLY.IsotopeNumber
                }
            };
            _dict_001.Add("DiffPPM", Math.Abs(_dict_001["AlphaPPM"].Value - _dict_001["BetaPPM"].Value));
            _dict_001.Add("MaxPPM", Math.Max(Math.Abs(_dict_001["AlphaPPM"].Value), Math.Abs(_dict_001["BetaPPM"].Value)));
            List<(int, double)> rvVAZFxp = m__00A1.PredictSparseSpectrum(VmOdrwVK.Peptide.ResidueTuples, VmOdrwVK.Peptide.N_TerminalModMass, VmOdrwVK.Peptide.C_TerminalModMass, m__00A0.CXLReagent, VmOdrwVK.ReagentPosition1);
            List<(int, double)> fnejFsfF = m__00A1.PredictSparseSpectrum(MMkcJTLY.Peptide.ResidueTuples, MMkcJTLY.Peptide.N_TerminalModMass, MMkcJTLY.Peptide.C_TerminalModMass, m__00A0.CXLReagent, MMkcJTLY.ReagentPosition1);
            List<IGrouping<bool?, CleavePSM>> list = (from x in hTCyNyHt
                                                      group x by x.Peptide.IsDecoy).ToList();
            if (list.Count == 1) _dict_001.Add("AlphaDeltaCN", ScoringFunctions.GetDeltaCN(hTCyNyHt.Skip(num - 1).ToList()));
            else _dict_001.Add("AlphaDeltaCN", ScoringFunctions.GetDeltaCN(list[0].ToList().Skip(num - 1).ToList()));
            List<IGrouping<bool?, CleavePSM>> list2 = (from x in OVTCnUkD
                                                       group x by x.Peptide.IsDecoy).ToList();
            if (list2.Count == 1) _dict_001.Add("BetaDeltaCN", ScoringFunctions.GetDeltaCN(OVTCnUkD.Skip(num2 - 1).ToList()));
            else _dict_001.Add("BetaDeltaCN", ScoringFunctions.GetDeltaCN(list2[0].ToList().Skip(num2 - 1).ToList()));
            _dict_001.Add("MinDeltaCN", (_dict_001["AlphaDeltaCN"].HasValue && _dict_001["BetaDeltaCN"].HasValue) ? Math.Min(_dict_001["AlphaDeltaCN"].Value, _dict_001["BetaDeltaCN"].Value) : 0.0);
            double vrCUdDYa;
            int CAIbMXLc;
            int qOXFbeNZ;
            double num3 = TagScoring.CalculateTagScore(VmOdrwVK.Peptide, kELfcMRh.Ions, m__00A2.PredictTagSpectrum(VmOdrwVK.Peptide.ResidueTuples, VmOdrwVK.Peptide.N_TerminalModMass, VmOdrwVK.Peptide.C_TerminalModMass, VmOdrwVK.ReagentPosition1), m__00A0.PairFinderPPM, out vrCUdDYa, out CAIbMXLc, out qOXFbeNZ);
            _dict_001.Add("AlphaTagScore", num3);
            _dict_001.Add("AlphaTagIntensity", vrCUdDYa);
            _dict_001.Add("AlphaTagLongest", qOXFbeNZ);
            double vrCUdDYa2;
            int CAIbMXLc2;
            int qOXFbeNZ2;
            double num4 = TagScoring.CalculateTagScore(MMkcJTLY.Peptide, kELfcMRh.Ions, m__00A2.PredictTagSpectrum(MMkcJTLY.Peptide.ResidueTuples, MMkcJTLY.Peptide.N_TerminalModMass, MMkcJTLY.Peptide.C_TerminalModMass, MMkcJTLY.ReagentPosition1), m__00A0.PairFinderPPM, out vrCUdDYa2, out CAIbMXLc2, out qOXFbeNZ2);
            _dict_001.Add("BetaTagScore", num4);
            _dict_001.Add("BetaTagIntensity", vrCUdDYa2);
            _dict_001.Add("BetaTagLongest", qOXFbeNZ2);
            _dict_001.Add("MinTagScore", Math.Max(_dict_001["AlphaTagScore"].Value, _dict_001["BetaTagScore"].Value));
            _dict_001.Add("MinTagIntensity", Math.Max(_dict_001["AlphaTagIntensity"].Value, _dict_001["BetaTagIntensity"].Value));
            CalculateDecoupledScores(kELfcMRh.Ions, rvVAZFxp, fnejFsfF, m__00A1, out var HLYjmLrD, out var iSkWbEVV);
            _dict_001.Add("AlphaDDPScore", HLYjmLrD);
            _dict_001.Add("BetaDDPScore", iSkWbEVV);
            if (VmOdrwVK.Peptide.AsCleanString == MMkcJTLY.Peptide.AsCleanString) _dict_001.Add("MinDDPScore", VmOdrwVK.Score);
            else _dict_001.Add("MinDDPScore", Math.Min(HLYjmLrD, iSkWbEVV));
            int num5 = PceKWJOr.Where(((string SequenceIdentifier, string Sequence) a) => !a.SequenceIdentifier.Contains(m__00A0.DecoyTag)).Count();
            var (num6, num7) = PoissonScores.CalculateSuggestedScore(VmOdrwVK, VmOdrwVK.PeaksMatched, MMkcJTLY, MMkcJTLY.PeaksMatched, kELfcMRh.Ions.Count, num5);
            _dict_001.Add("XlinkxAlpha", num6);
            _dict_001.Add("XlinkxBeta", num7);
            _dict_001.Add("XlinkxScore", Math.Min(num6, num7));
            var (num8, num9) = PoissonScores.CalculateSuggestedScore(VmOdrwVK, CAIbMXLc, MMkcJTLY, CAIbMXLc2, (int)(2.0 / 3.0 * (double)kELfcMRh.Ions.Count), num5, 2.0);
            _dict_001.Add("XlinkxGroupedAlpha", num8);
            _dict_001.Add("XlinkxGroupedBeta", num9);
            _dict_001.Add("XlinkxGroupedScore", Math.Min(num8, num9));
            var (num10, num11) = PoissonScores.CalculateSuggestedScore(VmOdrwVK, CAIbMXLc + (int)num3 / 3, MMkcJTLY, CAIbMXLc2 + (int)num4 / 3, (int)(2.0 / 3.0 * (double)kELfcMRh.Ions.Count), num5, 2.0);
            _dict_001.Add("XlinkxTagAlpha", num10);
            _dict_001.Add("XlinkxTagBeta", num11);
            _dict_001.Add("XlinkxTagScore", Math.Min(num10, num11));
            double num12 = kELfcMRh.Ions.Sum<(double, double)>(((double, double) a) => a.Item2);
            _dict_001.Add("AlphaPairIntensity", tVdIkkEa.intensity);
            _dict_001.Add("AlphaPairIntensityNorm", tVdIkkEa.intensity / num12);
            _dict_001.Add("BetaPairIntensity", UmDfLahS.intensity);
            _dict_001.Add("BetaPairIntensityNorm", UmDfLahS.intensity / num12);
            _dict_001.Add("PairIntensityMulNorm", tVdIkkEa.intensity * UmDfLahS.intensity / Math.Pow(num12, 2.0));
            _dict_001.Add("PoissonScore", CleanPoissonScore(kELfcMRh, VmOdrwVK.Peptide, VmOdrwVK.ReagentPosition1, MMkcJTLY.Peptide, MMkcJTLY.ReagentPosition1, m_00A4, num5, m__00A0.CXLReagent, m__00A0.PPMMS2Tolerance));
            return _dict_001;
        }

        internal static double CleanPoissonScore(
           MSUltraLight? DgjAhPuw,
           Peptide uHSGbzCd,
           int SYWeicZn,
           int UzXOfNER,
           CleavePredictor kKHAPNnd,
           int cDDRKFOX,
           CleaveReagent UNRbFyeV,
           double MMkcJTLY)
        {

            var thMS = kKHAPNnd.PredictAnnotationSpectrum(
               uHSGbzCd.ResidueTuples,
               uHSGbzCd.N_TerminalModMass,
               uHSGbzCd.C_TerminalModMass,
               SYWeicZn,
               UzXOfNER,
               UNRbFyeV
               );

            var ions = DgjAhPuw.Ions.Where(a => a.Intensity > 1000).ToList();
            if (ions == null) return 1;

            var hashFeeT = new HashSet<string>();
            foreach (var fGroup in thMS.AlphaIons.GroupBy(a => $"{a.Series}{a.Number}"))
            {
                foreach (var f in fGroup)
                {
                    int? i = ScoutCore.SpectraOperations.PeakSearching.BinarySearchForMZ(ions, f.MZ, MMkcJTLY);
                    if (i != null)
                    {
                        hashFeeT.Add(fGroup.Key);
                        continue;
                    }
                }
            }

            double new_scoreP = new_scorep(
                uHSGbzCd.Length,
                DgjAhPuw.Ions.Count,
                hashFeeT.Count,
                8,
                0.02,
                cDDRKFOX
                );

            return new_scoreP;
        }

        internal static double CleanPoissonScore(
            MSUltraLight? VmOdrwVK,
            Peptide MMkcJTLY,
            int BZWXbGYN,
            Peptide KQhsRcrk,
            int hTCyNyHt,
            CleavePredictor OVTCnUkD,
            int tVdIkkEa,
            CleaveReagent UmDfLahS,
            double kELfcMRh)
        {
            var thMS = OVTCnUkD.PredictAnnotationSpectrum(
                MMkcJTLY.ResidueTuples,
                MMkcJTLY.N_TerminalModMass,
                MMkcJTLY.C_TerminalModMass,
                BZWXbGYN,
                KQhsRcrk.ResidueTuples,
                KQhsRcrk.N_TerminalModMass,
                KQhsRcrk.C_TerminalModMass,
                hTCyNyHt,
                UmDfLahS);


            List<(double, double)> iUndsX = VmOdrwVK.Ions.Where(a => a.Intensity > 1000).ToList();
            if (iUndsX.Count == 0)
            {
                double intensity_cutoff = VmOdrwVK.Ions.Max(a => a.Intensity) * 0.0055;
                iUndsX = VmOdrwVK.Ions.Where(a => a.Intensity > intensity_cutoff).ToList();
                iUndsX.RemoveAll(a => a.Item2 == 0);
                if (iUndsX.Count == 0) return 0;
            }

            var hasgOpdE = new HashSet<string>();
            foreach (var fGroup in thMS.AlphaIons.GroupBy(a => $"{a.Series}{a.Number}"))
            {
                foreach (var f in fGroup)
                {
                    int? i = ScoutCore.SpectraOperations.PeakSearching.BinarySearchForMZ(iUndsX, f.MZ, kELfcMRh);
                    if (i != null)
                    {
                        hasgOpdE.Add(fGroup.Key);
                        continue;
                    }
                }
            }


            var hashIedX = new HashSet<string>();
            foreach (var fGroup in thMS.BetaIons.GroupBy(a => $"{a.Series}{a.Number}"))
            {
                foreach (var f in fGroup)
                {
                    int? i = ScoutCore.SpectraOperations.PeakSearching.BinarySearchForMZ(iUndsX, f.MZ, kELfcMRh);
                    if (i != null)
                    {
                        hashIedX.Add(fGroup.Key);
                        continue;
                    }
                }
            }

            (double n3, double n4) = new_scorep(
            MMkcJTLY.Length,
            KQhsRcrk.Length,
            MMkcJTLY.CompareToPep(KQhsRcrk),
            VmOdrwVK.Ions.Count,
            hasgOpdE.Count,
            hashIedX.Count,
            8,
            0.02,
            tVdIkkEa
            );

            return Math.Min(n3, n4);
        }


        private static double new_scorep(int iZtNfaot, int pTYorJhH, int JYCCEBgB, int bzuMjnDj, double qLvZowFy, int rkUkyHCm)
        {
            double num = iZtNfaot;
            double pIkdmPFR = PoissonScores.GetPValueMPFR(num, num, pTYorJhH, JYCCEBgB, bzuMjnDj, qLvZowFy, rkUkyHCm, 500u, (mpfr_rnd_t)0);
            double num2 = -1.0 * Math.Log10(pIkdmPFR);
            if (num2 <= 0.0)
            {
                num2 = 0.0;
            }

            return num2;
        }

        private static (double, double) new_scorep(int zQWrmpSW, int ZEOtuDQP, bool vdIXgekv, int JKQRPMZb, int xXOlxxRi, int faAODDJF, int YWECClIC, double qZBNgSEE, int HWvOxylJ)
        {
            double num = zQWrmpSW;
            double num2 = ZEOtuDQP;
            double pIkdmPFR = PoissonScores.GetPValueMPFR(num, num2, JKQRPMZb, (!vdIXgekv) ? ((double)xXOlxxRi) : ((double)xXOlxxRi / 1.5), YWECClIC, qZBNgSEE, HWvOxylJ, 500u, (mpfr_rnd_t)0);
            double d = ((!vdIXgekv) ? PoissonScores.GetPValueMPFR(num2, num, JKQRPMZb, (!vdIXgekv) ? ((double)faAODDJF) : ((double)faAODDJF / 1.5), YWECClIC, qZBNgSEE, HWvOxylJ, 500u, (mpfr_rnd_t)0) : pIkdmPFR);
            double num3 = -1.0 * Math.Log10(pIkdmPFR);
            double num4 = -1.0 * Math.Log10(d);
            if (num3 <= 0.0 || num4 <= 0.0)
            {
                num3 = 0.0;
                num4 = 0.0;
            }

            return (num3, num4);
        }

        public void CalculateDecoupledScores(List<(double, double)> oZzdNgoL, List<(int, double)> RvVAZFxp, List<(int, double)> FnejFsfF, CleavePredictor MRbPMxgG, out double HLYjmLrD, out double iSkWbEVV)
        {
            Transformer.RemoveMatchingBins(RvVAZFxp, FnejFsfF, out RvVAZFxp, out FnejFsfF);
            (HLYjmLrD, iSkWbEVV) = DotProductScore(oZzdNgoL, RvVAZFxp, FnejFsfF, m__00A0);
        }

        public static (double, double) DotProductScore(List<(double, double)> nHLovNDS, List<(int, double)> kAFeGXOu, List<(int, double)> vJnemedN, ScoutParameters UIYTJXBz)
        {
            (int, double)[] array = SearchPackFactory.PrepareSparseExperimentalSpectrum(nHLovNDS, UIYTJXBz);

            if ((int)UIYTJXBz.ScoreFunction == 1)
            {
                Transformer.DivideByNorm(kAFeGXOu);
                Transformer.DivideByNorm(vJnemedN);
            }
            int num = default(int);
            double item = ScoringFunctions.SparseDotProduct(kAFeGXOu, array, out num, (List<int>)null);
            int num2 = default(int);
            double item2 = ScoringFunctions.SparseDotProduct(vJnemedN, array, out num2, (List<int>)null);
            return (item, item2);
        }

        public static double DotProductScore(List<(double, double)> cCoOFkQf, List<(int, double)> WJxqdkQR, ScoutParameters KuZdrVCF)
        {
            (int, double)[] array = SearchPackFactory.PrepareSparseExperimentalSpectrum(cCoOFkQf, KuZdrVCF);
            if ((int)KuZdrVCF.ScoreFunction == 1) Transformer.DivideByNorm(WJxqdkQR);

            int num = default(int);
            return ScoringFunctions.SparseDotProduct(WJxqdkQR, array, out num, (List<int>)null);
        }
    }
}
