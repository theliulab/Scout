using Digestor;
using Microsoft.Win32;
using ScoutCore;
using Scout.Properties;
using SpectrumWizard;
using SpectrumWizard.Predictors.CleavableXL;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Resources;
using System.ComponentModel;
using System.Reflection;
using Scout.Parameters;
using ICSharpCode.SharpZipLib.Zip;

namespace Scout.Parameters
{
    /// <summary>
    /// Interaction logic for WindowScoutParametersEdit.xaml
    /// </summary>
    public partial class WindowScoutParametersEdit : Window
    {
        public class DatagridItemModification
        {
            public AminoacidMod Mod { get; set; }

            public bool ModSelected { get; set; }
            public string Name { get => Mod.Name; }
            public double MassShift { get => Mod.MassShift; }
            public string Residues { get => Mod.TargetResidues; }
            public bool IsNTerm { get => Mod.IsNTerm; }

            public DatagridItemModification(AminoacidMod mod)
            {
                Mod = mod;
                ModSelected = false;
            }
        }


        public ScoutParameters ScoutParams { get; set; }
        public List<CleaveReagent> ReagentLibrary { get; }
        public List<Enzyme> EnzymeLibrary { get; }
        public bool AcceptedChanges { get; private set; }

        public bool IsRestore { get; set; }
        private List<AminoacidMod> VariableMods { get; set; }
        private List<AminoacidMod> StaticMods { get; set; }

        public WindowScoutParametersEdit(
            ScoutParameters scoutParams,
            List<CleaveReagent> reagentLibrary,
            List<Enzyme> enzymeLibrary,
            List<AminoacidMod> variableMods,
            List<AminoacidMod> staticMods
            )
        {
            InitializeComponent();

            ScoutParams = scoutParams.Copy(scoutParams);
            ReagentLibrary = reagentLibrary.ToList();
            EnzymeLibrary = enzymeLibrary.ToList();
            VariableMods = variableMods;
            StaticMods = staticMods;

            LoadParamsToGUI(ScoutParams);
            LoadDatagrids(variableMods, staticMods);
        }

        private void LoadDatagrids(
            List<AminoacidMod> variableMods,
            List<AminoacidMod> staticMods)
        {
            var varMods = variableMods.Select(a => new DatagridItemModification(a)).ToList();
            var statMods = staticMods.Select(a => new DatagridItemModification(a)).ToList();

            foreach (var m in varMods)
            {
                if (ScoutParams.VariableModifications.Exists(a => a.Name == m.Mod.Name))
                {
                    m.ModSelected = true;
                }
            }

            foreach (var m in statMods)
            {
                if (ScoutParams.StaticModifications.Exists(a => a.Name == m.Mod.Name))
                {
                    m.ModSelected = true;
                }
            }

            DataGridVariableMods.ItemsSource = null;
            DataGridVariableMods.ItemsSource = varMods;

            DataGridStaticMods.ItemsSource = null;
            DataGridStaticMods.ItemsSource = statMods;

            DataGridReagents.ItemsSource = null;
            DataGridReagents.ItemsSource = ReagentLibrary;

            DataGridEnzymes.ItemsSource = null;
            DataGridEnzymes.ItemsSource = EnzymeLibrary;

            ContaminantsText.Text = Digestor.Properties.Contaminants.Default.UserContaminants;
        }

