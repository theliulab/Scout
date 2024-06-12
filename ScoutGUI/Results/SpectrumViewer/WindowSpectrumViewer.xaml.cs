using ScoutCore.PSMEngines;
using ScoutCore.QueryLogic;
using ScoutCore.Results;
using PatternTools.MSParserLight;
using SpectrumWizard;
using SpectrumWizard.Predictors.CleavableXL;
using SpectrumViewerConverters.SpectrumWizard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ScoutPostProcessing.Scoring;
using ScoutPostProcessing.CSMLogic;
using ScoutCore;
using ScoutCore.FileManagement;
using System.Threading;

namespace Scout.Results.SpectrumViewer
{
    /// <summary>
    /// Interaction logic for WindowSpectrumViewer.xaml
    /// </summary>
    public partial class WindowSpectrumViewer : Window
    {
        /// <summary>
        /// public variables
        /// </summary>

        public ScoutParameters SearchParams { get; set; }
        public Dictionary<(int, List<(float mz, short z)>), MSUltraLight> Spectra { get; private set; }
        //public Dictionary<int, MSUltraLight> Spectra { get; private set; }

        public ScoredCSM CurrentScan { get; private set; }
        public ScoredCSM CurrentCandidate { get; private set; }
        public CleavePSM CurrentAlpha { get; set; }
        public CleavePSM CurrentBeta { get; set; }
        public CleavePredictor AnnotationPredictor { get; private set; }
        public List<ScanResults> AllMS2 { get; private set; }
        public CleaveAnnotationSpectrum AnnotationSpectrum { get; private set; }

        public bool IsLoopLink { get; private set; }

        public WindowSpectrumViewer()
        {
            InitializeComponent();
        }

        private void MenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (IsLoaded == false) return;

            AnnotationSpectrum = PredictCleaveSpectrum();
            LoadSpectrumViewer();
            LoadTheoreticalIonsDataGrid();
            LoadPeptideAnnotation();
            LoadDeviationPlot();
        }

        private MSUltraLight GetExperimentalSpectrum(
            short precursorCharge,
            double precursorMZ,
            int scanNumber
            )
        {
            MSUltraLight _ms = Spectra.Where(a => a.Key.Item1 == scanNumber && a.Key.Item2.Any(b => (float)b.mz == (float)precursorMZ && b.z == precursorCharge)).FirstOrDefault().Value;
            if (_ms == null)
                _ms = new MSUltraLight();
            return _ms;
        }

        private void LoadTheoreticalIonsDataGrid()
        {
            var ms = GetExperimentalSpectrum(CurrentCandidate.PrecursorCharge, CurrentCandidate.PrecursorMZ, CurrentCandidate.ScanNumber);

            if (IsLoopLink)
                MyTheoreticalIonsTable.Load(ms, AnnotationSpectrum, CurrentAlpha.Peptide.AsCleanString);
            else
                MyTheoreticalIonsTable.Load(ms, AnnotationSpectrum, CurrentAlpha.Peptide.AsCleanString, CurrentBeta.Peptide.AsCleanString);
        }

        private string GetPeptideWithLinkPosition(string peptide, int position)
        {
            List<char> new_peptide = peptide.ToCharArray().ToList();

            new_peptide.Insert(position, ' ');
            new_peptide.Insert(position + 1, '[');
            new_peptide.Insert(position + 3, ']');
            new_peptide.Insert(position + 4, ' ');

            return new String(new_peptide.ToArray());
        }

