using ScoutCore;
using Scout.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using Scout.Parameters;
using Microsoft.ML.Data;

namespace Scout.Parameters
{
    /// <summary>
    /// Interaction logic for WindowSearchParams.xaml
    /// </summary>
    public partial class WindowAdvancedSearchParams : Window
    {
        public ScoutParameters SearchParams { get; set; }
        public bool Accepted = false;
        public bool IsRestore { get; private set; }
        private WindowScoutParametersEdit ParentWindow;

        public WindowAdvancedSearchParams(
            ScoutParameters scoutParams,
            WindowScoutParametersEdit parentWindow)
        {
            SearchParams = scoutParams.Copy(scoutParams);
            ParentWindow = parentWindow;

            InitializeComponent();
            LoadParams();

        }

        private void LoadParams()
        {
            if (SearchParams == null)
            {
                return;
            }

            CheckSaveSpectra.IsChecked = SearchParams.SaveSpectraToResults;
            CheckAddContaminants.IsChecked = SearchParams.AddContaminants;
            CheckAddDecoys.IsChecked = SearchParams.AddDecoys;
            TextFastaBatchSize.Text = SearchParams.FastaBatchSize.ToString();
            TextFragBinTol.Text = SearchParams.BinSize.ToString();
            TextFragBinOffset.Text = SearchParams.Offset.ToString();
            TextMinBinSize.Text = SearchParams.MinBinMZ.ToString();
            TextMaxBinSize.Text = SearchParams.MaxBinMZ.ToString();
            TextIsotopicPossibilities.Text = SearchParams.IsotopicPossibilitiesPrecursor.ToString();
            CheckSilac.IsChecked = SearchParams.SilacSearch;
            CheckHybridSilac.IsChecked = SearchParams.SilacHybridMode;
            CheckTMT.IsChecked = SearchParams.IsobaricLabelling_Search;
            CheckSilac_Click(null, null);
            CheckHybridSilac_Click(null, null);
            CheckTMT_Click(null, null);
        }

