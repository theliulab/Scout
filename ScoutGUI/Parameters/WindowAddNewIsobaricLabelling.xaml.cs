using Digestor;
using Digestor.Modification;
using ScoutCore;
using SpectrumWizard;
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
    /// Interaction logic for WindowAddNewIsobaricLabelling.xaml
    /// </summary>
    public partial class WindowAddNewIsobaricLabelling : Window
    {
        public ScoutParameters SearchParams { get; set; }

        public bool Accepted = false;
        public WindowAddNewIsobaricLabelling(ScoutParameters scoutParams)
        {
            InitializeComponent();

            SearchParams = scoutParams.Copy(scoutParams);
            Load();
        }

        private void Load()
        {
            ComboReagent.ItemsSource = null;
            ComboReagent.ItemsSource = SearchParams.IsobaricLabelling_Mods.Select(a => a.Name);

            int reagent_index = -1;
            if (SearchParams.SelectedIsobaricLabelling_Mod != null)
                reagent_index = SearchParams.IsobaricLabelling_Mods.FindIndex(a => a.Name == SearchParams.SelectedIsobaricLabelling_Mod.Name);
            ComboReagent.SelectedIndex = reagent_index;

            UpDownFreeResidueTol.Value = SearchParams.IsobaricLabelling_AllowedFreeResidues;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var r = System.Windows.Forms.MessageBox.Show("Accept changes?", "Scout (beta version) :: Information", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Information);
            if (r == System.Windows.Forms.DialogResult.Yes)
            {
                Accepted = true;
                SearchParams.IsobaricLabelling_AllowedFreeResidues = (int)UpDownFreeResidueTol.Value;
            }
            else
            {
                Accepted = false;
            }
        }

        private void ButtonReagent_Click(object sender, RoutedEventArgs e)
        {
            var w = new WindowIsobaricLabellingReagents(SearchParams);
            w.ShowDialog();

            if (SearchParams.SelectedIsobaricLabelling_Mod != null &&
                w.SearchParams.IsobaricLabelling_Mods.Where(a => a.Name.Equals(SearchParams.SelectedIsobaricLabelling_Mod.Name)).ToList().Count == 0)
            {
                ComboReagent.SelectedItem = null;
                SearchParams.SelectedIsobaricLabelling_Mod = null;
            }

            SearchParams.IsobaricLabelling_Mods = w.SearchParams.IsobaricLabelling_Mods;
            Load();
        }

        private void ButtonConfirm_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ComboReagent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int reagent_index = ComboReagent.SelectedIndex;
            if (reagent_index > -1)
                SearchParams.SelectedIsobaricLabelling_Mod = SearchParams.IsobaricLabelling_Mods[reagent_index];
        }
    }

    public class IsobaricLabelling
    {
        private ScoutParameters _searchParams { get; set; }
        public List<AminoacidMod> Mods { get; set; }
        public AminoacidMod Selected_Mod { get; set; }
        public int IsobaricLabelling_AllowedFreeResidues { get; set; } = 1;
        public IsobaricLabelling(ScoutParameters searchParams)
        {
            _searchParams = searchParams;
            Mods = searchParams.IsobaricLabelling_Mods;
            IsobaricLabelling_AllowedFreeResidues = searchParams.IsobaricLabelling_AllowedFreeResidues;
            Selected_Mod = searchParams.SelectedIsobaricLabelling_Mod;
        }
    }
}
