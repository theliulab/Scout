using ScoutCore;
using Scout.Results.CSMs;
using ScoutPostProcessing;
using ScoutPostProcessing.CSMLogic;
using ScoutPostProcessing.ResiduePairLogic;
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

namespace Scout.Results.ResPairs
{
    /// <summary>
    /// Interaction logic for ControlResPairResultsNew.xaml
    /// </summary>
    public partial class ControlResPairResultsNew : UserControl
    {
        public static List<ResPair> GuiResPairs { get; private set; }
        private List<ResPair> OriginalGuiResPairs { get; set; }
        private bool IsOnlyInter { get; set; } = false;
        private bool IsDecoy { get; set; } = false;

        private XLFilteredResults _post;
        private ScoutParameters _searchParams;
        private PostProcessingParameters _postParams;

        public ControlResPairResultsNew()
        {
            InitializeComponent();
        }


        internal void Load(XLFilteredResults post)
        {
            _post = post;

            _searchParams = post.SearchParams;
            _postParams = post.PostParams;

            GuiResPairs = _post.PackageResPairs.FilteredResPairs;
            OriginalGuiResPairs = new List<ResPair>(GuiResPairs);

            Filter();

            FDR_SeparatedOrCombined_Label.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {

                if (_post.PostParams.FDRMode == FDRModes.CombinedIntraInter)
                    FDR_SeparatedOrCombined_Label.Text = "FDR:";
                else
                    FDR_SeparatedOrCombined_Label.Text = "FDR inter- and intra-residue pair links:";
            }
            ));

            TextFDR_ResPairs.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {

                if (_post.PostParams.FDRMode == FDRModes.CombinedIntraInter)
                    TextFDR_ResPairs.Text = _post.PackageResPairs.MeasuredFDR_Combined.ToString("N4");
                else
                    TextFDR_ResPairs.Text = _post.PackageResPairs.MeasuredFDR_Separate_Interlink.ToString("N4") + " and " + _post.PackageResPairs.MeasuredFDR_Separate_Intralink.ToString("N4");
            }
            ));

            TextIDsCount.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { TextIDsCount.Text = GuiResPairs.Count.ToString(); }));
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
            GuiResPairs = new List<ResPair>(OriginalGuiResPairs);

            if (IsOnlyInter == true)
                GuiResPairs = GuiResPairs.Where(a => a.IsInterLink == true).ToList();

            if (!IsDecoy)
                GuiResPairs = GuiResPairs.Where(a => a.IsDecoy == false).ToList();

            double score = -1;
            ScoreThreshold.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {

                if (ScoreThreshold.Value != null &&
                !String.IsNullOrEmpty(ScoreThreshold.Value.ToString()) &&
                !ScoreThreshold.Text.ToString().Equals("0"))
                {
                    score = (double)ScoreThreshold.Value;
                    GuiResPairs = GuiResPairs.Where(a => a.ClassificationScore >= score).ToList();
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
                    List<ResPair> alphaPeptides = null;
                    List<ResPair> betaPeptides = null;
                    List<ResPair> firstProtein = null;
                    List<ResPair> secondProtein = null;
                    List<ResPair> firstGene = null;
                    List<ResPair> secondGene = null;

                    string alpha = "";
                    string beta = "";
                    if (sequence.Contains("-"))
                    {
                        alpha = sequence.Split('-')[0];
                        beta = sequence.Split('-')[1];

                        alphaPeptides = GuiResPairs.Where(a => a.LongestAlphaCSM.AlphaPSM.Peptide.AsCleanString.ToLower().Contains(alpha) &&
                                                           a.LongestBetaCSM.BetaPSM.Peptide.AsCleanString.ToLower().Contains(beta)).ToList();
                    }
                    else
                    {
                        alphaPeptides = GuiResPairs.Where(a => a.LongestAlphaCSM.AlphaPSM.Peptide.AsCleanString.ToLower().Contains(sequence)).ToList();
                        betaPeptides = GuiResPairs.Where(a => a.LongestBetaCSM.BetaPSM.Peptide.AsCleanString.ToLower().Contains(sequence)).ToList();
                        firstProtein = GuiResPairs.Where(a => a.AlphaProteins.ToLower().Contains(sequence)).ToList();
                        secondProtein = GuiResPairs.Where(a => a.BetaProteins.ToLower().Contains(sequence)).ToList();
                        firstGene = GuiResPairs.Where(a => a.AlphaGenes.ToLower().Contains(sequence)).ToList();
                        secondGene = GuiResPairs.Where(a => a.BetaGenes.ToLower().Contains(sequence)).ToList();
                    }
                    GuiResPairs.Clear();
                    if (alphaPeptides != null && alphaPeptides.Count > 0)
                        GuiResPairs.AddRange(alphaPeptides);
                    if (betaPeptides != null && betaPeptides.Count > 0)
                        GuiResPairs.AddRange(betaPeptides);
                    if (firstProtein != null && firstProtein.Count > 0)
                        GuiResPairs.AddRange(firstProtein);
                    if (secondProtein != null && secondProtein.Count > 0)
                        GuiResPairs.AddRange(secondProtein);
                    if (firstGene != null && firstGene.Count > 0)
                        GuiResPairs.AddRange(firstGene);
                    if (secondGene != null && secondGene.Count > 0)
                        GuiResPairs.AddRange(secondGene);

                    GuiResPairs = GuiResPairs.Distinct().ToList();
                }

            }));

            #endregion

            MyResPairTable.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { MyResPairTable.Load(GuiResPairs, _searchParams, _postParams); }));
            TextIDsCount.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { TextIDsCount.Text = GuiResPairs.Count.ToString(); }));

        }

        private void ButtonFilter_Click(object sender, RoutedEventArgs e)
        {
            Filter();
        }

        private void ButtonResetFilter_Click(object sender, RoutedEventArgs e)
        {
            #region reset filters
            ScoreThreshold.Value = 0;
            TextPept.Text = "";
            CheckInterOnly.IsChecked = false;
            CheckShowDecoys.IsChecked = false;

            #endregion

            Filter();
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

        private void CheckShowDecoys_Checked(object sender, RoutedEventArgs e)
        {
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
