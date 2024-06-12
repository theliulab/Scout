using ScoutCore;
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
using System.Windows.Shapes;

namespace Scout.Results.ResPairs
{
    /// <summary>
    /// Interaction logic for WindowResPairs.xaml
    /// </summary>
    public partial class WindowResPairs : Window
    {
        public WindowResPairs(List<ResPair> respairs,
                          ScoutParameters scoutParams,
                          PostProcessingParameters postParams)
        {
            InitializeComponent();
            MyResPairTable.Load(respairs, scoutParams, postParams);
        }
    }
}
