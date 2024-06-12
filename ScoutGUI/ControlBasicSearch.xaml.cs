using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using ScoutCore;
using ScoutCore.SpectraOperations;
using Scout.Results;
using ScoutPostProcessing;
using SpectrumWizard;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using ThermoFisher.CommonCore.Data;
using System.Security.Cryptography;
using Scout.Util;
using System.Security.RightsManagement;
using Accord.Statistics.Distributions.Univariate;
using System.Text.RegularExpressions;

namespace Scout
{
    /// <summary>
    /// Interaction logic for ControlBasicSearch.xaml
    /// </summary>
    public partial class ControlBasicSearch : UserControl
    {
        public static Program mainProgramGUI = new Program();
        public static bool SearchStatus { get; set; }
        public static ScoutParameters? SearchParameters { get; set; }
        public static PostProcessingParameters? PostParameters { get; set; }
        private bool FolderMode { get; set; }
        public static CancellationTokenSource? _tokenSource = null;

        public ControlBasicSearch()
        {
            InitializeComponent();

            ScoutVersion.Text = "Scout - v. " + ScoutCore.Utils.GetAppVersion();
        }

        private void RadioFolder_Changed(object sender, RoutedEventArgs e)
        {
            if (IsLoaded == false) return;

            if (RadioFolder.IsChecked == true)
            {
                FolderMode = true;
                TextFolder.IsEnabled = true;
                ButtonFolder.IsEnabled = true;

                TextFile.IsEnabled = false;
                ButtonFile.IsEnabled = false;
            }
            else
            {
                FolderMode = false;
                TextFolder.IsEnabled = false;
                ButtonFolder.IsEnabled = false;

                TextFile.IsEnabled = true;
                ButtonFile.IsEnabled = true;
            }
        }
        private void ButtonBrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new CommonOpenFileDialog()
            {
                //Filters = new List<string>() { "Raw file (.raw)|*.raw" },
                IsFolderPicker = true,

            };

            if (ofd.ShowDialog() != CommonFileDialogResult.Ok) return;

            setInitialParams();
            TextFolder.Text = ofd.FileName;
        }
        private void ButtonBrowseFile_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new FileFolderDialog();
            ofd.Filter = "Spectra files (*.raw, *.mgf, *.ms2, *.mzml)|*.raw;*.mgf;*.ms2;*.mzml|Raw file|*.raw|MGF file|*.mgf|MS2 file|*.ms2|mzML file|*.mzml|Bruker file|*.d";
            ofd.ShowDialog();
            if (String.IsNullOrEmpty(ofd.SelectedPaths) && String.IsNullOrEmpty(ofd.SelectedPath)) return;
            if (!String.IsNullOrEmpty(ofd.SelectedPaths))
                TextFile.Text = ofd.SelectedPaths.Substring(0, ofd.SelectedPaths.Length - 1);
            else
                TextFile.Text = ofd.SelectedPath;

            if (mainProgramGUI == null)
                mainProgramGUI = new Program();
            setInitialParams();

