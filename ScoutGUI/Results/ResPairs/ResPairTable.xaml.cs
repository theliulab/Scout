using ScoutCore;
using Scout.Results.CSMs;
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

namespace Scout.Results.ResPairs
{
    /// <summary>
    /// Interaction logic for ResPairTable.xaml
    /// </summary>
    public partial class ResPairTable : UserControl
    {
        public List<ResPair> ResPairs { get; private set; }

        private ScoutParameters _searchParams;
        private PostProcessingParameters _postParams;


        public ResPairTable()
        {
            InitializeComponent();
        }



        internal void Load(
            List<ResPair> resPairs,
            ScoutParameters searchParams,
            PostProcessingParameters postParams)
        {
            ResPairs = resPairs;
            ResPairs.Sort((a, b) => b.ClassificationScore.CompareTo(a.ClassificationScore));
            _searchParams = searchParams;
            _postParams = postParams;

            LoadDataGrid();
        }

        private void LoadDataGrid()
        {
            DataGridResPairs.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { DataGridResPairs.ItemsSource = null; DataGridResPairs.ItemsSource = ResPairs; }));
        }

        private void DataGridResPairs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = ((DataGrid)e.Source).SelectedItem;
            if (item == null) return;

            var rps = (ResPair)item;

            var w = new WindowCSMs(rps.CSMs, _searchParams, _postParams);
            w.ShowDialog();
        }

        private void DataGridResPairs_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
    }
}
