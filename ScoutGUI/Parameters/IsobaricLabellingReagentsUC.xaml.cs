using Scout.Properties;
using ScoutCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Scout.Parameters
{
    /// <summary>
    /// Interaction logic for IsobaricLabellingReagentsUC.xaml
    /// </summary>
    public partial class IsobaricLabellingReagentsUC : UserControl
    {
        public ScoutParameters SearchParams { get; set; }
        public List<SpectrumWizard.AminoacidMod> IsobaricLabelling_Mods { get; private set; }

        public IsobaricLabellingReagentsUC()
        {
            InitializeComponent();
        }
        public void Load(ScoutParameters _searchParams)
        {
            SearchParams = _searchParams;

            try
            {
                bool saveNew = false;
                if (!String.IsNullOrEmpty(Settings.Default.LabellingReagents))
                {
                    IsobaricLabelling_Mods = ScoutCore.FileManagement.Serializer.FromJson<List<SpectrumWizard.AminoacidMod>>(
                    Settings.Default.LabellingReagents, true);

                    if (SearchParams.IsobaricLabelling_Mods.Count > IsobaricLabelling_Mods.Count)
                        saveNew = true;
                    else if (SearchParams.IsobaricLabelling_Mods.Count < IsobaricLabelling_Mods.Count)
                        SearchParams.IsobaricLabelling_Mods = IsobaricLabelling_Mods;
                }
                else
                    saveNew = true;

                if (saveNew)
                {
                    IsobaricLabelling_Mods = SearchParams.IsobaricLabelling_Mods;
                    Settings.Default.LabellingReagents = ScoutCore.FileManagement.Serializer.ToJSON(
                            SearchParams.IsobaricLabelling_Mods, false, true);
                    Settings.Default.Save();
                }
            }
            catch (Exception)
            {

                throw;
            }

            CleanFields();
            UpdateDataGrid();
        }

        private void ButtonConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (CheckFields()) return;

            if (AddOrUpdateLabellingReagent())
            {
                CleanFields();
                UpdateDataGrid();
            }
        }

        private bool CheckFields()
        {
            if (String.IsNullOrEmpty(TextName.Text))
            {
                System.Windows.MessageBox.Show(
                            "'Reagent Name' field is empty!",
                            "Scout (beta version) :: Warning",
                            (System.Windows.MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                            (System.Windows.MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Warning);

                TextName.Focus();
                return true;
            }

            if (String.IsNullOrEmpty(TextTargetResidues.Text))
            {
                System.Windows.MessageBox.Show(
                            "'Target Residues' field is empty!",
                            "Scout (beta version) :: Warning",
                            (System.Windows.MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                            (System.Windows.MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Warning);

                TextTargetResidues.Focus();
                return true;
            }

            if (UpDownFreeResidueTol.Value == 0)
            {
                System.Windows.MessageBox.Show(
                            "'Mass Shift' field is zero!",
                            "Scout (beta version) :: Warning",
                            (System.Windows.MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                            (System.Windows.MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Warning);

                UpDownFreeResidueTol.Focus();
                return true;
            }

            return false;
        }

        private bool AddOrUpdateLabellingReagent()
        {
            string name = TextName.Text;
            string target_residues = TextTargetResidues.Text;
            double mass_shift = (double)UpDownFreeResidueTol.Value;

            bool isNew = false;
            if (IsobaricLabelling_Mods != null &&
                IsobaricLabelling_Mods.Where(a => a.Name == name).ToList().Count > 0)
            {

                SpectrumWizard.AminoacidMod? current_aa = IsobaricLabelling_Mods.FirstOrDefault(a => a.Name == name);
                if (current_aa == null)
                {
                    isNew = true;
                }
                else
                {
                    //Update
                    current_aa.TargetResidues = target_residues;
                    current_aa.MassShift = mass_shift;

                    System.Windows.Forms.MessageBox.Show("Labelling reagent has been updated sucessfully!", "Scout (beta version) :: Warning",
                       System.Windows.Forms.MessageBoxButtons.OK,
                       System.Windows.Forms.MessageBoxIcon.Information);
                }

            }
            else isNew = true;

            if (isNew == true)
            {
                //Add
                SpectrumWizard.AminoacidMod aa = new SpectrumWizard.AminoacidMod
                {
                    Name = name,
                    TargetResidues = target_residues,
                    MassShift = mass_shift,
                    IsVariable = false,
                    IsCTerm = false,
                    IsNTerm = true
                };

                IsobaricLabelling_Mods.Add(aa);

                System.Windows.Forms.MessageBox.Show("Labelling reagent has been added sucessfully!", "Scout (beta version) :: Warning",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Information);
            }


            return true;
        }

        private void DataGridReagents_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void DataGridReagents_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Delete)
                return;

            SpectrumWizard.AminoacidMod reagent = (SpectrumWizard.AminoacidMod)DataGridReagents.SelectedItem;
            var r = System.Windows.Forms.MessageBox.Show("Do you want to remove '" + reagent.Name + "'?", "Scout (beta version) :: Warning", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning);
            if (r == System.Windows.Forms.DialogResult.Yes)
            {
                IsobaricLabelling_Mods.Remove(reagent);
                Settings.Default.LabellingReagents = ScoutCore.FileManagement.Serializer.ToJSON(
                            IsobaricLabelling_Mods, false, true);
                Settings.Default.Save();
                System.Windows.MessageBox.Show(
                            "Reagent has been removed successfully!",
                            "Scout (beta version) :: Information",
                            (System.Windows.MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                            (System.Windows.MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Information);
                CleanFields();
                UpdateDataGrid();
            }
            else
            {
                e.Handled = true;
            }
        }

        private void ButtonLoad_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog()
            {
                Title = "Load Scout labelling reagents",
                Filter = "Scout labelling reagents|*.json",
                AddExtension = true
            };

            if (ofd.ShowDialog() != true) return;

            try
            {

                string content = File.ReadAllText(ofd.FileName);
                IsobaricLabelling_Mods = ScoutCore.FileManagement.Serializer.FromJson<List<SpectrumWizard.AminoacidMod>>(content, true);
                Settings.Default.LabellingReagents = ScoutCore.FileManagement.Serializer.ToJSON(
                            IsobaricLabelling_Mods, false, true);
                Settings.Default.Save();

                Load(SearchParams);

                System.Windows.MessageBox.Show(
                                    "Labelling reagents have been loaded successfully!",
                                    "Scout (beta) :: Information",
                                    (System.Windows.MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                                    (System.Windows.MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Information);
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show(
                                "Failed to load labelling reagents!",
                                "Scout (beta version) :: Error",
                                (System.Windows.MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                                (System.Windows.MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Error);
                return;
            }
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new Microsoft.Win32.SaveFileDialog()
            {
                Title = "Scout labelling reagents",
                Filter = "Scout labelling reagents|*.json",
                FileName = "scout_labelling_reagents.json",
                AddExtension = true
            };

            if (sfd.ShowDialog() != true) return;

            try
            {
                File.Delete(sfd.FileName);
                string json = ScoutCore.FileManagement.Serializer.ToJSON(
                            IsobaricLabelling_Mods, false, true);
                File.WriteAllText(sfd.FileName, json);
                System.Windows.MessageBox.Show(
                                        "Labelling reagents have been exported successfully!",
                                        "Scout (beta version) :: Information",
                                        (System.Windows.MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                                        (System.Windows.MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Information);
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show(
                                "Failed to export reagents!",
                                "Scout (beta version) :: Error",
                                (System.Windows.MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                                (System.Windows.MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Error);
                return;
            }
        }

        private void CleanFields()
        {
            TextName.Text = "";
            TextTargetResidues.Text = "";
            UpDownFreeResidueTol.Value = 0;
        }

        private void DataGridReagents_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                SpectrumWizard.AminoacidMod current_aa = (SpectrumWizard.AminoacidMod)((DataGrid)(sender)).CurrentItem;
                if (current_aa == null) return;

                CleanFields();
                FillReagentFields(current_aa);
            }
            catch (Exception)
            {
                CleanFields();
                TextName.IsReadOnly = false;
                TextName.Focus();
            }
        }
        private void FillReagentFields(SpectrumWizard.AminoacidMod current_aa)
        {
            TextName.Text = current_aa.Name; TextName.IsReadOnly = true;
            TextTargetResidues.Text = current_aa.TargetResidues;
            UpDownFreeResidueTol.Value = current_aa.MassShift;
        }

        private void UpdateDataGrid()
        {
            DataGridReagents.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {
                DataGridReagents.ItemsSource = null;
                DataGridReagents.ItemsSource = IsobaricLabelling_Mods;
            }));
        }

        private void TextTargetResidues_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            tb.Text = tb.Text.ToUpper();
            tb.CaretIndex = tb.Text.Length;
        }

        private bool IsTextOnlyLetters(string text)
        {
            foreach (char c in text)
            {
                if (!char.IsLetter(c))
                    return false;
            }
            return true;
        }

        private void TextTargetResidues_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!IsTextOnlyLetters(e.Text))
                e.Handled = true;
        }

        private void TextTargetResidues_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
        }
    }
}