            if (!String.IsNullOrEmpty(TextFile.Text))
            {
                string[] qtd_files = Regex.Split(TextFile.Text, ";");
                if (qtd_files.Length == 1)
                    mainProgramGUI.SearchParameters.MSFileExtension = Path.GetExtension(TextFile.Text);
                else
                    mainProgramGUI.SearchParameters.MSFileExtension = "_multiple";
            }
        }
        private void ButtonBrowseFasta_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog()
            {
                Filter = "Fasta file|*.fasta"
            };

            if (ofd.ShowDialog() != true) return;

            TextFasta.Text = ofd.FileName;
            if (mainProgramGUI == null)
                mainProgramGUI = new Program();
            setInitialParams();
            mainProgramGUI.SearchParameters.FastaFile = TextFasta.Text;
        }
        async void ButtonSearch_Click(object sender, RoutedEventArgs e)
        {
            if (ButtonFullSearch.Tag.Equals("search"))
            {
                if (String.IsNullOrEmpty(TextFasta.Text))
                {
                    Console.WriteLine("ERROR: No database file has been found. Please select one.");
                    return;
                }
                else if (FolderMode == false && String.IsNullOrEmpty(TextFile.Text))
                {
                    Console.WriteLine("ERROR: No spectra file has been found. Please select one.");
                    return;
                }
                else if (FolderMode == true && String.IsNullOrEmpty(TextFolder.Text))
                {
                    Console.WriteLine("ERROR: 'RAW folder' is empty. Please set a directory where the spectra files are.");
                    return;
                }

                if (mainProgramGUI == null)
                    mainProgramGUI = new Program();

                if (SearchParameters.BDP_Mode)
                {
                    SearchParameters.CXLReagent = SpectrumWizard.Predictors.CleavableXL.CleaveReagent.BDP_NHP;
                    MessageBox.Show("BDP MODE IS ON!!! SWITCHING TO BDP-NHP REAGENT!!!!");
                }

                setInitialParams();
                mainProgramGUI.SearchParameters.FastaFile = TextFasta.Text;
                mainProgramGUI.FolderMode = FolderMode;

                if (FolderMode == false && !String.IsNullOrEmpty(TextFile.Text))
                {
                    mainProgramGUI.SearchParameters.RawPath = TextFile.Text;
                }
                else if (FolderMode == true && !String.IsNullOrEmpty(TextFolder.Text))
                {
                    mainProgramGUI.SearchParameters.RawPath = TextFolder.Text;
                }

                ChangeStartButtonLabel(1);

                SearchStatus = true;//Start search
                _tokenSource = new CancellationTokenSource();
                var token = _tokenSource.Token;
                try
                {
                    await runAsync(token);
                }
                catch (Exception)
                {
                    SearchStatus = false;//Search has been finished.
                    Console.WriteLine("WARN: Task has been cancelled!");
                }
                finally
                {
                    _tokenSource.Dispose();
                }
                SearchStatus = false;//Search has been finished.
            }
            else
            {
                var cancel_task = System.Windows.Forms.MessageBox.Show($"Are you sure you want to cancel the process?", "Scout :: Warning", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning);

                if (cancel_task == System.Windows.Forms.DialogResult.Yes)
                {
                    _tokenSource.Cancel();
                    ChangeStartButtonLabel(2);
                }
            }
        }

        public void ChangeStartButtonLabel(byte status)
        {
            if (status == 0)
            {
                #region Set 'Start' image in the button
                ButtonFullSearch.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                {
                    ButtonFullSearch.IsEnabled = true;
                    ButtonFullSearch.Tag = "search";
                }));
                run_btn_text.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                {
                    run_btn_text.Text = "Start";
                    run_btn_text.Width = 30;
                    run_btn_img.Source = new BitmapImage(new Uri("pack://application:,,,/start.png"));
                }));
                #endregion
            }
            else if (status == 1)
            {
                #region Set 'Cancel' image in the button
                ButtonFullSearch.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                {
                    ButtonFullSearch.Tag = "cancel";
                }));
                run_btn_text.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                {
                    run_btn_text.Text = "Cancel";
                    run_btn_text.Width = 35;
                    run_btn_img.Source = new BitmapImage(new Uri("pack://application:,,,/exception.png"));
                }));
                #endregion
            }
            else if (status == 2)
            {
                #region Set 'Canceling' image in the button
                ButtonFullSearch.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { ButtonFullSearch.IsEnabled = false; }));
                run_btn_text.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                {
                    run_btn_text.Text = "Wait. Canceling...";
                    run_btn_text.Width = 90;
                }));
                #endregion
            }
        }

        private void HandleFinishedThread()
        {
            if (mainProgramGUI.PostResults != null)
            {
                if (!String.IsNullOrEmpty(mainProgramGUI.WarningMSG))
                {
                    System.Windows.MessageBox.Show(
                       "One or more warnings happened during the searching:\n\n" +
                       mainProgramGUI.WarningMSG,
                       "Scout :: Warning",
                       (System.Windows.MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                       (System.Windows.MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Warning);
                }

                string[] rawFolder = mainProgramGUI.SearchParameters.RawPath.Split(System.IO.Path.DirectorySeparatorChar);

                string fileName = rawFolder[rawFolder.Length - 1];
                if (fileName.Contains("."))// Here is the file name properly. So, get the dir name.
                    fileName = rawFolder[rawFolder.Length - 2];

                fileName += ".scout";

                WindowResultsBrowser w = new WindowResultsBrowser(mainProgramGUI.PostResults, fileName);
                w.Show();
            }
            else
                Console.WriteLine("WARNING: There are no results to display.");

            ChangeStartButtonLabel(0);
        }

        Task runAsync(CancellationToken token)
        {
            var scheduler = TaskScheduler.FromCurrentSynchronizationContext();

            return Task.Factory.StartNew(
            () =>
            { mainProgramGUI.PerformSearch(token); }
            ).ContinueWith(r => HandleFinishedThread(), scheduler);

        }

        public static void CancelledTask()
        {
            if (_tokenSource != null)
                _tokenSource.Cancel();
        }

        private void TextFasta_KeyUp(object sender, KeyEventArgs e)
        {
            if (mainProgramGUI.SearchParameters != null)
                mainProgramGUI.SearchParameters.FastaFile = TextFasta.Text;
        }

        public static void setInitialParams()
        {
            if (mainProgramGUI == null)
                mainProgramGUI = new Program();
            mainProgramGUI.SearchParameters = SearchParameters;
            mainProgramGUI.PostParameters = PostParameters;
            mainProgramGUI.PostResults = null;
            mainProgramGUI.WarningMSG = string.Empty;
        }
    }
}
