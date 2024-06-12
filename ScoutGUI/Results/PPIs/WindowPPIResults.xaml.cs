using ScoutCore;
using ScoutPostProcessing;
using ScoutPostProcessing.PPILogic;
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
using System.Windows.Shapes;

namespace Scout.Results.PPIs
{
    /// <summary>
    /// Interaction logic for WindowPPIResults.xaml
    /// </summary>
    public partial class WindowPPIResults : Window
    {
        public WindowPPIResults(List<PPI> ppi,
                          ScoutParameters scoutParams,
                          PostProcessingParameters postParams)
        {
            InitializeComponent();
            MyPPITable.Load(ppi, scoutParams, postParams);
        }
    }
}
