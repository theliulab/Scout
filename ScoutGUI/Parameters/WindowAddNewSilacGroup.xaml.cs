using Digestor.Modification;
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
    /// Interaction logic for WindowAddNewSilacGroup.xaml
    /// </summary>
    public partial class WindowAddNewSilacGroup : Window
    {
        public ScoutParameters SearchParams { get; set; }
        public MetabolicLabellingGroups MetabolicLabellingGroups { get; private set; }

        public bool Accepted = false;
        public WindowAddNewSilacGroup(ScoutParameters scoutParams)
        {
            SearchParams = scoutParams.Copy(scoutParams);
            InitializeComponent();

            MetabolicLabellingGroups = new MetabolicLabellingGroups(SearchParams);
            PropGrid.SelectedObject = MetabolicLabellingGroups;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var r = System.Windows.Forms.MessageBox.Show("Accept changes?", "Scout (beta version) :: Information", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Information);
            if (r == System.Windows.Forms.DialogResult.Yes)
            {
                Accepted = true;
            }
            else
            {
                Accepted = false;
            }
        }
    }

    public class MetabolicLabellingGroups
    {
        private ScoutParameters _searchParams { get; set; }
        public List<SilacGroup> MetabolicLabellingGroupList { get; set; }
        public MetabolicLabellingGroups(ScoutParameters searchParams)
        {
            _searchParams = searchParams;
            MetabolicLabellingGroupList = searchParams.SilacGroups;
        }

    }
}
