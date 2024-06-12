using Digestor;
using ScoutCore;
using Scout.Results.CSMs;
using ScoutPostProcessing;
using ScoutPostProcessing.CSMLogic;
using SpectrumWizard;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Xml.Linq;
using Xceed.Wpf.Toolkit.Primitives;

namespace Scout.Results.CSMs
{
    /// <summary>
    /// Interaction logic for ControlCSMResultsNew.xaml
    /// </summary>
    public partial class ControlCSMResultsNew : UserControl
    {
        public const string ALL_FILES_COMBOBOX = "All files";
        public static List<ScoredCSM> GuiCSMs { get; private set; }
        private List<ScoredCSM> OriginalGuiCSMs { get; set; }
        private bool IsOnlyInter { get; set; } = false;
        private bool IsDecoy { get; set; } = false;

        public static bool ShowQuantCol { get; set; } = false;

        private XLFilteredResults _post;
        private ScoutParameters _searchParams;
        private PostProcessingParameters _postParams;

        private Dictionary<int, AminoacidMod> _varModsDict;
        private Dictionary<char, List<AminoacidMod>> _staticModsDict;

        public ObservableCollection<FileItemCheckBox> FileOptions { get; set; }
        //public List<FileItemCheckBox> FileOptions { get; set; }

        public class DataGridItemCSM
        {
            public ScoredCSM CSM { get; set; }

            public int Scan { get; set; }
            public string AlphaPeptide { get; set; }
            public int AlphaPeptPosition { get; set; }
            public int AlphaProtPosition { get; set; }
            public string AlphaMods { get; set; }
            public string AlphaProteins { get; set; }
            public string BetaPeptide { get; set; }
            public int BetaPeptPosition { get; set; }
            public int BetaProtPosition { get; set; }
            public string BetaMods { get; set; }
            public string BetaProteins { get; set; }
            public string File { get; set; }
            public double Score { get; set; }
            public string QuantitationTag { get; set; }

            public DataGridItemCSM(ScoredCSM csm,
                Dictionary<int, AminoacidMod> varModsDict,
                Dictionary<char, List<AminoacidMod>> statModsDict
                )
            {
                CSM = csm;

                Scan = CSM.ScanNumber;
                AlphaPeptide = GetPeptideWithLinkPosition(CSM.AlphaPSM.Peptide.AsCleanString, CSM.AlphaPSM.ReagentPosition1);
                BetaPeptide = GetPeptideWithLinkPosition(CSM.BetaPSM.Peptide.AsCleanString, CSM.BetaPSM.ReagentPosition1);
                AlphaPeptPosition = CSM.AlphaPSM.ReagentPosition1 + 1;
                BetaPeptPosition = CSM.BetaPSM.ReagentPosition1 + 1;

                PeptideMapping bestMappingAlpha = CSM.AlphaMappings[0];
                PeptideMapping bestMappingBeta = CSM.BetaMappings[0];

                int alphaAminoacidPosition = bestMappingAlpha.ProteinPosition + CSM.AlphaPSM.ReagentPosition1 + 1;
                int betaAminoacidPosition = bestMappingBeta.ProteinPosition + CSM.BetaPSM.ReagentPosition1 + 1;

                AlphaPeptPosition = CSM.AlphaPSM.ReagentPosition1 + 1;
                BetaPeptPosition = CSM.BetaPSM.ReagentPosition1 + 1;
                AlphaProtPosition = alphaAminoacidPosition;
                BetaProtPosition = betaAminoacidPosition;
                AlphaProteins = String.Join(',', CSM.AlphaMappings.Select(a => a.Locus));
                BetaProteins = String.Join(',', CSM.BetaMappings.Select(a => a.Locus));
                AlphaMods = GetModificationsString(CSM.AlphaPSM.Peptide, varModsDict, statModsDict);
                BetaMods = GetModificationsString(CSM.BetaPSM.Peptide, varModsDict, statModsDict);
                File = CSM.FileName;
                Score = CSM.ClassificationScore;
                QuantitationTag = CSM.QuantitationTag;
                ShowQuantCol = !String.IsNullOrEmpty(QuantitationTag);
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

            private string GetModificationsString(
                Peptide peptide,
                Dictionary<int, AminoacidMod> varModsDict,
                Dictionary<char, List<AminoacidMod>> statModsDict)
            {
                if (peptide.Modifications == null || peptide.Modifications.Count == 0)
                    return "";

                StringBuilder sbMods = new();

                foreach (var (position, modIndex) in peptide.Modifications)
                {
                    var mod = varModsDict[modIndex];

                    if (mod.IsNTerm && position == 0)
                        sbMods.Append("N-Term(" + mod.Name + "); ");
                    else if (mod.IsCTerm && position == peptide.Length - 1)
                        sbMods.Append("C-Term(" + mod.Name + "); ");
                    else
                        sbMods.Append($"{peptide.ResidueTuples[position].aminoacid}{position + 1} ({mod.Name}); ");
                }

                for (int i = 0; i < peptide.ResidueTuples.Length; i++)
                {
                    (char aminoacid, double modMass) = peptide.ResidueTuples[i];
                    int position = i;

                    if (statModsDict.ContainsKey(aminoacid) == false) continue;

                    var staticMods = statModsDict[aminoacid];

                    foreach (var mod in staticMods)
                    {
                        sbMods.Append($"{peptide.ResidueTuples[position].aminoacid}{position + 1} ({mod.Name}); ");
                    }
                }


                return sbMods.ToString();
            }
        }

