using ScoutCore;
using Scout.Results.CSMs;
using Scout.Results.ResPairs;
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
    /// Interaction logic for PPITable.xaml
    /// </summary>
    public partial class PPITable : UserControl
    {
        private ScoutParameters _searchParams;
        private PostProcessingParameters _postParams;

        public PPITable()
        {
            InitializeComponent();
        }

        internal void Load(
            List<PPI> ppis,
            ScoutParameters scoutParams,
            PostProcessingParameters postParams)
        {
            DataGridPPIs.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {
                _searchParams = scoutParams;
                _postParams = postParams;
                DataGridPPIs.ItemsSource = null;
                ppis.Sort((a, b) => b.ClassificationScore.CompareTo(a.ClassificationScore));
                DataGridPPIs.ItemsSource = ppis;
            }));
        }

        private void DataGridPPIs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = ((DataGrid)e.Source).SelectedItem;
            if (item == null) return;

            var ppi = (PPI)item;

            var w = new WindowResPairs(ppi.ResPairs, _searchParams, _postParams);
            w.ShowDialog();
        }

        private void DataGridPPIs_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
    }
}