        private void LoadPeptideAnnotation(bool fixedZoom = false)
        {
            if (AnnotationSpectrum == null) return;

            List<(char AA, string N_termSeries, string C_termSeries)> alpha_annotations = new();
            List<(char AA, string N_termSeries, string C_termSeries)> beta_annotations = null;

            List<char> originalMoleculeAlpha = AnnotationSpectrum.AlphaIons.Where(a => a.Series != SpectrumWizard.Predictors.FragmentSeries.Pair).GroupBy(a => a.Number).Select(a => a.First().FinalAA).ToList();
            List<char> originalMoleculeBeta = null;
            if (!IsLoopLink)
                originalMoleculeBeta = AnnotationSpectrum.BetaIons.Where(a => a.Series != SpectrumWizard.Predictors.FragmentSeries.Pair).GroupBy(a => a.Number).Select(a => a.First().FinalAA).ToList();

            double startMZ = 0;
            double endMZ = 0;
            if (fixedZoom == false)
            {
                endMZ = GetExperimentalSpectrum(CurrentCandidate.PrecursorCharge, CurrentCandidate.PrecursorMZ, CurrentCandidate.ScanNumber).Ions.Max(a => a.MZ);
            }
            else
            {
                (double minMZ, double maxMZ, double minIntensity, double maxIntensity) = SpectrumViewer.CurrentZoom;
                startMZ = minMZ;
                endMZ = maxMZ;
            }
            #region alpha peptide
            int total_ions = originalMoleculeAlpha.Count;
            for (int i = 0; i < total_ions; i++)
            {
                string a = "";
                string b = "";

                if (AnnotationSpectrum.AlphaIons.Exists(
                    x => x.MZ >= startMZ
                    && x.MZ <= endMZ
                    && x.Number == total_ions - i
                    && x.MatchedIons != null
                    && (x.Series == SpectrumWizard.Predictors.FragmentSeries.X || x.Series == SpectrumWizard.Predictors.FragmentSeries.Y || x.Series == SpectrumWizard.Predictors.FragmentSeries.Z)
                    ))
                {
                    a = "y1";
                }

                if (AnnotationSpectrum.AlphaIons.Exists(
                    x => x.MZ >= startMZ
                    && x.MZ <= endMZ
                    && x.Number == i + 1
                    && x.MatchedIons != null
                    && (x.Series == SpectrumWizard.Predictors.FragmentSeries.A || x.Series == SpectrumWizard.Predictors.FragmentSeries.B || x.Series == SpectrumWizard.Predictors.FragmentSeries.C)
                    ))
                {
                    b = "b2";
                }

                alpha_annotations.Add((originalMoleculeAlpha[i], a, b));

            }
            #endregion

            if (!IsLoopLink)
            {
                #region beta peptide
                beta_annotations = new();

                total_ions = originalMoleculeBeta.Count;
                for (int i = 0; i < total_ions; i++)
                {
                    string a = "";
                    string b = "";

                    if (AnnotationSpectrum.BetaIons.Exists(
                        x => x.MZ >= startMZ
                        && x.MZ <= endMZ
                        && x.Number == total_ions - i
                        && x.MatchedIons != null
                        && (x.Series == SpectrumWizard.Predictors.FragmentSeries.X || x.Series == SpectrumWizard.Predictors.FragmentSeries.Y || x.Series == SpectrumWizard.Predictors.FragmentSeries.Z)
                        ))
                    {
                        a = "y1";
                    }

                    if (AnnotationSpectrum.BetaIons.Exists(
                        x => x.MZ >= startMZ
                        && x.MZ <= endMZ
                        && x.Number == i + 1
                        && x.MatchedIons != null
                        && (x.Series == SpectrumWizard.Predictors.FragmentSeries.A || x.Series == SpectrumWizard.Predictors.FragmentSeries.B || x.Series == SpectrumWizard.Predictors.FragmentSeries.C)
                        ))
                    {
                        b = "b2";
                    }

                    beta_annotations.Add((originalMoleculeBeta[i], a, b));

                }
                #endregion

                _SequenceAnotation.Height = 180;
                _SequenceAnotation.Plot(alpha_annotations, beta_annotations, CurrentAlpha.ReagentPosition1, CurrentBeta.ReagentPosition1);
            }
            else
            {
                _SequenceAnotation.Height = 120;
                _SequenceAnotation.Plot(alpha_annotations, null, CurrentAlpha.ReagentPosition1, CurrentAlpha.ReagentPosition2);
            }

        }

        private void LoadDeviationPlot()
        {
            if (AnnotationSpectrum == null) return;
            if (IsLoopLink)
            {
                _DeviationPlot.Visibility = Visibility.Collapsed;
                _DeviationPlotLoopLink.Visibility = Visibility.Visible;
                _DeviationPlotLoopLink.Plot(GetExperimentalSpectrum(CurrentCandidate.PrecursorCharge, CurrentCandidate.PrecursorMZ, CurrentCandidate.ScanNumber), AnnotationSpectrum, SearchParams.PPMMS2Tolerance);
            }
            else
            {
                _DeviationPlotLoopLink.Visibility = Visibility.Collapsed;
                _DeviationPlot.Visibility = Visibility.Visible;
                _DeviationPlot.Plot(GetExperimentalSpectrum(CurrentCandidate.PrecursorCharge, CurrentCandidate.PrecursorMZ, CurrentCandidate.ScanNumber), AnnotationSpectrum, SearchParams.PPMMS2Tolerance);
            }
        }

