using ScoutPostProcessing;
using SpectrumWizard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ThermoFisher.CommonCore.Data;

namespace Scout.Results.Statistics
{
    /// <summary>
    /// Interaction logic for StatisticsUC.xaml
    /// </summary>
    public partial class StatisticsUC : UserControl
    {
        private XLFilteredResults _results { get; set; }
        private List<ScatterDataPoint> list_points { get; set; }
        public StatisticsUC()
        {
            InitializeComponent();
        }

        internal class CandidateIon
        {
            public double MZ { get; set; }
            public double ppm { get; set; }
            public double score { get; set; }
            public int scanNumber { get; set; }

            public CandidateIon(double mz, double ppm, double score, int scanNumber)
            {
                MZ = mz;
                this.ppm = ppm;
                this.score = score;
                this.scanNumber = scanNumber;
            }
        }
        public void Load(XLFilteredResults results)
        {
            _results = results;
            ((ColumnSeries)chartChargePlot.Series[0]).ItemsSource = GetCharges();
            ((ColumnSeries)chartReactionSitesPlot.Series[0]).ItemsSource = GetReactionSites();
            //GetPPMs();

        }

        private KeyValuePair<string, int>[] GetCharges()
        {
            List<KeyValuePair<string, int>> list = new();
            if (_results == null || _results.PackageCSMs == null)
                return list.ToArray();

            var grouped = (from csm in _results.PackageCSMs.FilteredCSMs
                           group csm by csm.PrecursorCharge into g
                           select new { CSM = g.Key, Amount = g.ToList().Count });

            foreach (var value in grouped)
                list.Add(new KeyValuePair<string, int>(value.CSM + "+", value.Amount));
            return list.ToArray();
        }

        private KeyValuePair<string, int>[] GetReactionSites()
        {
            Dictionary<string, int> list = new();
            if (_results == null || _results.PackageCSMs == null)
                return list.ToArray();

            var grouped = (from csm in _results.PackageCSMs.FilteredCSMs
                           group csm by csm.AlphaPSM.Peptide.AsCleanString[csm.AlphaPSM.ReagentPosition1] + "_" + csm.BetaPSM.Peptide.AsCleanString[csm.BetaPSM.ReagentPosition1] into g
                           select new { RS = g.Key, Amount = g.ToList().Count });

            foreach (var value in grouped)
                list.Add(value.RS, value.Amount);

            string react_sites = _results.SearchParams.CXLReagent.Targets;

            Dictionary<string, int> final_list = new();
            List<string> keys = list.Keys.OrderBy(a => a).ToList();
            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                string[] tmp = key.Split('_');

                char rs1 = tmp[0].ToCharArray()[0];
                char rs2 = tmp[1].ToCharArray()[0];
                if (tmp[0] == tmp[1])
                {
                    if (!react_sites.Contains(rs1) && !react_sites.Contains(rs2))
                        final_list.TryAdd("[N-term - N-term]", list[key]);
                    else
                        final_list.TryAdd("[" + key.Replace("_", " - ") + "]", list[key]);
                    continue;
                }

                string new_key = tmp[1] + "_" + tmp[0];
                int index = keys.IndexOf(new_key);
                if (index != -1 && i < index)
                {
                    int new_value = -1;
                    list.TryGetValue(new_key, out new_value);

                    if (!react_sites.Contains(rs1))
                        final_list.TryAdd("[N-term - " + rs2 + "]", list[key] + new_value);
                    else if (!react_sites.Contains(rs2))
                        final_list.TryAdd("[N-term - " + rs1 + "]", list[key] + new_value);
                    else
                        final_list.TryAdd("[" + key.Replace("_", " - ") + "]", list[key] + new_value);

                }
            }

            List<KeyValuePair<string, int>> _list = final_list.ToList();
            _list.Sort(delegate (KeyValuePair<string, int> pair1, KeyValuePair<string, int> pair2)
                {
                    return pair2.Value.CompareTo(pair1.Value);
                });
            return _list.ToArray();
        }

        [STAThread]
        async void GetPPMs()
        {
            list_points = new();

            if (_results == null || _results.PackageResPairs == null)
                return;

            var ions = (from rp in _results.PackageResPairs.FilteredResPairs.AsParallel()
                        select new CandidateIon(rp.CSMs[0].PrecursorMZ, Math.Max(rp.CSMs[0].AlphaPPM, rp.CSMs[0].BetaPPM), rp.CSMs[0].ClassificationScore, rp.CSMs[0].ScanNumber)).ToList();

            showLoad();
            await runAsync(ions);
        }

        [STAThread]
        Task runAsync(List<CandidateIon> ions)
        {
            var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            
            return Task.Factory.StartNew(
            () =>
            {
                try
                {
                    foreach (var ion in ions)
                    {
                        ScatterDataPoint sdp = new ScatterDataPoint();
                        sdp.DependentValue = ion.ppm;
                        sdp.IndependentValue = ion.MZ;
                        sdp.ToolTip = "ppm: " + ion.ppm.ToString("0.0000") + "\nm/z: " + ion.MZ.ToString("0.0000") + "\nScan: " + ion.scanNumber + "\nCSM Score: " + ion.score.ToString("0.0000");
                        lock (list_points)
                            list_points.Add(sdp);
                    }
                }
                catch (Exception e)
                {
                    throw;
                }
            }
            ).ContinueWith(r => HandleFinishedThread(), scheduler);
        }

        private void HandleFinishedThread()
        {
            //((ScatterSeries)chartPPMPlot.Series[0]).ItemsSource = list_points;
            //hideLoad();
        }

        private void hideLoad()
        {
            //WaitImage.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { WaitImage.Visibility = Visibility.Collapsed; }));
            //LoadingLabel.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { LoadingLabel.Visibility = Visibility.Collapsed; }));
            //LoadingImage.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { LoadingImage.Visibility = Visibility.Collapsed; }));
        }

        private void showLoad()
        {
            //WaitImage.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { WaitImage.Visibility = Visibility.Visible; }));
            //LoadingLabel.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { LoadingLabel.Visibility = Visibility.Visible; }));
            //LoadingImage.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { LoadingImage.Visibility = Visibility.Visible; }));
        }

        private void DataPoint_MouseEnter(object sender, MouseEventArgs e)
        {
            ((ScatterDataPoint)sender).ToolTip = ((ScatterDataPoint)((ScatterDataPoint)sender).DataContext).ToolTip;
        }
    }
}