        private void LoadParamsToGUI(ScoutParameters scoutParams)
        {
            UpDownPPM.Value = scoutParams.PPMMS1Tolerance;
            UpDownPPM_MS2.Value = scoutParams.PPMMS2Tolerance;
            UpDownMinPepLength.Value = scoutParams.MinPepLength;
            UpDownMaxPepLength.Value = scoutParams.MaxPepLength;
            UpDownMissedCleavages.Value = scoutParams.MiscleavageNum;
            UpDownMinPepMass.Value = scoutParams.MinPepMass;
            UpDownMaxPepMass.Value = scoutParams.MaxPepMass;
            UpDownMaxMods.Value = scoutParams.MaximumVariableModsPerPeptide;
            UpDownIonPairPPM.Value = scoutParams.PairFinderPPM;
            CheckDeconvIonPair.IsChecked = scoutParams.DeconvolutionForPairSearching;
            CheckDeconvScoring.IsChecked = scoutParams.DeconvolutionForMSScoring;

            ComboEnzyme.ItemsSource = null;
            ComboEnzyme.ItemsSource = EnzymeLibrary;

            int enzymeIndex = scoutParams.Enzyme != null && scoutParams.Enzyme.GetProtease() != null ? EnzymeLibrary.FindIndex(a => a.GetProtease().Name == scoutParams.Enzyme.GetProtease().Name) : -1;
            if (enzymeIndex != -1)
                ComboEnzyme.SelectedIndex = enzymeIndex;

            int enzymeSpecIndex = 1;
            if (scoutParams.EnzymeSpecificity == Enzyme.EnzymeSpecificity.FullySpecific)
                enzymeSpecIndex = 0;
            ComboEnzymeSpecificity.SelectedIndex = enzymeSpecIndex;

            ComboCleaveReagent.ItemsSource = null;
            ComboCleaveReagent.ItemsSource = ReagentLibrary;

            int reagentIndex = scoutParams.CXLReagent != null && scoutParams.CXLReagent.Name != null ? ReagentLibrary.FindIndex(a => a.Name == scoutParams.CXLReagent.Name) : -1;
            if (reagentIndex != -1)
                ComboCleaveReagent.SelectedIndex = reagentIndex;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (IsRestore) return;

            var r = System.Windows.Forms.MessageBox.Show("Accept changes?", "Scout (beta version) :: Information", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Information);

            if (r != System.Windows.Forms.DialogResult.Yes) { return; }

            ScoutParams = GetScoutParamsFromGUI();
            SaveContaminants(ContaminantsText.Text);

            AcceptedChanges = true;
        }

        private void SaveContaminants(string contaminants)
        {
            Digestor.Properties.Contaminants.Default.UserContaminants = contaminants;
            Digestor.Properties.Contaminants.Default.Save();
        }

        private ScoutParameters GetScoutParamsFromGUI()
        {
            var scoutParams = ScoutParams.Copy(ScoutParams);

            scoutParams.PPMMS1Tolerance = (double)UpDownPPM.Value;
            scoutParams.PPMMS2Tolerance = (double)UpDownPPM_MS2.Value;
            scoutParams.MinPepLength = (int)UpDownMinPepLength.Value;
            scoutParams.MaxPepLength = (int)UpDownMaxPepLength.Value;
            scoutParams.MiscleavageNum = (int)UpDownMissedCleavages.Value;
            scoutParams.MinPepMass = (double)UpDownMinPepMass.Value;
            scoutParams.MaxPepMass = (double)UpDownMaxPepMass.Value;
            scoutParams.MaximumVariableModsPerPeptide = (int)UpDownMaxMods.Value;
            scoutParams.PairFinderPPM = (int)UpDownIonPairPPM.Value;
            scoutParams.FullPairsOnly = false;
            scoutParams.DeconvolutionForPairSearching = (bool)CheckDeconvIonPair.IsChecked;
            scoutParams.DeconvolutionForMSScoring = (bool)CheckDeconvScoring.IsChecked;

            scoutParams.VariableModifications = ((List<DatagridItemModification>)DataGridVariableMods.ItemsSource)
                .Where(a => a.ModSelected)
                .Select(a => a.Mod)
                .ToList();
            scoutParams.StaticModifications = ((List<DatagridItemModification>)DataGridStaticMods.ItemsSource)
                .Where(a => a.ModSelected)
                .Select(a => a.Mod)
                .ToList();

            MainWindow.VariableMods = ((List<DatagridItemModification>)DataGridVariableMods.ItemsSource).Select(a => a.Mod).ToList();
            MainWindow.StaticMods = ((List<DatagridItemModification>)DataGridStaticMods.ItemsSource).Select(a => a.Mod).ToList();

            scoutParams.CXLReagent = (CleaveReagent)ComboCleaveReagent.SelectedItem;
            scoutParams.Enzyme = (Enzyme)ComboEnzyme.SelectedItem;
            var enzymeSpecIndex = ComboEnzymeSpecificity.SelectedIndex;
            scoutParams.EnzymeSpecificity = enzymeSpecIndex == 0 ? Enzyme.EnzymeSpecificity.FullySpecific : Enzyme.EnzymeSpecificity.SemiSpecific;

            return scoutParams;
        }