        private void LoadSpectrumViewer(bool fixedZoom = false)
        {
            if (!IsLoopLink)
            {
                if (CurrentAlpha == null || CurrentBeta == null) return;
            }
            else if (CurrentAlpha == null) return;

            List<(double MZ, double Intensity)> experimentalIons = GetExperimentalSpectrum(CurrentCandidate.PrecursorCharge, CurrentCandidate.PrecursorMZ, CurrentCandidate.ScanNumber).Ions;

            if (fixedZoom == false)
            {
                SpectrumViewer.Load(experimentalIons, AnnotationSpectrum, SearchParams.PPMMS2Tolerance);
                SpectrumViewer.Plot(0, experimentalIons.Max(a => a.MZ));
            }
            else
            {
                (double minMZ, double maxMZ, double minIntensity, double maxIntensity) = SpectrumViewer.CurrentZoom;
                SpectrumViewer.Load(experimentalIons, AnnotationSpectrum, SearchParams.PPMMS2Tolerance);
                SpectrumViewer.Plot(minMZ, maxMZ, minIntensity, maxIntensity);
            }


            //SpectrumViewer.LoadSpectrum(experimentalIons, pToolsIons, 20);
        }

        private CleaveAnnotationSpectrum PredictCleaveSpectrum()
        {
            CleaveAnnotationSpectrum predicted = null;

            if (IsLoopLink)
                predicted = AnnotationPredictor.PredictAnnotationSpectrum(
                            CurrentAlpha.Peptide.ResidueTuples,
                            CurrentAlpha.Peptide.N_TerminalModMass,
                            CurrentAlpha.Peptide.C_TerminalModMass,
                            CurrentAlpha.ReagentPosition1,
                            CurrentAlpha.Peptide.ResidueTuples,
                            CurrentAlpha.Peptide.N_TerminalModMass,
                            CurrentAlpha.Peptide.C_TerminalModMass,
                            CurrentAlpha.ReagentPosition2,
                            SearchParams.CXLReagent);
            else
                predicted = AnnotationPredictor.PredictAnnotationSpectrum(
                            CurrentAlpha.Peptide.ResidueTuples,
                            CurrentAlpha.Peptide.N_TerminalModMass,
                            CurrentAlpha.Peptide.C_TerminalModMass,
                            CurrentAlpha.ReagentPosition1,
                            CurrentBeta.Peptide.ResidueTuples,
                            CurrentBeta.Peptide.N_TerminalModMass,
                            CurrentBeta.Peptide.C_TerminalModMass,
                            CurrentBeta.ReagentPosition1,
                            SearchParams.CXLReagent);

            var alphaPairs = predicted.AlphaIons
                .Where(a => a.Series == SpectrumWizard.Predictors.FragmentSeries.Pair)
                .ToList();

            List<AnnotationIon> betaPairs = null;
            if (!IsLoopLink)
                betaPairs = predicted.BetaIons
                .Where(a => a.Series == SpectrumWizard.Predictors.FragmentSeries.Pair)
                .ToList();

            //MH: 1612.738
            //var alphaPairs2 = PairUtils.GetPairIons(CurrentAlpha.Peptide.MH, Buf.Params.CXLReagent, 2, 1);

            if (CheckAlpha.IsChecked == false)
            {
                predicted.AlphaIons = new();
            }

            if (!IsLoopLink && CheckBeta.IsChecked == false)
            {
                predicted.BetaIons = new();
            }

            if (CheckChargeOne.IsChecked == false)
            {
                predicted.AlphaIons = predicted.AlphaIons
                    .Where(a => a.Charge != 1).ToList();

                if (!IsLoopLink)
                    predicted.BetaIons = predicted.BetaIons
                        .Where(a => a.Charge != 1).ToList();
            }

            if (CheckChargeTwo.IsChecked == false)
            {
                predicted.AlphaIons = predicted.AlphaIons
                    .Where(a => a.Charge != 2).ToList();

                if (!IsLoopLink)
                    predicted.BetaIons = predicted.BetaIons
                    .Where(a => a.Charge != 2).ToList();
            }

            if (CheckChargeThree.IsChecked == false)
            {
                predicted.AlphaIons = predicted.AlphaIons
                    .Where(a => a.Charge != 3).ToList();

                if (!IsLoopLink)
                    predicted.BetaIons = predicted.BetaIons
                    .Where(a => a.Charge != 3).ToList();
            }

            if (CheckIsotopes.IsChecked == false)
            {
                predicted.AlphaIons = predicted.AlphaIons
                    .Where(a => a.IsotopeNumber == 0).ToList();

                if (!IsLoopLink)
                    predicted.BetaIons = predicted.BetaIons
                    .Where(a => a.IsotopeNumber == 0).ToList();
            }

            if (CheckPairs.IsChecked == true)
            {
                predicted.AlphaIons = predicted.AlphaIons.Union(alphaPairs).ToList();

                if (!IsLoopLink)
                    predicted.BetaIons = predicted.BetaIons.Union(betaPairs).ToList();
            }
            else
            {
                predicted.AlphaIons = predicted.AlphaIons.Except(alphaPairs).ToList();

                if (!IsLoopLink)
                    predicted.BetaIons = predicted.BetaIons.Except(betaPairs).ToList();
            }

            return predicted;
        }

