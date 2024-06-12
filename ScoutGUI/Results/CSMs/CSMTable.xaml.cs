using Digestor;
using ScoutCore;
using ScoutCore.SpectraOperations;
using Scout.Results.SpectrumViewer;
using ScoutPostProcessing;
using ScoutPostProcessing.CSMLogic;
using SpectrumWizard;
using System;
using System.Collections.Generic;
using System.IO;
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
using ScoutCore.PSMEngines;
using System.Threading;

namespace Scout.Results.CSMs
{
    /// <summary>
    /// Interaction logic for CSMTable.xaml
    /// </summary>
    public partial class CSMTable : UserControl
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
            public string AlphaPeptide { get; set; }
            public int AlphaPeptPosition { get; set; }
            public string AlphaProtPosition { get; set; }
            public string AlphaMods { get; set; }
            public string AlphaProteins { get; set; }
            public string AlphaGenes { get; set; }
            public double AlphaPeptMZ { get; set; }
            public string BetaPeptide { get; set; }
            public int BetaPeptPosition { get; set; }
            public string BetaProtPosition { get; set; }
            public string BetaMods { get; set; }
            public string BetaProteins { get; set; }
            public string BetaGenes { get; set; }
            public double BetaPeptMZ { get; set; }
            public double TheoreticalMZ { get; set; }
            public double ExperimentalMZ { get; set; }
            public double PrecursorPPM { get; set; }
            public double OldPPM { get; set; }
            public double AlphaPPMError { get; set; }
            public double BetaPPMError { get; set; }
            public double PrecursorCharge { get; set; }
            public string File { get; set; }
            public double Score { get; set; }
            public string QuantitationTag { get; set; }
            public string LinkType { get; set; }
            public string ReactionSitesProbability { get; set; }

            public DataGridItemCSM(ScoredCSM csm,
                Dictionary<int, AminoacidMod> varModsDict)
            {
                CSM = csm;

                Scan = CSM.ScanNumber;
                ExperimentalMZ = CSM.PrecursorMZ;
                TheoreticalMZ = CSM.TheoreticalMZ;
                AlphaPeptide = GetPeptideWithLinkPosition(CSM.AlphaPSM.Peptide.AsCleanString, CSM.AlphaPSM.ReagentPosition1);
                BetaPeptide = GetPeptideWithLinkPosition(CSM.BetaPSM.Peptide.AsCleanString, CSM.BetaPSM.ReagentPosition1);
                AlphaPeptMZ = ChargeOperations.ToMZ(CSM.AlphaPSM.Peptide.MH, CSM.PrecursorCharge);
                BetaPeptMZ = ChargeOperations.ToMZ(CSM.BetaPSM.Peptide.MH, CSM.PrecursorCharge);
                OldPPM = GetPPMFromDiffIsotopes();
                double? _precursorPPM;
                CSM.Scores.TryGetValue("PrecursorPPM", out _precursorPPM);
                PrecursorPPM = _precursorPPM != null ? _precursorPPM.Value : OldPPM;
                AlphaPPMError = Math.Abs(CSM.AlphaPPM);
                BetaPPMError = Math.Abs(CSM.BetaPPM);
                PrecursorCharge = CSM.PrecursorCharge;
                AlphaMods = String.Join(", ", GetModificationsString(CSM.AlphaPSM.Peptide, varModsDict));
                BetaMods = String.Join(", ", GetModificationsString(CSM.BetaPSM.Peptide, varModsDict));
                AlphaPeptPosition = CSM.AlphaPSM.ReagentPosition1 + 1;
                BetaPeptPosition = CSM.BetaPSM.ReagentPosition1 + 1;
                AlphaProtPosition = String.Join(", ", GetProtPositions(CSM.AlphaMappings, CSM.AlphaPSM.ReagentPosition1));
                BetaProtPosition = String.Join(", ", GetProtPositions(CSM.BetaMappings, CSM.BetaPSM.ReagentPosition1));
                AlphaProteins = String.Join(", ", CSM.AlphaMappings.Select(a => "sp|" + a.Locus + "|" + a.Gene).Distinct().ToList());
                BetaProteins = String.Join(", ", CSM.BetaMappings.Select(a => "sp|" + a.Locus + "|" + a.Gene).Distinct().ToList());
                AlphaGenes = String.Join(", ", CSM.AlphaMappings.Select(a => a.Gene).Distinct().ToList());
                BetaGenes = String.Join(", ", CSM.BetaMappings.Select(a => a.Gene).Distinct().ToList());
                //Score = (CSM.Scores["XLScore"].Value * 0.45 + CSM.Scores["MinDDPScore"].Value * 0.55);//MinDDPScore + MinFinalScore
                double? xl_score = 0;
                CSM.Scores.TryGetValue("XLScore", out xl_score);
                double? minDDP_score = 0;
                CSM.Scores.TryGetValue("MinDDPScore", out minDDP_score);
                Score = minDDP_score.Value * 0.25 + CSM.ClassificationScore * 0.55 + xl_score.Value * 0.2;//MinDDPScore + classification score + XLScore
                //Score = CSM.ClassificationScore;
                QuantitationTag = CSM.QuantitationTag;
                ShowQuantCol = !String.IsNullOrEmpty(QuantitationTag);
                File = CSM.FileName;
                LinkType = GetLinkType(CSM.IsInterLink, AlphaProteins, BetaProteins, AlphaProtPosition, BetaProtPosition);
            }