        public class FileItemCheckBox : INotifyPropertyChanged
        {
            private bool _selected;
            private string? _name;

            public string Name
            {
                get => _name; set
                {
                    _name = value;
                    EmitChange(nameof(Name));
                }
            }
            public bool Selected
            {
                get => _selected; set
                {
                    _selected = value;
                    EmitChange(nameof(Selected));
                }
            }

            private void EmitChange(params string[] names)
            {
                if (PropertyChanged == null)
                    return;
                foreach (var name in names)
                    PropertyChanged(this,
                      new PropertyChangedEventArgs(name));
            }

            private void NotifyPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            public event PropertyChangedEventHandler? PropertyChanged;

        }

        private string[] GetFileNamesFromCSMs()
        {
            return (from csm in GuiCSMs.AsParallel()
                    select System.IO.Path.GetFileName(csm.FileName)).Distinct().OrderBy(s => s).ToArray();
        }


        private void FillFileCheckBox()
        {
            try
            {
                FileComboBox.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { FileComboBox.SelectedValue = null; }));
                FileComboBox.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { FileComboBox.SelectedItem = null; }));
                FileComboBox.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { FileComboBox.DataContext = null; }));

                var items = GetFileNamesFromCSMs();
                FileOptions = new();
                FileOptions.Add(new FileItemCheckBox { Name = ALL_FILES_COMBOBOX, Selected = true });
                foreach (var item in items)
                    FileOptions.Add(new FileItemCheckBox { Name = item, Selected = false });


                FileComboBox.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { FileComboBox.DataContext = this; }));

            }
            catch (InvalidOperationException)
            {
            }
        }

        public ControlCSMResultsNew()
        {
            InitializeComponent();
            SetTextPeptWidth();
        }

        private void SetTextPeptWidth()
        {
            double _width = this.ActualWidth * 0.38;
            if (_width == 0)
                _width = SystemParameters.FullPrimaryScreenWidth * 0.38;
            TextPept.MinWidth = _width;
            TextPept.Width = _width;
        }

        public void Load(
            XLFilteredResults post)
        {
            _post = post;

            _searchParams = post.SearchParams;
            _postParams = post.PostParams;

            GuiCSMs = _post.PackageCSMs.FilteredCSMs;
            GuiCSMs.AddRange(_post.PackageCSMsLoopLinks.FilteredCSMs);

            OriginalGuiCSMs = new List<ScoredCSM>(GuiCSMs);

            FillFileCheckBox();
            Filter();

            FDR_SeparatedOrCombined_Label.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {

                if (_post.PostParams.FDRMode == FDRModes.CombinedIntraInter)
                    FDR_SeparatedOrCombined_Label.Text = "FDR cross-links:";
                else
                    FDR_SeparatedOrCombined_Label.Text = "FDR inter- and intra-links:";
            }
            ));

            TextFDR_CrossLinks.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {

                if (_post.PostParams.FDRMode == FDRModes.CombinedIntraInter)
                    TextFDR_CrossLinks.Text = _post.PackageCSMs.MeasuredFDR_Combined.ToString("N4");
                else
                    TextFDR_CrossLinks.Text = _post.PackageCSMs.MeasuredFDR_Separate_Interlink.ToString("N4") + " and " + _post.PackageCSMs.MeasuredFDR_Separate_Intralink.ToString("N4");
            }
            ));

            TextFDR_LoopLinks.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {
                TextFDR_LoopLinks.Text = _post.PackageCSMsLoopLinks.MeasuredFDR_Combined.ToString("N4");
            }
            ));

            TextCrosslinkIDsCount.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { TextCrosslinkIDsCount.Text = GuiCSMs.Where(a => !a.IsLoopLink).ToList().Count.ToString(); }));
            TextLooplinkIDsCount.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { TextLooplinkIDsCount.Text = GuiCSMs.Where(a => a.IsLoopLink).ToList().Count.ToString(); }));
        }

        private void CheckInterOnly_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded == false) return;

            if (((CheckBox)sender).IsChecked == true)
            {
                IsOnlyInter = true;
            }
            else
            {
                IsOnlyInter = false;
            }

            Filter();
        }
        private void Filter()
        {
            #region filters
            GuiCSMs = new List<ScoredCSM>(OriginalGuiCSMs);

            if (IsOnlyInter)
                GuiCSMs = GuiCSMs.Where(a => a.IsInterLink == true).ToList();

            if (!IsDecoy)
                GuiCSMs = GuiCSMs.Where(a => a.IsDecoy == false).ToList();

            List<string> selectedFiles = new List<string>(FileComboBox.SelectedItems.Count);
            if (FileComboBox.SelectedItems != null)
            {
                foreach (var file in FileComboBox.SelectedItems) selectedFiles.Add(((FileItemCheckBox)file).Name);
                selectedFiles = selectedFiles.Distinct().ToList();
            }

            if (selectedFiles.Count == 0)//'All files' needs to be selected
            {
                FileComboBox.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { FileComboBox.SelectedItem = FileOptions[0]; }));
            }

            if (!(selectedFiles == null || selectedFiles.Count == 0) && !selectedFiles.Contains(ALL_FILES_COMBOBOX))
            {
                GuiCSMs = GuiCSMs.Where(a => selectedFiles.Contains(System.IO.Path.GetFileName(a.FileName))).ToList();
            }

            int scanNumber = -1;
            ScanNumber.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {

                if (ScanNumber.Value != null &&
                 !String.IsNullOrEmpty(ScanNumber.Value.ToString()) &&
                 !ScanNumber.Value.ToString().Equals("Scan number"))
                {
                    scanNumber = (int)ScanNumber.Value;
                    GuiCSMs = GuiCSMs.Where(a => a.ScanNumber == scanNumber).ToList();
                }

            }));

            double score = -1;
            ScoreThreshold.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {

                if (ScoreThreshold.Value != null &&
               !String.IsNullOrEmpty(ScoreThreshold.Value.ToString()) &&
               !ScoreThreshold.Text.ToString().Equals("0"))
                {
                    score = (double)ScoreThreshold.Value;
                    GuiCSMs = GuiCSMs.Where(a => a.ClassificationScore >= score).ToList();
                }

            }));

            string sequence = "";
            TextPept.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {

                if (TextPept.Text != null &&
                !String.IsNullOrEmpty(TextPept.Text.ToString()) &&
                TextPept.Text.ToString().Length > 3)
                {
                    sequence = TextPept.Text.ToLower();
                    List<ScoredCSM> alphaPeptides = null;
                    List<ScoredCSM> betaPeptides = null;
                    List<ScoredCSM> firstProtein = null;
                    List<ScoredCSM> secondProtein = null;
                    List<ScoredCSM> firstGene = null;
                    List<ScoredCSM> secondGene = null;

                    string alpha = "";
                    string beta = "";
                    if (sequence.Contains("-"))
                    {
                        alpha = sequence.Split('-')[0];
                        beta = sequence.Split('-')[1];

                        alphaPeptides = GuiCSMs.Where(a => !a.IsLoopLink &&
                                                           a.AlphaPSM.Peptide.AsCleanString.ToLower().Contains(alpha) &&
                                                           a.BetaPSM.Peptide.AsCleanString.ToLower().Contains(beta)).ToList();
                    }
                    else
                    {
                        alphaPeptides = GuiCSMs.Where(a => a.AlphaPSM.Peptide.AsCleanString.ToLower().Contains(sequence)).ToList();
                        betaPeptides = GuiCSMs.Where(a => !a.IsLoopLink && a.BetaPSM.Peptide.AsCleanString.ToLower().Contains(sequence)).ToList();
                        firstProtein = GuiCSMs.Where(a => a.AlphaMappings.Any(b => b.Locus != null && b.Locus.ToLower().Contains(sequence))).ToList();
                        secondProtein = GuiCSMs.Where(a => !a.IsLoopLink && a.BetaMappings.Any(b => b.Locus != null && b.Locus.ToLower().Contains(sequence))).ToList();
                        firstGene = GuiCSMs.Where(a => a.AlphaMappings.Any(b => b.Gene != null && b.Gene.ToLower().Contains(sequence))).ToList();
                        secondGene = GuiCSMs.Where(a => !a.IsLoopLink && a.BetaMappings.Any(b => b.Gene != null && b.Gene.ToLower().Contains(sequence))).ToList();
                    }
                    GuiCSMs.Clear();
                    if (alphaPeptides != null && alphaPeptides.Count > 0)
                        GuiCSMs.AddRange(alphaPeptides);
                    if (betaPeptides != null && betaPeptides.Count > 0)
                        GuiCSMs.AddRange(betaPeptides);
                    if (firstProtein != null && firstProtein.Count > 0)
                        GuiCSMs.AddRange(firstProtein);
                    if (secondProtein != null && secondProtein.Count > 0)
                        GuiCSMs.AddRange(secondProtein);
                    if (firstGene != null && firstGene.Count > 0)
                        GuiCSMs.AddRange(firstGene);
                    if (secondGene != null && secondGene.Count > 0)
                        GuiCSMs.AddRange(secondGene);

                    GuiCSMs = GuiCSMs.Distinct().ToList();
                }

            }));

            #endregion

            MyCSMTable.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { MyCSMTable.Load(GuiCSMs, _searchParams, _postParams); }));
            MyLoopLinkTable.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { MyLoopLinkTable.Load(GuiCSMs, _searchParams, _postParams); }));

            TextCrosslinkIDsCount.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { TextCrosslinkIDsCount.Text = GuiCSMs.Where(a => !a.IsLoopLink).ToList().Count.ToString(); }));
            TextLooplinkIDsCount.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { TextLooplinkIDsCount.Text = GuiCSMs.Where(a => a.IsLoopLink).ToList().Count.ToString(); }));


        }
        private void ButtonFilter_Click(object sender, RoutedEventArgs e)
        {
            Filter();
        }

        private void ButtonResetFilter_Click(object sender, RoutedEventArgs e)
        {
            #region reset filters
            ScanNumber.Value = 0;
            ScanNumber.Text = "";
            ScanNumber.Watermark = "Scan number";
            ScoreThreshold.Value = 0;
            TextPept.Text = "";
            CheckInterOnly.IsChecked = false;
            CheckShowDecoys.IsChecked = false;

            FillFileCheckBox();
            #endregion

            Filter();
        }

        private void ScanNumber_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                Filter();
            }
        }

        private void ScoreThreshold_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                Filter();
            }
        }

        private void TextPept_KeyDown(object sender, KeyEventArgs e)
        {
            var isNumber = e.Key >= Key.D0 && e.Key <= Key.D9;
            var isLetter = e.Key >= Key.A && e.Key <= Key.Z;
            var isDash = e.Key == Key.OemMinus;
            if (isNumber == true ||
                isLetter == true ||
                isDash == true)
                e.Handled = false;
            else
                e.Handled = true;

            if (e.Key == System.Windows.Input.Key.Enter)
            {
                Filter();
            }
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetTextPeptWidth();
        }

        private void CheckShowDecoys_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded == false) return;

            if (((CheckBox)sender).IsChecked == true)
            {
                IsDecoy = true;
            }
            else
            {
                IsDecoy = false;
            }

            Filter();
        }
    }
}