        private void Button_AddNewReagent(object sender, RoutedEventArgs e)
        {
            var w = new WindowAddNewReagent();

            w.ShowDialog();

            if (w.Accepted == true)
            {
                ReagentLibrary.Add(w.Reagent);
                MainWindow.CleaveReagents = ReagentLibrary;

                CleaveReagent s = (CleaveReagent)ComboCleaveReagent.SelectedItem;


                ComboCleaveReagent.ItemsSource = null;
                ComboCleaveReagent.SelectedItem = null;
                ComboCleaveReagent.ItemsSource = ReagentLibrary;
                ComboCleaveReagent.SelectedIndex = ReagentLibrary.IndexOf(s);

                DataGridReagents.ItemsSource = null;
                DataGridReagents.ItemsSource = ReagentLibrary;
            }
        }

        private void Button_AddModification(object sender, RoutedEventArgs e)
        {
            var w = new WindowAddNewModification();

            w.ShowDialog();

            if (w.Accepted == true)
            {
                var varMods = (List<DatagridItemModification>)DataGridVariableMods.ItemsSource;
                var statMods = (List<DatagridItemModification>)DataGridStaticMods.ItemsSource;

                if ((varMods != null && varMods.Where(a => a.Name.Equals(w.Modification.Name)).Count() > 0) ||
                    (statMods != null && statMods.Where(a => a.Name.Equals(w.Modification.Name)).Count() > 0))
                {
                    System.Windows.Forms.MessageBox.Show("A modification with the same name already exists!", "Warning",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning);

                    w = new WindowAddNewModification(w.Modification);

                    w.ShowDialog();

                    if (w.Accepted == true)
                    {
                        EditModification(w.Modification);
                    }
                    return;
                }

                if (w.Modification.IsVariable == true)
                {
                    varMods.Add(new DatagridItemModification(w.Modification));

                    DataGridVariableMods.ItemsSource = null;
                    DataGridVariableMods.ItemsSource = varMods;
                }
                else
                {
                    statMods.Add(new DatagridItemModification(w.Modification));

                    DataGridStaticMods.ItemsSource = null;
                    DataGridStaticMods.ItemsSource = statMods;
                }

            }
        }

        private void EditModification(AminoacidMod modification)
        {
            var varMods = (List<DatagridItemModification>)DataGridVariableMods.ItemsSource;
            var statMods = (List<DatagridItemModification>)DataGridStaticMods.ItemsSource;

            //Try to retrieve a variable mod
            var currentMod = varMods.Where(a => a.Name.Equals(modification.Name)).FirstOrDefault();

            if (currentMod == null)
            {
                //Try to retrieve a static mod
                currentMod = statMods.Where(a => a.Name.Equals(modification.Name)).FirstOrDefault();
                if (currentMod == null) return;
            }

            if (currentMod.Mod.IsVariable == true)
            {
                if (modification.IsVariable == true)
                    currentMod.Mod = modification;
                else
                {
                    //remove mod from variable mod list
                    varMods = varMods.Where(a => !a.Name.Equals(modification.Name)).ToList();

                    //add mod in static mod list
                    statMods.Add(new DatagridItemModification(modification));

                    DataGridStaticMods.ItemsSource = null;
                    DataGridStaticMods.ItemsSource = statMods;
                }
                DataGridVariableMods.ItemsSource = null;
                DataGridVariableMods.ItemsSource = varMods;
            }
            else
            {
                if (modification.IsVariable == false)
                    currentMod.Mod = modification;
                else
                {
                    //remove mod from static mod list
                    statMods = statMods.Where(a => !a.Name.Equals(modification.Name)).ToList();

                    //add mod in variable mod list
                    varMods.Add(new DatagridItemModification(modification));

                    DataGridVariableMods.ItemsSource = null;
                    DataGridVariableMods.ItemsSource = varMods;
                }
                DataGridStaticMods.ItemsSource = null;
                DataGridStaticMods.ItemsSource = statMods;
            }
        }