            private string GetLinkType(
                bool isInterLink,
                string alphaPtns,
                string betaPtns,
                string alphaProtPosition,
                string betaProtPosition)
            {
                if (isInterLink)
                    return "Heteromeric-inter";
                else
                {
                    if (alphaPtns.Equals(betaPtns) && alphaProtPosition.Equals(betaProtPosition))
                        return "Homomeric-inter";
                    else
                        return "Intra";
                }
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

            private string GetPeptideWithLinkPosition(string peptide, int position)
            {
                List<char> new_peptide = peptide.ToCharArray().ToList();

                new_peptide.Insert(position, ' ');
                new_peptide.Insert(position + 1, '[');
                new_peptide.Insert(position + 3, ']');
                new_peptide.Insert(position + 4, ' ');

                return new String(new_peptide.ToArray());
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
                Dictionary<int, AminoacidMod> modsDict)
            {
                if (peptide.Modifications == null || peptide.Modifications.Count == 0)
                    return new();

                List<string> mods = new();

                foreach (var (position, modIndex) in peptide.Modifications)
                {
                    AminoacidMod? mod = null;
                    modsDict.TryGetValue(modIndex, out mod);

                    if (mod == null) continue;
                    if (position == -1)
                        mods.Add("N-Term(" + mod.Name + ")");
                    else if (position == peptide.Length)
                        mods.Add("C-Term(" + mod.Name + ")");
                    else
                        mods.Add($"{peptide.ResidueTuples[position].aminoacid}{position + 1} ({mod.Name})");
                }

                return mods;
            }
        }

        public CSMTable()
        {
            InitializeComponent();
        }

        public void Load(
            List<ScoredCSM> csms,
            ScoutParameters scoutParameters,
            PostProcessingParameters postParameters)
        {
            CSMs = csms;
            CSMs.Sort((a, b) => b.ClassificationScore.CompareTo(a.ClassificationScore));
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
            DataGridCSMs.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {
                DataGridCSMs.ItemsSource = null;
                var datagridItems = CSMs.Where(a => a.IsLoopLink == false).Select(a => new DataGridItemCSM(a, _varModsDict)).ToList();
                datagridItems.Sort((a, b) => b.Score.CompareTo(a.Score));
                DataGridCSMs.ItemsSource = datagridItems;

            }));
        }


        private void DataGridCSMs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = ((DataGrid)e.Source).SelectedItem;
            if (item == null) return;

            var csm = ((DataGridItemCSM)item).CSM;

            CancellationTokenSource ts = new();
            var token = ts.Token;
            WindowSpectrumViewer window = new();
            if (window.LoadScan(token, csm, _searchParams))
                window.ShowDialog();
        }

        private void DataGridCSMs_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
    }
}
