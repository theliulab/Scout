using Microsoft.Win32;
using Scout.Results.CSMs;
using Scout.Results.PPIs;
using Scout.Results.ResPairs;
using ScoutCore;
using ScoutPostProcessing;
using ScoutPostProcessing.CSMLogic;
using ScoutPostProcessing.PPILogic;
using ScoutPostProcessing.ResiduePairLogic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using ThermoFisher.CommonCore.Data.Business;

namespace Scout.TruthEvaluation
{
    /// <summary>
    /// Interaction logic for ControlTruthByGroup.xaml
    /// </summary>
    public partial class ControlTruthByGroup : UserControl
    {
        internal class ListElementGroup
        {
            public List<IFDRElement> Elements { get; set; }
            public string ShownString { get; set; }
        }



        public enum IdentificationLevel
        {
            CSM,
            ResPair,
            PPI,
            Looplink
        }

        public IdentificationLevel Level { get; private set; }
        public Dictionary<string, HashSet<string>> Truth { get; private set; }
        public ScoutParameters SearchParams { get; }
        public PostProcessingParameters PostParams { get; }
        public List<IFDRElement> Elements { get; set; }
        public List<IFDRElement> CorrectElements { get; private set; }
        public List<IFDRElement> IncorrectElements { get; private set; }

        public ControlTruthByGroup(
            IdentificationLevel level,
            List<IFDRElement> elements,
            Dictionary<string, HashSet<string>> truth,
            ScoutParameters searchParams,
            PostProcessingParameters postParams
            )
        {
            InitializeComponent();

            Level = level;
            Truth = truth;
            SearchParams = searchParams;
            PostParams = postParams;
            Elements = elements;

            AssembleGUI();
        }

        private void AssembleGUI()
        {
            TextLevel.Text = Level switch
            {
                IdentificationLevel.CSM => "CSMs",
                IdentificationLevel.ResPair => "ResPairs",
                IdentificationLevel.PPI => "PPIs",
                IdentificationLevel.Looplink => "Looplinks"
            };

            TextTotal.Text = $"Total: {Elements.Count}";

            if (Elements.Count == 0) return;

            List<IFDRElement> correct = new();
            List<IFDRElement> incorrect = new();
            (correct, incorrect) = TruthEvaluationHelper.GetCorrectAndIncorrectElements(Elements.Where(a => a.IsDecoy == false).ToList(), Truth);
            CorrectElements = correct;
            IncorrectElements = incorrect;

            GroupCorrect.Header = $"Correct: {CorrectElements.Count} ({(CorrectElements.Count / ((float)Elements.Where(a => a.IsDecoy == false).Count())):P1})";
            GroupIncorrect.Header = $"Incorrect: {IncorrectElements.Count} ({(IncorrectElements.Count / ((float)Elements.Where(a => a.IsDecoy == false).Count())):P1})";


            var correctInter = CorrectElements.Where(a => a.IsInterLink == true).ToList();
            var correctIntra = CorrectElements.Where(a => a.IsInterLink == false).ToList();
            var incorrectInter = IncorrectElements.Where(a => a.IsInterLink == true).ToList();
            var incorrectIntra = IncorrectElements.Where(a => a.IsInterLink == false).ToList();



            ListCorrect.ItemsSource = null;
            ListCorrect.Items.Clear();
            ListCorrect.ItemsSource = new ObservableCollection<ListElementGroup>()
            {
                new ListElementGroup()
                {
                    ShownString = $"\tInter: { correctInter.Count() } ( {correctInter.Count() / ((float)correctInter.Count() + incorrectInter.Count()):P3} )",
                    Elements = correctInter
                },
                new ListElementGroup()
                {
                    ShownString = $"\tIntra: { correctIntra.Count() } ( {correctIntra.Count() / ((float)correctIntra.Count() + incorrectIntra.Count()):P3} )",
                    Elements = correctIntra
                },
            };

            ListIncorrect.ItemsSource = null;
            ListIncorrect.Items.Clear();
            ListIncorrect.ItemsSource = new ObservableCollection<ListElementGroup>()
            {
                new ListElementGroup()
                {
                    ShownString = $"\tInter: { incorrectInter.Count() } ( {incorrectInter.Count() / ((float)correctInter.Count() + incorrectInter.Count()):P3} )",
                    Elements = incorrectInter
                },
                new ListElementGroup()
                {
                    ShownString = $"\tIntra: { incorrectIntra.Count() } ( {incorrectIntra.Count() / ((float)correctIntra.Count() + incorrectIntra.Count()):P3} )",
                    Elements = incorrectIntra
                },
            };

        }

        private void ListCorrect_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ListCorrect.SelectedItem == null) return;

            var elements = ((ListElementGroup)ListCorrect.SelectedItem).Elements;
            var shownString = ((ListElementGroup)ListCorrect.SelectedItem).ShownString;

            UserControl table = null;
            switch (Level)
            {
                case IdentificationLevel.CSM:
                    var csmTable = new CSMTable();
                    csmTable.Load(elements.Cast<ScoredCSM>().ToList(), SearchParams, PostParams);
                    table = csmTable;
                    break;
                case IdentificationLevel.ResPair:
                    var rpTable = new ResPairTable();
                    rpTable.Load(elements.Cast<ResPair>().ToList(), SearchParams, PostParams);
                    table = rpTable;
                    break;
                case IdentificationLevel.PPI:
                    var ppiTable = new PPITable();
                    ppiTable.Load(elements.Cast<PPI>().ToList(), SearchParams, PostParams);
                    table = ppiTable;
                    break;
                default:
                    break;
            }

            var w = new Window()
            {
                Title = shownString,
                Content = table
            };

            w.Show();
        }

        private void ListIncorrect_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ListIncorrect.SelectedItem == null) return;

            var elements = ((ListElementGroup)ListIncorrect.SelectedItem).Elements;
            var shownString = ((ListElementGroup)ListIncorrect.SelectedItem).ShownString;

            UserControl table = null;
            switch (Level)
            {
                case IdentificationLevel.CSM:
                    var csmTable = new CSMTable();
                    csmTable.Load(elements.Cast<ScoredCSM>().ToList(), SearchParams, PostParams);
                    table = csmTable;
                    break;
                case IdentificationLevel.ResPair:
                    var rpTable = new ResPairTable();
                    rpTable.Load(elements.Cast<ResPair>().ToList(), SearchParams, PostParams);
                    table = rpTable;
                    break;
                case IdentificationLevel.PPI:
                    var ppiTable = new PPITable();
                    ppiTable.Load(elements.Cast<PPI>().ToList(), SearchParams, PostParams);
                    table = ppiTable;
                    break;
                default:
                    break;
            }

            var w = new Window()
            {
                Title = shownString,
                Content = table
            };

            w.Show();
        }


        private void ButtonStandardize_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog()
            {
                Filter = "CSV File (.csv)|*.csv"
            };

            if (sfd.ShowDialog() == false) return;

            Standardizer.SaveWithTruthColumns(sfd.FileName, Truth, Elements, SearchParams, PostParams);
            //System.Diagnostics.Process.Start(sfd.FileName);
        }
    }
}