        private void DataGridMods_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = ((DataGrid)e.Source).SelectedItem;
            if (item == null) return;

            var dgMod = ((DatagridItemModification)item);
            var w = new WindowAddNewModification(dgMod.Mod);

            w.ShowDialog();

            if (w.Accepted == true)
            {
                EditModification(w.Modification);
            }
        }

        private void DataGridVariableMods_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            //e.Cancel = true;
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

            if (sfd.ShowDialog() != true) return;

            try
            {
                var scoutParams = GetScoutParamsFromGUI();
                string json = scoutParams.ToSaveJson();
                File.Delete(sfd.FileName);
                File.WriteAllText(sfd.FileName, json);
                System.Windows.Forms.MessageBox.Show("Search parameters have been saved successfully!", "Information",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: Could not save search parameters.\n{0}\n{1}", ex.Message, ex.StackTrace);
                System.Windows.Forms.MessageBox.Show("Could not save search parameters!", "Error",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private void ButtonLoad_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog()
            {
                Title = "Load Scout search parameters",
                Filter = "Scout search params|*.json",
                AddExtension = true
            };

            if (ofd.ShowDialog() != true) return;

            try
            {
                ScoutParameters newParams = ScoutParameters.Load(ofd.FileName);

                ScoutParams = newParams;
                if (ReagentLibrary.Exists(a => a.Name == ScoutParams.CXLReagent.Name) == false)
                {
                    ReagentLibrary.Add(ScoutParams.CXLReagent);
                }
                var new_mod = CheckModifications(VariableMods, ScoutParams.VariableModifications);
                if (new_mod.Count > 0)
                {
                    VariableMods.AddRange(new_mod);
                }
                new_mod = CheckModifications(StaticMods, ScoutParams.StaticModifications);
                if (new_mod.Count > 0)
                {
                    StaticMods.AddRange(new_mod);
                }
                if (EnzymeLibrary.Exists(a => a.Name == ScoutParams.Enzyme.Name) == false)
                {
                    EnzymeLibrary.Add(ScoutParams.Enzyme);
                }

                LoadParamsToGUI(ScoutParams);
                LoadDatagrids(VariableMods, StaticMods);

                System.Windows.Forms.MessageBox.Show("Search Parameters have been loaded successfully!", "Information",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: Could not load search parameters.\n{0}\n{1}", ex.Message, ex.StackTrace);
                System.Windows.Forms.MessageBox.Show("Could not load search parameters!", "Error",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private List<AminoacidMod> CheckModifications(List<AminoacidMod> currentMods, List<AminoacidMod> paramsMods)
        {
            List<AminoacidMod> new_list = new();
            foreach (var mod in paramsMods)
            {
                if (currentMods.Exists(a => a.Name == mod.Name) == false)
                {
                    AminoacidMod _new = new();
                    mod.ModIndex = _new.ModIndex;
                    new_list.Add(mod);
                }
            }
            return new_list;
        }


        private void ButtonSetAsDefault_Click(object sender, RoutedEventArgs e)
        {
            var r = System.Windows.Forms.MessageBox.Show("This will override the application's default parameters. Are you sure?", "Information", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Information);

            if (r != System.Windows.Forms.DialogResult.Yes) { return; }

            var scoutParams = GetScoutParamsFromGUI();

            Settings.Default.ScoutParamsJson = scoutParams.ToSaveJson();
            Settings.Default.Save();
            System.Windows.Forms.MessageBox.Show("Parameters have been set sucessfully!", "Information", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
        }

        private void ButtonRestoreDefault_Click(object sender, RoutedEventArgs e)
        {
            var r = System.Windows.Forms.MessageBox.Show("This will remove the current settings and restore the default parameters. Are you sure?", "Warning", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning);

            if (r != System.Windows.Forms.DialogResult.Yes) { IsRestore = false; return; }

            Settings.Default.ScoutParamsJson = string.Empty;
            Settings.Default.PostParamsJson = string.Empty;
            Settings.Default.ReagentsJson = string.Empty;
            Settings.Default.EnzymesJson = string.Empty;
            Settings.Default.VariableModsJson = string.Empty;
            Settings.Default.StaticModsJson = string.Empty;

            Settings.Default.Save();

            IsRestore = true;

            System.Windows.Forms.MessageBox.Show("Parameters have been restored sucessfully!", "Information", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            this.Close();
        }

        private void HyperlinkAdvanced_Click(object sender, RoutedEventArgs e)
        {
            var scoutParams = GetScoutParamsFromGUI();
            var w = new WindowAdvancedSearchParams(scoutParams, this);

            w.ShowDialog();

            if (w.Accepted == true)
            {
                ScoutParams = w.SearchParams;
            }
        }

        private void DataGridReagents_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete)
                return;

            string xl_reagent = GetSelectedValue(DataGridReagents);
            var r = System.Windows.Forms.MessageBox.Show("Do you want to remove '" + xl_reagent + "'?", "Warning", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning);
            if (r != System.Windows.Forms.DialogResult.Yes)
            {
                e.Handled = true;
                return;
            }

            MainWindow.CleaveReagents = ReagentLibrary;

        }

        private string GetSelectedValue(DataGrid grid, int columnIndex = 0)
        {
            if (grid.SelectedCells.Count == 0) return string.Empty;

            DataGridCellInfo cellInfo = grid.SelectedCells[columnIndex];
            if (cellInfo.Column == null) return "0";

            DataGridBoundColumn column = cellInfo.Column as DataGridBoundColumn;
            if (column == null) return null;

            FrameworkElement element = new FrameworkElement() { DataContext = cellInfo.Item };
            BindingOperations.SetBinding(element, TagProperty, column.Binding);

            return element.Tag.ToString();
        }

        private void DataGridVariableMods_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete)
                return;

            string mod = GetSelectedValue(DataGridVariableMods, 1);
            var r = System.Windows.Forms.MessageBox.Show("Do you want to remove '" + mod + "'?", "Warning", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning);
            if (r != System.Windows.Forms.DialogResult.Yes)
            {
                e.Handled = true;
                return;
            }
        }

        private void DataGridStaticMods_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete)
                return;

            string mod = GetSelectedValue(DataGridStaticMods, 1);
            var r = System.Windows.Forms.MessageBox.Show("Do you want to remove '" + mod + "'?", "Warning", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning);
            if (r != System.Windows.Forms.DialogResult.Yes)
            {
                e.Handled = true;
                return;
            }
        }

        private void DataGridEnzymes_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete)
                return;

            string enzyme = GetSelectedValue(DataGridEnzymes);
            var r = System.Windows.Forms.MessageBox.Show("Do you want to remove '" + enzyme + "'?", "Warning", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning);
            if (r != System.Windows.Forms.DialogResult.Yes)
            {
                e.Handled = true;
                return;
            }

            MainWindow.Enzymes = EnzymeLibrary;
        }

        private void Button_AddNewEnzyme(object sender, RoutedEventArgs e)
        {
            var w = new WindowAddNewEnzyme();

            w.ShowDialog();

            if (w.Accepted == true)
            {
                EnzymeLibrary.Add(w.Enzyme);
                MainWindow.Enzymes = EnzymeLibrary;

                Enzyme enzyme = (Enzyme)ComboEnzyme.SelectedItem;
                ComboEnzyme.ItemsSource = null;
                ComboEnzyme.SelectedItem = null;
                ComboEnzyme.ItemsSource = EnzymeLibrary;
                ComboEnzyme.SelectedIndex = EnzymeLibrary.IndexOf(enzyme);

                DataGridEnzymes.ItemsSource = null;
                DataGridEnzymes.ItemsSource = EnzymeLibrary;
            }
        }
    }
}
