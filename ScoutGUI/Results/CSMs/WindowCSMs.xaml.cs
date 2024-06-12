using ScoutCore;
using ScoutPostProcessing;
using ScoutPostProcessing.CSMLogic;
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

namespace Scout.Results.CSMs
{
    /// <summary>
    /// Interaction logic for WindowCSMs.xaml
    /// </summary>
    public partial class WindowCSMs : Window
    {
        public WindowCSMs(List<ScoredCSM> csms,
                          ScoutParameters scoutParams,
                          PostProcessingParameters postParams)
        {
            InitializeComponent();
            MyCSMsTable.Load(csms, scoutParams, postParams);
        }
    }
}
