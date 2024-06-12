using ScoutCore;
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

namespace Scout.Parameters
{
    /// <summary>
    /// Interaction logic for WindowIsobaricLabellingReagents.xaml
    /// </summary>
    public partial class WindowIsobaricLabellingReagents : Window
    {
        public ScoutParameters SearchParams { get; set; }

        public WindowIsobaricLabellingReagents(ScoutParameters _searchParams)
        {
            InitializeComponent();
            SearchParams = _searchParams;
            isobaricLabellingUC.Load(_searchParams);
    }

        private void Window_Closed(object sender, EventArgs e)
        {
            SearchParams.IsobaricLabelling_Mods = isobaricLabellingUC.IsobaricLabelling_Mods;
        }
    }
}