        public bool LoadScan(CancellationToken token, ScoredCSM newScan, ScoutParameters searchParams)
        {
            SearchParams = searchParams;
            if (LoadParams(token, newScan))
            {
                ResetSpectrumViewer();
                LoadPair(newScan);
                return true;
            }
            else return false;
        }

        private List<(float mz, short z)> GetPrecursors(List<(double mz, short z)> precursors)
        {
            List<(float mz, short z)> new_precursors = new();
            if (precursors == null)
                return new();


            return (from prec in precursors.AsParallel()
                    select ((float)(prec.mz), prec.z)).ToList();
        }

        private bool LoadParams(CancellationToken token, ScoredCSM newScan)
        {
            try
            {
                if (!String.IsNullOrEmpty(newScan.Spectrum))
                {
                    var spectrum = new List<MSUltraLight>() { ScoutCore.FileManagement.Serializer.FromJson<MSUltraLight>(newScan.Spectrum, false) };
                    Spectra = spectrum.ToDictionary(a => (a.ScanNumber, GetPrecursors(a.Precursors)), a => a);
                }
                else
                {
                    Spectra = SpectrumParser.Parse(token, newScan.FileName, true, 2, desiredScans: new List<int>() { newScan.ScanNumber }).ToDictionary(a => (a.ScanNumber, GetPrecursors(a.Precursors)), a => a);
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error to retrieve this spectrum!\n{ex.Message}", "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                return false;
            }
            var annotationPredParams = new CleavePredictionParameters()
            {
                IsotopeMax = 2,
                ChargeMax = 3,

                AddPrecursor = true,
                AddIonPairs = true,

                AddASeries = false,
                AddBSeries = true,
                AddCSeries = false,

                AddXSeries = false,
                AddYSeries = true,
                AddZSeries = false,

                NeutralLossH2O = true,
                NeutralLossNH3 = true,

                Reagent = SearchParams.CXLReagent,
                //StaticMods = SearchParams.StaticModifications,
                AlwaysAddExtraIons = true
            };
            AnnotationPredictor = new CleavePredictor(annotationPredParams);
            return true;
        }

        private void LoadPair(ScoredCSM cand)
        {
            CurrentCandidate = cand;
            CurrentAlpha = cand.AlphaPSM;
            CurrentBeta = cand.BetaPSM;
            IsLoopLink = cand.IsLoopLink;

            SpectrumViewer.Clear();

            LoopLinkViewer();
            AnnotationSpectrum = PredictCleaveSpectrum();

            LoadSpectrumViewer();
            LoadTheoreticalIonsDataGrid();
            LoadPeptideAnnotation();
            LoadDeviationPlot();
        }

        private void LoopLinkViewer()
        {
            if (IsLoopLink)
            {
                CheckBeta.Visibility = Visibility.Collapsed;
                CheckAlpha.Content = "peptide";
            }
        }

        private void ResetSpectrumViewer()
        {
            SpectrumViewer.Clear();
        }

        private void CheckTheoreticalMatchedOnly_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded == false) return;

            LoadTheoreticalIonsDataGrid();
        }
    }
}
