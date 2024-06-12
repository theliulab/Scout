using ScoutCore;
using ScoutPostProcessing;
using ScoutPostProcessing.PPILogic;
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

namespace Scout.Results.PPIs
{
    /// <summary>
    /// Interaction logic for ControlPPIResultsNew.xaml
    /// </summary>
    public partial class ControlPPIResultsNew : UserControl
    {
        private XLFilteredResults _post;
        private ScoutParameters _searchParams;
        private PostProcessingParameters _postParams;
        private bool IsOnlyInter { get; set; } = true;
        private bool IsDecoy { get; set; } = false;
        private bool IsGroupedByGene { get; set; } = false;
        public static List<PPI> GuiPPIs { get; private set; }
        private List<PPI> OriginalGuiPPIs { get; set; }

        public ControlPPIResultsNew()
        {
            InitializeComponent();
        }


        internal void Load(XLFilteredResults post)
        {
            _post = post;

            _searchParams = post.SearchParams;
            _postParams = post.PostParams;
            IsGroupedByGene = _postParams.GroupedByGene;

            GuiPPIs = _post.PackagePPIs.FilteredPPIs;
            OriginalGuiPPIs = new List<PPI>(GuiPPIs);

            Filter();

            CheckGroupByGene.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { CheckGroupByGene.IsChecked = _postParams.GroupedByGene; }));