        private bool SaveParams()
        {
            #region check if some field is empty
            if (String.IsNullOrEmpty(TextFastaBatchSize.Text))
            {
                System.Windows.Forms.MessageBox.Show("'Fasta batch size' field is empty. Please fix it!", "Scout :: Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                return false;
            }
            else if (String.IsNullOrEmpty(TextFragBinTol.Text))
            {
                System.Windows.Forms.MessageBox.Show("'Fragment bin tolerance' field is empty. Please fix it!", "Scout :: Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                return false;
            }
            else if (String.IsNullOrEmpty(TextFragBinOffset.Text))
            {
                System.Windows.Forms.MessageBox.Show("'Fragment bin offset' field is empty. Please fix it!", "Scout :: Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                return false;
            }
            else if (String.IsNullOrEmpty(TextMinBinSize.Text))
            {
                System.Windows.Forms.MessageBox.Show("'Minimum fragment bin m/z' field is empty. Please fix it!", "Scout :: Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                return false;
            }
            else if (String.IsNullOrEmpty(TextMaxBinSize.Text))
            {
                System.Windows.Forms.MessageBox.Show("'Maximum fragment bin m/z' field is empty. Please fix it!", "Scout :: Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                return false;
            }
            else if (String.IsNullOrEmpty(TextIsotopicPossibilities.Text))
            {
                System.Windows.Forms.MessageBox.Show("'No. Isotopic Possibilities' field is empty. Please fix it!", "Scout :: Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                return false;
            }
            #endregion

            try
            {
                SearchParams.SaveSpectraToResults = CheckSaveSpectra.IsChecked ?? false;
                SearchParams.AddContaminants = CheckAddContaminants.IsChecked ?? false;
                SearchParams.AddDecoys = CheckAddDecoys.IsChecked ?? false;
                SearchParams.FastaBatchSize = Convert.ToInt32(TextFastaBatchSize.Text);
                SearchParams.BinSize = Convert.ToDouble(TextFragBinTol.Text);
                SearchParams.Offset = Convert.ToDouble(TextFragBinOffset.Text);
                SearchParams.MinBinMZ = Convert.ToDouble(TextMinBinSize.Text);
                SearchParams.MaxBinMZ = Convert.ToDouble(TextMaxBinSize.Text);
                SearchParams.IsotopicPossibilitiesPrecursor = Convert.ToInt32(TextIsotopicPossibilities.Text);
                SearchParams.SilacSearch = CheckSilac.IsChecked ?? false;
                SearchParams.SilacHybridMode = CheckHybridSilac.IsChecked ?? false;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Could not save search parameters.\n" + ex.Message, "Scout (beta version) :: :: Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return false;
            }


            return true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (IsRestore) return;

            var r = System.Windows.Forms.MessageBox.Show("Accept changes?", "Scout (beta version) :: Information", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Information);
            if (r == System.Windows.Forms.DialogResult.Yes)
            {
                bool isCorrect = SaveParams();
                if (isCorrect)
                {
                    Accepted = true;
                }
                else
                {
                    e.Cancel = true;
                }
            }
            else
            {
                Accepted = false;
            }
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog()
            {
                Title = "Save Scout search parameters",
                Filter = "Scout search params|*.json",
                FileName = "Scout_search_params.json",
                AddExtension = true
            };

            if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            try
            {
                string json = SearchParams.ToSaveJson();
                File.WriteAllText(sfd.FileName, json);
                System.Windows.Forms.MessageBox.Show("Search parameters have been saved successfully!", "Scout (beta version) :: Information",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: Could not save search parameters.\n{0}\n{1}", ex.Message, ex.StackTrace);
                System.Windows.Forms.MessageBox.Show("Could not save search parameters!", "Scout (beta version) :: Error",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private void ButtonLoad_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog()
            {
                Title = "Load Scout post parameters",
                Filter = "Scout search params|*.json",
                AddExtension = true
            };

            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            try
            {
                ScoutParameters newParams = ScoutParameters.Load(ofd.FileName);
                SearchParams = newParams;
                LoadParams();

                System.Windows.Forms.MessageBox.Show("Search Parameters have been loaded successfully!", "Scout (beta version) :: Information",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: Could not load search parameters.\n{0}\n{1}", ex.Message, ex.StackTrace);
                System.Windows.Forms.MessageBox.Show("Could not load search parameters!", "Scout (beta version) :: Error",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private void ButtonSetAsDefault_Click(object sender, RoutedEventArgs e)
        {
            var r = System.Windows.Forms.MessageBox.Show("This will override the application's default parameters. Are you sure?", "Scout (beta version) :: Information", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning);
            if (r != System.Windows.Forms.DialogResult.Yes) { return; }

            Settings.Default.ScoutParamsJson = SearchParams.ToSaveJson();
            Settings.Default.Save();
        }

        private void ButtonRestoreDefault_Click(object sender, RoutedEventArgs e)
        {
            var r = System.Windows.Forms.MessageBox.Show("This will remove the current settings and restore the parameters to software default. Are you sure?", "Scout (beta version) :: Information", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning);
            if (r != System.Windows.Forms.DialogResult.Yes) { IsRestore = false; return; }

            Settings.Default.ScoutParamsJson = string.Empty;
            Settings.Default.PostParamsJson = string.Empty;
            Settings.Default.ReagentsJson = string.Empty;
            Settings.Default.EnzymesJson = string.Empty;
            Settings.Default.VariableModsJson = string.Empty;
            Settings.Default.Save();

            IsRestore = true;

            System.Windows.Forms.MessageBox.Show("Parameters have been restored sucessfully!", "Scout (beta version) :: Information", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            ParentWindow.IsRestore = true;
            ParentWindow.Close();
            this.Close();
        }

        private void Button_AddSilacGroup(object sender, RoutedEventArgs e)
        {
            var w = new WindowAddNewSilacGroup(SearchParams);
            w.ShowDialog();

            if (w.Accepted == true)
            {
                SearchParams.SilacGroups = w.MetabolicLabellingGroups.MetabolicLabellingGroupList;
            }
        }

        private void CheckSilac_Click(object sender, RoutedEventArgs e)
        {
            if (CheckSilac.IsChecked == true)
            {
                SearchParams.SilacSearch = true;
                ButtonSilacGroup.Visibility = Visibility.Visible;
                CheckHybridSilac.Visibility = Visibility.Visible;
                CheckHybridSilac_Click(null, null);

                if (CheckTMT.IsChecked == true)
                {
                    CheckTMT.IsChecked = false;
                    CheckTMT_Click(null, null);
                }
            }
            else
            {
                SearchParams.SilacSearch = false;
                ButtonSilacGroup.Visibility = Visibility.Collapsed;
                CheckHybridSilac.Visibility = Visibility.Collapsed;
                SearchParams.SilacHybridMode = false;
            }
        }

        private void PreviewTextDoubleInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextDoubleAllowed(e.Text);
        }
        private void PreviewTextIntInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextIntAllowed(e.Text);
        }

        private static readonly Regex _regex_double = new Regex("[^0-9.]+"); //regex that matches disallowed text
        private static readonly Regex _regex_int = new Regex("[^0-9]+"); //regex that matches disallowed text
        private static bool IsTextDoubleAllowed(string text)
        {
            return !_regex_double.IsMatch(text);
        }
        private static bool IsTextIntAllowed(string text)
        {
            return !_regex_int.IsMatch(text);
        }

        private void PastingIntHandler(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(String)))
            {
                String text = (String)e.DataObject.GetData(typeof(String));
                if (!IsTextIntAllowed(text)) e.CancelCommand();
            }
            else e.CancelCommand();
        }

        private void PastingDoubleHandler(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(String)))
            {
                String text = (String)e.DataObject.GetData(typeof(String));
                if (!IsTextDoubleAllowed(text)) e.CancelCommand();
            }
            else e.CancelCommand();
        }

        private void CheckHybridSilac_Click(object sender, RoutedEventArgs e)
        {
            if (CheckHybridSilac.IsChecked == true)
            {
                SearchParams.SilacHybridMode = true;
            }
            else
            {
                SearchParams.SilacHybridMode = false;
            }
        }

        private void Button_AddTMT(object sender, RoutedEventArgs e)
        {
            var w = new WindowAddNewIsobaricLabelling(SearchParams);
            w.ShowDialog();

            if (w.Accepted == true)
            {
                SearchParams.IsobaricLabelling_AllowedFreeResidues = w.SearchParams.IsobaricLabelling_AllowedFreeResidues;
                SearchParams.IsobaricLabelling_Mods = w.SearchParams.IsobaricLabelling_Mods;
                SearchParams.SelectedIsobaricLabelling_Mod = w.SearchParams.SelectedIsobaricLabelling_Mod;
            }

        }

        private void CheckTMT_Click(object sender, RoutedEventArgs e)
        {
            if (CheckTMT.IsChecked == true)
            {
                SearchParams.IsobaricLabelling_Search = true;
                ButtonTMT.Visibility = Visibility.Visible;

                if (CheckSilac.IsChecked == true)
                {
                    CheckSilac.IsChecked = false;
                    CheckSilac_Click(null, null);
                }
            }
            else
            {
                SearchParams.IsobaricLabelling_Search = false;
                ButtonTMT.Visibility = Visibility.Collapsed;
            }
        }
    }
}
