using ScoutCore;
using ScoutPostProcessing.CSMLogic;
using ScoutPostProcessing;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using SpectrumWizard;
using Digestor;
using ScoutCore.SpectraOperations;
using Scout.Results.SpectrumViewer;
using ScoutCore.PSMEngines;
using System.Threading;

namespace Scout.Results.CSMs
{
    /// <summary>
    /// Interaction logic for LoopLinkTable.xaml
    /// </summary>
    public partial class LoopLinkTable : UserControl
    {
        private Dictionary<int, AminoacidMod> _varModsDict;
        //private Dictionary<char, List<AminoacidMod>> _staticModsDict;
        public static bool ShowQuantCol { get; set; } = false;

        public List<ScoredCSM> CSMs { get; private set; }

        private static ScoutParameters _searchParams;
        private static PostProcessingParameters _postParams;

        public class DataGridItemCSM
        {
            public ScoredCSM CSM { get; set; }

            public int Scan { get; set; }
            public string Peptide { get; set; }
            public int PeptPosition1 { get; set; }
            public int PeptPosition2 { get; set; }
            public string ProtPosition1 { get; set; }
            public string ProtPosition2 { get; set; }
            public string Mods { get; set; }
            public string Proteins { get; set; }
            public string Genes { get; set; }
            public double PeptMZ { get; set; }
            public double TheoreticalMZ { get; set; }
            public double ExperimentalMZ { get; set; }
            public double PrecursorPPM { get; set; }
            public double OldPPM { get; set; }
            public double PPMError { get; set; }
            public double PrecursorCharge { get; set; }
            public string File { get; set; }
            public double Score { get; set; }
            public string QuantitationTag { get; set; }
            public string ReactionSitesProbability { get; set; }

            public DataGridItemCSM(ScoredCSM csm,
                Dictionary<int, AminoacidMod> varModsDict)
            {
                CSM = csm;

                Scan = CSM.ScanNumber;
                ExperimentalMZ = CSM.PrecursorMZ;
                TheoreticalMZ = CSM.TheoreticalMZ;
                Peptide = GetPeptideWithLinkPosition(CSM.AlphaPSM.Peptide.AsCleanString, CSM.AlphaPSM.ReagentPosition1, CSM.AlphaPSM.ReagentPosition2);
                PeptMZ = ChargeOperations.ToMZ(CSM.AlphaPSM.Peptide.MH, CSM.PrecursorCharge);
                OldPPM = GetPPMFromDiffIsotopes();
                double? _precursorPPM;
                CSM.Scores.TryGetValue("PrecursorPPM", out _precursorPPM);
                PrecursorPPM = _precursorPPM != null ? _precursorPPM.Value : OldPPM;
                PPMError = Math.Abs(CSM.AlphaPPM);
                PrecursorCharge = CSM.PrecursorCharge;
                Mods = String.Join(", ", GetModificationsString(CSM.AlphaPSM.Peptide, varModsDict));
                PeptPosition1 = CSM.AlphaPSM.ReagentPosition1 + 1;
                PeptPosition2 = CSM.AlphaPSM.ReagentPosition2 + 1;
                ProtPosition1 = String.Join(", ", GetProtPositions(CSM.AlphaMappings, CSM.AlphaPSM.ReagentPosition1));
                ProtPosition2 = String.Join(", ", GetProtPositions(CSM.AlphaMappings, CSM.AlphaPSM.ReagentPosition2));
                Proteins = String.Join(", ", CSM.AlphaMappings.Select(a => "sp|" + a.Locus + "|" + a.Gene).Distinct().ToList());
                Genes = String.Join(", ", CSM.AlphaMappings.Select(a => a.Gene).Distinct().ToList());
                double? xl_score = 0;
                CSM.Scores.TryGetValue("XLScore", out xl_score);
                double? poisson_score = 0;
                CSM.Scores.TryGetValue("PoissonScore", out poisson_score);
                Score = poisson_score.Value * 0.025 + CSM.ClassificationScore * 0.1 + xl_score.Value * 0.5;//PoissonScore + classification score + XLScore
                //Score = CSM.ClassificationScore;
                QuantitationTag = CSM.QuantitationTag;
                ShowQuantCol = !String.IsNullOrEmpty(QuantitationTag);
                File = CSM.FileName;

            }