            FDR_SeparatedOrCombined_Label.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {

                if (_post.PostParams.FDRMode == FDRModes.CombinedIntraInter)
                    FDR_SeparatedOrCombined_Label.Text = "FDR:";
                else
                    FDR_SeparatedOrCombined_Label.Text = "FDR inter- and intra-protein links:";
            }
            ));

            TextFDR_PPIs.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {

                if (_post.PostParams.FDRMode == FDRModes.CombinedIntraInter)
                    TextFDR_PPIs.Text = _post.PackagePPIs.MeasuredFDR_Combined.ToString("N4");
                else
                    TextFDR_PPIs.Text = _post.PackagePPIs.MeasuredFDR_Separate_Interlink.ToString("N4") + " and " + _post.PackagePPIs.MeasuredFDR_Separate_Intralink.ToString("N4");
            }
            ));
            TextIDsCount.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { TextIDsCount.Text = GuiPPIs.Count.ToString(); }));
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

        private string GetPPIIdentifier(ResPair rp)
        {
            List<string> l = null;
            if (rp.LongestBetaCSM != null)
            {
                l = new List<string>()
                {
                    String.Join(";",rp.LongestAlphaCSM.AlphaPSM.Peptide.Mappings.Select(a=> IsGroupedByGene ? a.Gene : a.Locus).Distinct()),
                    String.Join(";",rp.LongestBetaCSM.BetaPSM.Peptide.Mappings.Select(a=> IsGroupedByGene ? a.Gene : a.Locus).Distinct())
                };
            }
            else
            {
                l = new List<string>()
                {
                    String.Join(";",rp.LongestAlphaCSM.AlphaPSM.Peptide.Mappings.Select(a=> IsGroupedByGene ? a.Gene : a.Locus).Distinct()),
                    String.Join(";",rp.LongestAlphaCSM.AlphaPSM.Peptide.Mappings.Select(a=> IsGroupedByGene ? a.Gene : a.Locus).Distinct()),
                };
            }

            l = l.OrderBy(a => a).ToList();
            return $"{l[0]}+{l[1]}";
        }

        private void GroupPPIsByGeneOrPPI()
        {
            int threadsToUse = Environment.ProcessorCount - 2;
            Parallel.ForEach(
                    GuiPPIs,
                    new ParallelOptions()
                    {
                        MaxDegreeOfParallelism = threadsToUse
                    },
                    (ppi) =>
                    {
                        ppi.ResPairs.Sort((a, b) => b.ClassificationScore.CompareTo(a.ClassificationScore));
                        ppi.PPI_ID = GetPPIIdentifier(ppi.ResPairs[0]);
                    }
                );
        }

        private void Filter()
        {
            #region filters
            GuiPPIs = new List<PPI>(OriginalGuiPPIs);
            GroupPPIsByGeneOrPPI();

            if (IsOnlyInter == true)
                GuiPPIs = GuiPPIs.Where(a => a.IsInterLink == true).ToList();

            if (!IsDecoy)
                GuiPPIs = GuiPPIs.Where(a => a.IsDecoy == false).ToList();

            double score = -1;
            ScoreThreshold.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {

                if (ScoreThreshold.Value != null &&
                 !String.IsNullOrEmpty(ScoreThreshold.Value.ToString()) &&
                 !ScoreThreshold.Text.ToString().Equals("0"))
                {
                    score = (double)ScoreThreshold.Value;
                    GuiPPIs = GuiPPIs.Where(a => a.ClassificationScore >= score).ToList();
                }

            }));


            string sequence = "";
            TextProt.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {

                if (TextProt.Text != null &&
                !String.IsNullOrEmpty(TextProt.Text.ToString()) &&
                TextProt.Text.ToString().Length > 3)
                {
                    sequence = TextProt.Text.ToLower();
                    List<PPI> firstProtein = null;
                    List<PPI> secondProtein = null;
                    List<PPI> firstGene = null;
                    List<PPI> secondGene = null;

                    string first = "";
                    string second = "";
                    if (sequence.Contains("-"))
                    {
                        first = sequence.Split('-')[0];
                        second = sequence.Split('-')[1];

                        firstProtein = GuiPPIs.Where(a => a.ProteinOneString.ToLower().Contains(first) &&
                                                           a.ProteinTwoString.ToLower().Contains(second)).ToList();
                    }
                    else
                    {
                        firstProtein = GuiPPIs.Where(a => a.ProteinOneString.ToLower().Contains(sequence)).ToList();
                        secondProtein = GuiPPIs.Where(a => a.ProteinTwoString.ToLower().Contains(sequence)).ToList();
                        firstGene = GuiPPIs.Where(a => a.GeneOneString.ToLower().Contains(sequence)).ToList();
                        secondGene = GuiPPIs.Where(a => a.GeneTwoString.ToLower().Contains(sequence)).ToList();
                    }
                    GuiPPIs.Clear();
                    if (firstProtein != null && firstProtein.Count > 0)
                        GuiPPIs.AddRange(firstProtein);
                    if (secondProtein != null && secondProtein.Count > 0)
                        GuiPPIs.AddRange(secondProtein);
                    if (firstGene != null && firstGene.Count > 0)
                        GuiPPIs.AddRange(firstGene);
                    if (secondGene != null && secondGene.Count > 0)
                        GuiPPIs.AddRange(secondGene);

                    GuiPPIs = GuiPPIs.Distinct().ToList();
                }

            }));


            #endregion
            MyPPITable.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {
                GuiPPIs.Sort((a, b) => b.ClassificationScore.CompareTo(a.ClassificationScore));
                MyPPITable.Load(GuiPPIs, _searchParams, _postParams);
            }));
            TextIDsCount.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { TextIDsCount.Text = GuiPPIs.Count.ToString(); }));

        }

        private void ButtonFilter_Click(object sender, RoutedEventArgs e)
        {
            Filter();
        }

        private void ButtonResetFilter_Click(object sender, RoutedEventArgs e)
        {
            #region reset filters
            ScoreThreshold.Value = 0;
            TextProt.Text = "";
            CheckInterOnly.IsChecked = true;
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

        private void TextProt_KeyDown(object sender, KeyEventArgs e)
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

        private void CheckGroupByGene_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded == false) return;

            if (((CheckBox)sender).IsChecked == true)
            {
                IsGroupedByGene = true;
            }
            else
            {
                IsGroupedByGene = false;
            }

            Filter();
        }
    }
}
