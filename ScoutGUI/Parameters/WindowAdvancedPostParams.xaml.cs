using Scout.Properties;
using ScoutCore;
using ScoutPostProcessing;
using ScoutPostProcessing.PPILogic;
using ScoutPostProcessing.ResiduePairLogic;
using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using ThermoFisher.CommonCore.Data.Business;

namespace Scout
{
    /// <summary>
    /// Interaction logic for WindowPostParams.xaml
    /// </summary>
    public partial class WindowPostParams : Window
    {
        public PostProcessingParameters PostParams { get; internal set; }

        public WindowPostParams(PostProcessingParameters postParams)
        {
            PostParams = postParams.Copy();
            InitializeComponent();
            LoadParams();
        }

        private void LoadParams()
        {
            if (PostParams == null)
            {
                return;
            }

            CheckUniquePPIsOnly.IsChecked = PostParams.UniquePPIsOnly;
            CheckSeparateFDR.IsChecked = PostParams.FDRMode == FDRModes.SeparateIntraInter;
            CheckGroupByGene.IsChecked = PostParams.GroupedByGene;
            TextCSMFDR.Text = PostParams.CSM_FDR.ToString();
            TextResPairFDR.Text = PostParams.ResPair_FDR.ToString();
            TextPPIFDR.Text = PostParams.PPI_FDR.ToString();
        }

        private bool SaveParams()
        {
            #region check if some field is empty
            if (String.IsNullOrEmpty(TextCSMFDR.Text))
            {
                System.Windows.Forms.MessageBox.Show("'FDR on CSM level' field is empty. Please fix it!", "Scout :: Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                return false;
            }
            else if (String.IsNullOrEmpty(TextResPairFDR.Text))
            {
                System.Windows.Forms.MessageBox.Show("'FDR on Residue Pair level' field is empty. Please fix it!", "Scout :: Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                return false;
            }
            else if (String.IsNullOrEmpty(TextPPIFDR.Text))
            {
                System.Windows.Forms.MessageBox.Show("'FDR on PPI level' field is empty. Please fix it!", "Scout :: Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                return false;
            }

            #endregion

            try
            {
                PostParams.UniquePPIsOnly = CheckUniquePPIsOnly.IsChecked ?? false;
                PostParams.FDRMode = CheckSeparateFDR.IsChecked == true ? FDRModes.SeparateIntraInter : FDRModes.CombinedIntraInter;
                PostParams.GroupedByGene = CheckGroupByGene.IsChecked ?? false;
                PostParams.CSM_FDR = Convert.ToDouble(TextCSMFDR.Text);
                PostParams.ResPair_FDR = Convert.ToDouble(TextResPairFDR.Text);
                PostParams.PPI_FDR = Convert.ToDouble(TextPPIFDR.Text);

                if (PostParams.CSM_FDR != PostParams.ResPair_FDR ||
                    PostParams.ResPair_FDR != PostParams.PPI_FDR ||
                    PostParams.CSM_FDR != PostParams.PPI_FDR)
                {
                    var r = System.Windows.Forms.MessageBox.Show("Scout can only reliably control the FDR of higher levels if the lower levels were filtered to the same cutoff (e.g. CSM 1%, ResPairs 1%, PPIs 1%).\n\nDo you want to proceed?", "Scout :: Warning", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning);
                    if (r != System.Windows.Forms.DialogResult.Yes) { return false; }
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Could not save post processing parameters.\n" + ex.Message, "Scout (beta version) :: Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return false;
            }
            return true;
        }
        public bool Accepted { get; internal set; }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog()
            {
                Title = "Save Scout post processing parameters",
                Filter = "Scout post processing params|*.json",
                FileName = "Scout_post_processing_params.json",
                AddExtension = true
            };

            if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            try
            {
                SaveParams();
                string json = PostParams.ToSaveJson();
                File.WriteAllText(sfd.FileName, json);
                System.Windows.Forms.MessageBox.Show("Post Processing Parameters have been saved successfully!", "Scout (beta version) :: Information",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: Could not save post processing parameters.\n{0}\n{1}", ex.Message, ex.StackTrace);
                System.Windows.Forms.MessageBox.Show("Could not save post processing parameters!", "Scout (beta version) :: Error",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private void ButtonLoad_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog()
            {
                Title = "Load Scout post processing parameters",
                Filter = "Scout post processing params|*.json",
                AddExtension = true
            };

            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            try
            {
                var newParams = PostProcessingParameters.Load(ofd.FileName);
                PostParams = newParams;
                LoadParams();

                System.Windows.Forms.MessageBox.Show("Post Processing Parameters have been loaded successfully!", "Scout (beta version) :: Information",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: Could not load post processing parameters.\n{0}\n{1}", ex.Message, ex.StackTrace);
                System.Windows.Forms.MessageBox.Show("Could not load post processing parameters!", "Scout (beta version) :: Error",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
            }

        }


        private void ButtonSetAsDefault_Click(object sender, RoutedEventArgs e)
        {
            var r = System.Windows.Forms.MessageBox.Show("This will override the application's default parameters. Are you sure?", "Information", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning);
            if (r != System.Windows.Forms.DialogResult.Yes) { return; }

            Settings.Default.PostParamsJson = PostParams.ToSaveJson();
            Settings.Default.Save();
        }
        private void ButtonRestoreDefault_Click(object sender, RoutedEventArgs e)
        {
            var r = System.Windows.Forms.MessageBox.Show("This will remove the current settings and restore the parameters to software default. Are you sure?", "Scout (beta version) :: Information", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning);
            if (r != System.Windows.Forms.DialogResult.Yes) { return; }

            var defaultParams = new PostProcessingParameters();

            Settings.Default.PostParamsJson = defaultParams.ToSaveJson();
            Settings.Default.Save();

            PostParams = defaultParams;
            LoadParams();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
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

        private static readonly Regex _regex_double = new Regex("[^0-9.]+"); //regex that matches disallowed text
        private static bool IsTextDoubleAllowed(string text)
        {
            return !_regex_double.IsMatch(text);
        }

        private void PreviewTextDoubleInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextDoubleAllowed(e.Text);
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
    }
}