            private List<int> GetProtPositions(List<PeptideMapping> peptideMappings, int reagent_position)
            {
                List<int> positions = new List<int>();

                foreach (var bestMapping in peptideMappings)
                {
                    positions.Add(bestMapping.ProteinPosition + reagent_position + 1);
                }

                return positions;
            }

            private double GetPPMFromDiffIsotopes()
            {
                if (ExperimentalMZ == 0 || TheoreticalMZ == 0)
                    return double.NaN;

                double expMH = ChargeOperations.ToMH(ExperimentalMZ, CSM.PrecursorCharge);
                double theorMH = ChargeOperations.ToMH(TheoreticalMZ, CSM.PrecursorCharge);
                int isotopes = (int)Math.Floor(Math.Abs(expMH - theorMH));
                double ppm = 0;
                if (Math.Abs(isotopes) > 15)
                    return double.NaN;
                do
                {
                    ppm = Math.Abs(ScoringFunctions.GetPPM(theorMH, expMH, isotopes));
                    if (theorMH > expMH)
                        isotopes++;
                    else
                        isotopes--;
                } while (ppm > 100);
                return ppm;
            }

            private string GetPeptideWithLinkPosition(string peptide, int position1, int position2)
            {
                List<char> new_peptide = peptide.ToCharArray().ToList();

                new_peptide.Insert(position1, ' ');
                new_peptide.Insert(position1 + 1, '[');
                new_peptide.Insert(position1 + 3, ']');
                new_peptide.Insert(position1 + 4, ' ');

                new_peptide.Insert(position2 + 4, ' ');
                new_peptide.Insert(position2 + 5, '[');
                new_peptide.Insert(position2 + 7, ']');
                new_peptide.Insert(position2 + 8, ' ');

                return new String(new_peptide.ToArray());
            }

            private List<string> GetModificationsString(
                Peptide peptide,
                Dictionary<int, AminoacidMod> varModsDict)
            {
                if (peptide.Modifications == null || peptide.Modifications.Count == 0)
                    return new();

                List<string> mods = new();

                foreach (var (position, modIndex) in peptide.Modifications)
                {
                    AminoacidMod? mod = null;
                    varModsDict.TryGetValue(modIndex, out mod);

                    if (mod == null) continue;
                    if (mod.IsNTerm && position == 0)
                        mods.Add("N-Term(" + mod.Name + ")");
                    else if (mod.IsCTerm && position == peptide.Length - 1)
                        mods.Add("C-Term(" + mod.Name + ")");
                    else
                        mods.Add($"{peptide.ResidueTuples[position].aminoacid}{position + 1} ({mod.Name})");
                }

                return mods;
            }
        }

        public LoopLinkTable()
        {
            InitializeComponent();
        }

        public void Load(
           List<ScoredCSM> csms,
           ScoutParameters scoutParameters,
           PostProcessingParameters postParameters)
        {
            CSMs = csms;
            _searchParams = scoutParameters;
            _postParams = postParameters;

            BuildModificationsDictionaries();
            LoadDataGrid();
        }

        private void BuildModificationsDictionaries()
        {
            var mods = _searchParams.VariableModifications
                .Concat(_searchParams.StaticModifications);

            if (_searchParams.IsobaricLabelling_Search &&
                _searchParams.IsobaricLabelling_Mods != null)
                mods = mods.Concat(_searchParams.IsobaricLabelling_Mods);

            _varModsDict = mods.ToDictionary(a => a.ModIndex, b => b);
        }

        private void LoadDataGrid()
        {
            DataGridLoopLinks.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {
                DataGridLoopLinks.ItemsSource = null;
                var datagridItems = CSMs.Where(a => a.IsLoopLink).Select(a => new DataGridItemCSM(a, _varModsDict)).ToList();
                datagridItems.Sort((a, b) => b.Score.CompareTo(a.Score));
                DataGridLoopLinks.ItemsSource = datagridItems;

            }));

            //if (ShowQuantCol)
            //    DataGridLoopLinks.Columns[10].Visibility = Visibility.Visible;
            //else
            //    v.Columns[10].Visibility = Visibility.Collapsed;
        }

        private void DataGridLoopLinks_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = ((DataGrid)e.Source).SelectedItem;
            if (item == null) return;

            var csm = ((DataGridItemCSM)item).CSM;

            WindowSpectrumViewer window = new();
            CancellationTokenSource? ts = new();
            var token = ts.Token;

            if (window.LoadScan(token, csm, _searchParams))
                window.ShowDialog();
        }
        private void DataGridLoopLinks_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
    }
}
