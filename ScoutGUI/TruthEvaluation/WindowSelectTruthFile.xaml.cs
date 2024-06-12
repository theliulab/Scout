using Microsoft.Win32;
using ScoutPostProcessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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

namespace Scout.TruthEvaluation
{
    /// <summary>
    /// Interaction logic for WindowSelectTruthFile.xaml
    /// </summary>
    public partial class WindowSelectTruthFile : Window
    {
        private XLFilteredResults results;

        public WindowSelectTruthFile(XLFilteredResults results)
        {
            InitializeComponent();

            this.results = results;
        }

        private void ButtonBrowseTruthFile_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog()
            {
                Filter = $"Text file (.txt)|*.txt"
            };

            if (ofd.ShowDialog() == false)
                return;

            PopulateTextTruth(ofd.FileName);

            PopulateGoodGUITruth(ofd.FileName);
        }

        private void PopulateGoodGUITruth(string fileName)
        {
            StackBetterGUI.Children.Clear();

            var truth = TruthEvaluationHelper.GetTruth(fileName);

            var looplinkControl = new ControlTruthByGroup(ControlTruthByGroup.IdentificationLevel.Looplink, results.PackageCSMsLoopLinks.FilteredElements, truth, results.SearchParams, results.PostParams);
            var csmControl = new ControlTruthByGroup(ControlTruthByGroup.IdentificationLevel.CSM, results.PackageCSMs.FilteredElements, truth, results.SearchParams, results.PostParams);
            var rpControl = new ControlTruthByGroup(ControlTruthByGroup.IdentificationLevel.ResPair, results.PackageResPairs.FilteredElements, truth, results.SearchParams, results.PostParams);
            var ppiControl = new ControlTruthByGroup(ControlTruthByGroup.IdentificationLevel.PPI, results.PackagePPIs.FilteredElements, truth, results.SearchParams, results.PostParams);

            StackBetterGUI.Children.Add(csmControl);
            StackBetterGUI.Children.Add(rpControl);
            StackBetterGUI.Children.Add(ppiControl);
            StackBetterGUI.Children.Add(looplinkControl);
        }

        private void PopulateTextTruth(string fileName)
        {
            var allCSMs = results.PackageCSMs.FilteredCSMs.Where(a => a.IsDecoy == false && a.IsLoopLink == false);
            var allRPs = results.PackageResPairs.FilteredResPairs.Where(a => a.IsDecoy == false);
            var allPPIs = results.PackagePPIs.FilteredPPIs.Where(a => a.IsDecoy == false);

            var (csm_errors_inter, csm_errors_intra, rp_errors_inter, rp_errors_intra, ppi_errors_inter, ppi_errors_intra) = TruthEvaluationHelper.CheckTruth(results, fileName);

            TextEvaluation.Text = "";

            TextEvaluation.Text += $"CSM:" +
                $"\n\tTotal: {allCSMs.Count()}" +
                $"\n\t\tIntra: {allCSMs.Where(a => a.IsInterLink == false).Count()}" +
                $"\n\t\tInter: {allCSMs.Where(a => a.IsInterLink == true).Count()}" +
                $"\n\tErrors:" +
                $"\n\t\tIntra: {csm_errors_intra} ({csm_errors_intra / (float)allCSMs.Where(a => a.IsInterLink == false).Count()})" +
                $"\n\t\tInter: {csm_errors_inter} ({csm_errors_inter / (float)allCSMs.Where(a => a.IsInterLink == true).Count()})";

            TextEvaluation.Text += $"\n\nResPairs:" +
                 $"\n\tTotal: {allRPs.Count()}" +
                 $"\n\t\tIntra: {allRPs.Where(a => a.IsInterLink == false).Count()}" +
                 $"\n\t\tInter: {allRPs.Where(a => a.IsInterLink == true).Count()}" +
                 $"\n\tErrors:" +
                 $"\n\t\tIntra: {rp_errors_intra} ({rp_errors_intra / (float)allRPs.Where(a => a.IsInterLink == false).Count()})" +
                 $"\n\t\tInter: {rp_errors_inter} ({rp_errors_inter / (float)allRPs.Where(a => a.IsInterLink == true).Count()})";

            TextEvaluation.Text += $"\n\nPPIs:" +
                 $"\n\tTotal: {allPPIs.Count()}" +
                 $"\n\t\tIntra: {allPPIs.Where(a => a.IsInterLink == false).Count()}" +
                 $"\n\t\tInter: {allPPIs.Where(a => a.IsInterLink == true).Count()}" +
                 $"\n\tErrors:" +
                 $"\n\t\tIntra: {ppi_errors_intra} ({ppi_errors_intra / (float)allPPIs.Where(a => a.IsInterLink == false).Count()})" +
                 $"\n\t\tInter: {ppi_errors_inter} ({ppi_errors_inter / (float)allPPIs.Where(a => a.IsInterLink == true).Count()})";
        }

        
    }
}
