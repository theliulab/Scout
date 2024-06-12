using Accord.Math.Geometry;
using Accord.Statistics.Distributions.Univariate;
using Microsoft.ML.Data;
using Microsoft.WindowsAPICodePack.Dialogs;
using PatternTools;
using Scout.Results.CSMs;
using Scout.Results.PPIs;
using Scout.Results.ResPairs;
using Scout.Results.Statistics;
using Scout.TruthEvaluation;
using Scout.Util.AboutWindow;
using Scout.Util.Updates;
using ScoutPostProcessing;
using ScoutPostProcessing.CSMLogic;
using ScoutPostProcessing.Output;
using ScoutPostProcessing.PPILogic;
using ScoutPostProcessing.ResiduePairLogic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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

namespace Scout.Results
{
    /// <summary>
    /// Interaction logic for WindowResultsBrowser.xaml
    /// </summary>
    public partial class WindowResultsBrowser : Window
    {
        public static ScoutPostProcessing.XLFilteredResults _post { get; set; }
        //private string _rawsFolder;
        //private FileInfo[] _buf_Paths;

        public WindowResultsBrowser(
            ScoutPostProcessing.XLFilteredResults post,
            string fileName)
        {
            InitializeComponent();

            _post = post;
            //_rawsFolder = rawsFolder;
            //_buf_Paths = buf_paths;

            LoadPost(post);
            this.WindowState = WindowState.Maximized;
            this.DataContext = new WindowResultsBrowserCommandContext(this);

            if (!String.IsNullOrEmpty(fileName))
            {
                //string fileName = post.RawsFolder;
                //string[] rawFolder = post.RawsFolder.Split(System.IO.Path.DirectorySeparatorChar);
                //fileName = rawFolder[rawFolder.Length - 1] + ".scout";
                this.Title = "Scout (beta version) :: v. " + post.FileVersion + " - " + fileName;
            }
            else
                this.Title = "Scout (beta version) :: v. " + post.FileVersion;

        }

        private void LoadPost(XLFilteredResults post)
        {
            MyControlCSMResults.Load(post);
            MyControlResPairResults.Load(post);
            MyControlPPIResults.Load(post);
            MyControlParamsResults.Load(post, this);

            if (post.TotalProcessingTime > 0)
            {
                TimeSpan dt = TimeSpan.FromSeconds(post.TotalProcessingTime);
                ProcessingTimeLabel.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                {
                    ProcessingTimeLabel.Text = " Idle : Processing time: " + dt.Days + " Day(s) "
                    + dt.Hours + " Hour(s) "
                    + dt.Minutes + " Minute(s) "
                    + dt.Seconds + " Second(s)";
                }));


            }
            else
                ProcessingTimeLabel.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                {
                    ProcessingTimeLabel.Text = " Idle : Processing time: 0 seconds";
                }));
        }

        private void MenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void setInitialParams()
        {
            if (ControlBasicSearch.mainProgramGUI == null)
                ControlBasicSearch.mainProgramGUI = new();

            ControlBasicSearch.mainProgramGUI.SearchParameters = _post.SearchParams;
            ControlBasicSearch.mainProgramGUI.PostParameters = _post.PostParams;
        }

        private bool CheckFasta()
        {
            bool new_fasta = false;
            bool isValid = true;
            FileInfo file = new FileInfo(_post.FastaPath);
            while (!file.Exists)
            {
                new_fasta = true;
                var loadFasta = System.Windows.Forms.MessageBox.Show($"Fasta file has not been found. Do you want to load a new database file?", "Scout :: Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (loadFasta == System.Windows.Forms.DialogResult.Yes)
                {
                    var ofd = new OpenFileDialog()
                    {
                        Title = "Select a database file",
                        Filter = "Fasta file|*.fasta"
                    };

                    if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK) break;
                    file = new FileInfo(ofd.FileName);
                }
                else
                {
                    isValid = false;
                    break;
                }
            }

            if (new_fasta == true && isValid == true)
            {
                _post.FastaPath = file.FullName;
                return true;
            }
            else
                return false;
        }

        private Util.WaitProcess.UCWaitScreen? load_wait_screen(string title, string msg)
        {
            var wait_screen = Util.Util.CallWaitWindow(title, msg);
            MainGrid.Children.Add(wait_screen);
            Grid.SetRowSpan(wait_screen, 10);
            Grid.SetRow(wait_screen, 0);
            var rows = MainGrid.RowDefinitions;
            rows[8].Height = new GridLength(8, GridUnitType.Star);

            return wait_screen;
        }

        private void hide_wait_screen(Util.WaitProcess.UCWaitScreen? wait_screen)
        {
            MainGrid.Children.Remove(wait_screen);
            var rows = MainGrid.RowDefinitions;
            rows[8].Height = GridLength.Auto;
        }

        public async void RunFilter()
        {
            CheckFasta();
            setInitialParams();

            var wait_screen = load_wait_screen("Wait", "Filtering results...");
            ThreadWorker worker = new ThreadWorker();
            bool isValid = false;
            await Task.Run(() => isValid = worker.RunFilter());
            hide_wait_screen(wait_screen);

            if (isValid == true)
                HandleFilterThreadDone();
        }

        private void MenuItemFilterAgain_Click(object sender, RoutedEventArgs e)
        {
            RunFilter();
        }

        private void HandleFilterThreadDone()
        {
            if (!String.IsNullOrEmpty(ControlBasicSearch.mainProgramGUI.WarningMSG))
            {
                System.Windows.MessageBox.Show(
                   "One or more warnings happened during the filtering:\n\n" +
                   ControlBasicSearch.mainProgramGUI.WarningMSG,
                   "Scout :: Warning",
                   (System.Windows.MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                   (System.Windows.MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Warning);
            }

            LoadPost(ControlBasicSearch.mainProgramGUI.PostResults);
            System.Windows.MessageBox.Show(
                       "Results have been filtered successfully.",
                       "Scout :: Information",
                       (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                       (System.Windows.MessageBoxImage)MessageBoxIcon.Information);

        }

        private void HandleLoadThreadDone()
        {
            LoadPost(_post);
            System.Windows.MessageBox.Show(
                       "Results have been loaded successfully.",
                       "Scout :: Information",
                       (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                       (System.Windows.MessageBoxImage)MessageBoxIcon.Information);
        }

        public async void ImportSpectra(bool isMzIdentML = false, bool isMzIdentML_1_2 = false, string fileName = "")
        {
            System.Windows.MessageBox.Show(
                       "Please select the directory where the raw files are.",
                       "Scout :: Information",
                       (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                       (System.Windows.MessageBoxImage)MessageBoxIcon.Information);

            var ofd = new CommonOpenFileDialog()
            {
                Title = "Scout :: Import spectra",
                //Filters = new List<string>() { "Raw file (.raw)|*.raw" },
                IsFolderPicker = true,
            };


            if (ofd.ShowDialog() != CommonFileDialogResult.Ok)
            {
                System.Windows.MessageBox.Show(
                      "No directory has been selected!",
                      "Scout :: Warning",
                      (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                      (System.Windows.MessageBoxImage)MessageBoxIcon.Warning);
                return;
            }

            setInitialParams();

            var wait_screen = load_wait_screen("Wait", "Importing spectra...");
            ThreadWorker worker = new ThreadWorker();
            bool isValid = false;
            await Task.Run(() => isValid = worker.RunImportSpectra(ofd.FileName));
            hide_wait_screen(wait_screen);

            if (isValid)
            {
                if (isMzIdentML)
                {
                    wait_screen = load_wait_screen("Wait", "Creating mzIdentML file...");
                    await Task.Run(() => HandleImportSpectraDoneFromMZIdentML(fileName, isMzIdentML_1_2));
                    hide_wait_screen(wait_screen);
                }
                else
                {
                    System.Windows.MessageBox.Show(
                       "Spectra have been imported successfully.",
                       "Scout :: Information",
                       (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                       (System.Windows.MessageBoxImage)MessageBoxIcon.Information);
                }
            }
        }

        private void HandleImportSpectraDoneFromMZIdentML(string filePath, bool isMzIdentML_1_2)
        {
            string error = "";
            bool isValid = false;
            if (isMzIdentML_1_2)
                isValid = XLFilteredResultsSaver.SaveResultsToMzIdentlML_1_2(filePath, _post, out error);
            else
                isValid = XLFilteredResultsSaver.SaveResultsToMzIdentlML_1_3(filePath, _post, out error);

            if (isValid == false)
            {
                System.Windows.Forms.MessageBox.Show($"Unable to save the results!\nERROR: {error}",
                                                     "Scout :: Warning",
                                                     System.Windows.Forms.MessageBoxButtons.OK,
                                                     System.Windows.Forms.MessageBoxIcon.Warning);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("The results have been saved successfully!",
                                                     "Scout :: Information",
                                                     System.Windows.Forms.MessageBoxButtons.OK,
                                                     System.Windows.Forms.MessageBoxIcon.Information);
            }
        }

        private void MenuItemExportScoutCSMsCSV_Click(object sender, RoutedEventArgs e)
        {
            new ExportScoutCSMsCSVKey().Execute(sender);
        }

        private void MenuItemExportScoutResPairsCSV_Click(object sender, RoutedEventArgs e)
        {
            new ExportScoutResPairsCSVKey().Execute(sender);
        }

        private void MenuItemExportScoutPPIsCSV_Click(object sender, RoutedEventArgs e)
        {
            new ExportScoutPPIsCSVKey().Execute(sender);
        }

        private void MenuItemExportScoutRawCSMsCSV_Click(object sender, RoutedEventArgs e)
        {
            new ExportScoutRawCSMsCSVKey().Execute(sender);
        }

        public static void SaveXlinkCyNet(string FileName)
        {
            XLFilteredResultsSaver.SavePPItoXlinkCyNET(
                                        FileName,
                                        ControlPPIResultsNew.GuiPPIs,
                                        _post.ProteinScores,
                                        _post.SearchParams,
                                        _post.PostParams,
                                        _post.FastaPath);
        }

        private void MenuItemExportXlinkCyNET_Click(object sender, RoutedEventArgs e)
        {
            new ExportXlinkCyNETKey().Execute(sender);
        }

        private void CommandBindingOpen_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private async void CommandBindingOpen_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var ofd = new OpenFileDialog()
            {
                Title = "Open Scout results",
                Filter = "Scout files|*.scout",
                FileName = "",
                Multiselect = true
            };

            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            var wait_screen = load_wait_screen("Wait", "Loading file...");
            ThreadWorker worker = new ThreadWorker();
            bool isValid = false;
            await Task.Run(() => isValid = worker.RunLoad(ofd.FileNames));
            hide_wait_screen(wait_screen);

            if (isValid == true)
                HandleLoadThreadDone();
        }

        private void CommandBindingSave_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private async void Execute_Save_Command(string file_name, int selected_index)
        {
            try
            {
                if (file_name.EndsWith(".scout"))
                {
                    var wait_screen = load_wait_screen("Wait", "Saving file...");
                    ThreadWorker worker = new ThreadWorker();
                    bool isValid = false;
                    await Task.Run(() => isValid = worker.SaveScoutFile(file_name, _post));
                    hide_wait_screen(wait_screen);

                    if (isValid == true)
                        System.Windows.Forms.MessageBox.Show("The results have been saved successfully!",
                                                             "Scout :: Information",
                                                             System.Windows.Forms.MessageBoxButtons.OK,
                                                             System.Windows.Forms.MessageBoxIcon.Information);
                }
                else
                {
                    var wait_screen = load_wait_screen("Wait", "Creating mzIdentML file...");
                    ThreadWorker worker = new ThreadWorker();
                    bool isValid = false;
                    string error = "";
                    await Task.Run(() => isValid = worker.SaveMzIDFile(file_name, _post, selected_index, out error));
                    hide_wait_screen(wait_screen);

                    if (isValid == false)
                    {
                        if (error.Contains("Fasta not found"))
                        {
                            bool isFastaValid = CheckFasta();
                            if (isFastaValid == false)
                                throw new Exception(error);
                            Execute_Save_Command(file_name, selected_index);
                        }
                        else if (error.Contains("Spectra object is null"))
                        {
                            var load_spectra = System.Windows.Forms.MessageBox.Show($"Spectra have not been found.\nDo you want to import them?",
                                                                                    "Scout :: Warning",
                                                                                    MessageBoxButtons.YesNo,
                                                                                    MessageBoxIcon.Warning);

                            if (load_spectra == System.Windows.Forms.DialogResult.Yes)
                            {
                                if (selected_index == 2)
                                    ImportSpectra(true, true, file_name);
                                else if (selected_index == 3)
                                    ImportSpectra(true, false, file_name);
                            }
                            else
                                throw new Exception(error);
                        }
                        else
                            throw new Exception(error);
                    }
                    else
                        System.Windows.Forms.MessageBox.Show("The results have been saved successfully!",
                                                             "Scout :: Information",
                                                             System.Windows.Forms.MessageBoxButtons.OK,
                                                             System.Windows.Forms.MessageBoxIcon.Information);
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine("WARN: " + exc.Message + "\n" + exc.StackTrace);
                System.Windows.Forms.MessageBox.Show($"Failed to save!\n{exc.Message}\n{exc.StackTrace}",
                                                     "Scout :: Warning",
                                                     System.Windows.Forms.MessageBoxButtons.OK,
                                                     System.Windows.Forms.MessageBoxIcon.Warning);
            }
        }

        private void CommandBindingSave_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var sfd = new SaveFileDialog()
            {
                Filter = "Scout Result File|*.scout|mzIdentML 1.2 Result File|*.mzid|mzIdentML 1.3 Result File|*.mzid"
            };

            if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            Execute_Save_Command(sfd.FileName, sfd.FilterIndex);
        }

        private void MenuItemQuantitationValues_Click(object sender, RoutedEventArgs e)
        {
            new ExportQuantitationValuesKey().Execute(sender);
        }

        private void MenuItem_About(object sender, RoutedEventArgs e)
        {
            AboutWindow info = new AboutWindow();
            info.ShowDialog();
        }

        private void MenuItemCheckTruth_Click(object sender, RoutedEventArgs e)
        {
            new CheckTruthKey().Execute(sender);
        }

        private void MenuItem_ReadMe(object sender, RoutedEventArgs e)
        {
            new ReadMeKey().Execute(sender);
        }

        private void MenuItemImportSpectra_Click(object sender, RoutedEventArgs e)
        {
            new ImportSpectraKey(this).Execute(sender);
        }

        private void MenuItemExportParameters_Click(object sender, RoutedEventArgs e)
        {
            new ExportParametersKey().Execute(sender);
        }

        private void MenuItem_CheckForUpdates(object sender, RoutedEventArgs e)
        {
            Update winUpdate = new();
            winUpdate.Load();
            winUpdate.ShowDialog();
        }

        private void MenuItem_DiscussionForum(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/diogobor/Scout/issues",
                    UseShellExecute = true,
                });
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show(
                        "Visit the Scout website for more usability information.",
                        "Scout :: Read Me",
                        (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                        (System.Windows.MessageBoxImage)MessageBoxIcon.Information);
                throw;
            }
        }

        private void MenuItemStatistics_Click(object sender, RoutedEventArgs e)
        {
            new CheckStatisticsKey().Execute(sender);
        }

        private void MenuItemExportDatabase_Click(object sender, RoutedEventArgs e)
        {
            new ExportDatabaseKey().Execute(sender);
        }
    }

    internal class ThreadWorker
    {
        private static CancellationTokenSource? _tokenSource = null;
        public bool RunFilter()
        {
            if (ControlBasicSearch.mainProgramGUI != null)
            {
                _tokenSource = new CancellationTokenSource();
                var token = _tokenSource.Token;

                ControlBasicSearch.mainProgramGUI.PostParameters = WindowResultsBrowser._post.PostParams;

                string _warningMSG = "";
                ControlBasicSearch.mainProgramGUI.PostProcessing(
                     token,
                     WindowResultsBrowser._post.FastaPath,
                     out _warningMSG,
                     WindowResultsBrowser._post.BufFiles.Select(a => new FileInfo(a)).ToArray());
                ControlBasicSearch.mainProgramGUI.WarningMSG = _warningMSG;

                return true;
            }
            return false;
        }

        public bool RunLoad(string[] FileNames)
        {
            try
            {
                var post = XLFilteredResults.Load(FileNames);
                WindowResultsBrowser._post = post;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            //XLFilteredResults.LoadAsync(FileName);
            //WindowResultsBrowser._post = XLFilteredResults.Results;
        }

        public bool RunImportSpectra(string _raw_path)
        {
            if (ControlBasicSearch.mainProgramGUI != null)
            {
                CancellationTokenSource? ts = new();
                var token = ts.Token;
                ControlBasicSearch.mainProgramGUI.ImportSpectra(token, _raw_path, WindowResultsBrowser._post);
                return true;
            }
            return false;
        }

        public bool SaveScoutFile(string file_name, XLFilteredResults results)
        {
            return XLFilteredResultsSaver.SaveResultsToBinaryFile(file_name, results);
        }

        public bool SaveMzIDFile(string file_name, XLFilteredResults results, int selected_index, out string error)
        {
            bool isValid = false;
            error = "";

            if (selected_index == 2)
                isValid = XLFilteredResultsSaver.SaveResultsToMzIdentlML_1_2(file_name,
                                                                             results,
                                                                             out error);
            else if (selected_index == 3)
                isValid = XLFilteredResultsSaver.SaveResultsToMzIdentlML_1_3(file_name,
                                                                             results,
                                                                             out error);
            return isValid;
        }
    }

    internal class CloseWindowKey : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            System.Environment.Exit(1);
        }
    }

    internal class ExportXlinkCyNETKey : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public async void Execute(object parameter)
        {
            if (WindowResultsBrowser._post == null)
            {
                Console.WriteLine("WARNING: There is no data to be exported!");
                System.Windows.Forms.MessageBox.Show("There is no data to be exported!",
                                                     "Scout :: Information",
                                                     System.Windows.Forms.MessageBoxButtons.OK,
                                                     System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = ""; // Default file name
            dlg.Filter = "XlinkCyNET|*.csv"; // Filter files by extension
            dlg.Title = "Export results to XlinkCyNET";

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                //check FastaFile
                bool new_fasta = false;
                FileInfo file = new FileInfo(WindowResultsBrowser._post.FastaPath);
                while (!file.Exists)
                {
                    new_fasta = true;
                    var loadFasta = System.Windows.Forms.MessageBox.Show($"Fasta file has not been found. Do you want to load a new database file?",
                                                                         "Scout :: Warning",
                                                                         MessageBoxButtons.YesNo,
                                                                         MessageBoxIcon.Warning);

                    if (loadFasta == System.Windows.Forms.DialogResult.Yes)
                    {
                        var ofd = new OpenFileDialog()
                        {
                            Filter = "Fasta file|*.fasta"
                        };

                        if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK) break;
                        file = new FileInfo(ofd.FileName);
                    }
                    else
                        break;
                }

                if (new_fasta == true)
                {
                    WindowResultsBrowser._post.FastaPath = file.FullName;
                }

                try
                {
                    await Task.Run(
                                () =>
                                {
                                    WindowResultsBrowser.SaveXlinkCyNet(dlg.FileName);
                                });

                    System.Windows.Forms.MessageBox.Show("The results have been exported successfully!", "Scout :: Information", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                }
                catch (Exception exc)
                {
                    Console.WriteLine("ERROR: " + exc.Message);
                    System.Windows.Forms.MessageBox.Show("Failed to export!\n" + exc.Message, "Scout :: Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                }
            }
        }
    }

    internal class ExportQuantitationValuesKey : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var sfd = new SaveFileDialog()
            {
                Title = "Export quantitation values",
                Filter = "Quantation values|*.csv",
                FileName = ""
            };

            if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            try
            {
                XLFilteredResultsSaver.SaveCSMsToQuant(sfd.FileName, ControlPPIResultsNew.GuiPPIs, WindowResultsBrowser._post.SearchParams);
                System.Windows.Forms.MessageBox.Show("The results have been exported successfully!", "Scout :: Information", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            }
            catch (Exception exc)
            {
                Console.WriteLine("ERROR: " + exc.Message);
                System.Windows.Forms.MessageBox.Show("Failed to save!", "Scout :: Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
            }
        }
    }

    internal class ExportScoutCSMsCSVKey : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var sfd = new SaveFileDialog()
            {
                Title = "Export CSMs",
                Filter = "CSMs file|*.csv",
                FileName = ""
            };

            if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            try
            {
                XLFilteredResultsSaver.SaveCSMsCSV(sfd.FileName, ControlCSMResultsNew.GuiCSMs, WindowResultsBrowser._post.SearchParams, WindowResultsBrowser._post.PostParams);
                System.Windows.Forms.MessageBox.Show("The results have been exported successfully!", "Scout :: Information", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            }
            catch (Exception exc)
            {
                Console.WriteLine("ERROR: " + exc.Message);
                System.Windows.Forms.MessageBox.Show("Failed to save!", "Scout :: Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
            }
        }
    }

    internal class ExportScoutResPairsCSVKey : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var sfd = new SaveFileDialog()
            {
                Title = "Export Residue Pairs",
                Filter = "Residue pairs file|*.csv",
                FileName = ""
            };

            if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            try
            {
                XLFilteredResultsSaver.SaveResiduePairsCSV(sfd.FileName, ControlResPairResultsNew.GuiResPairs, WindowResultsBrowser._post.PostParams);
                System.Windows.Forms.MessageBox.Show("The results have been exported successfully!", "Scout :: Information", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            }
            catch (Exception exc)
            {
                Console.WriteLine("ERROR: " + exc.Message);
                System.Windows.Forms.MessageBox.Show("Failed to save!", "Scout :: Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
            }
        }
    }

    internal class ExportScoutPPIsCSVKey : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var sfd = new SaveFileDialog()
            {
                Title = "Export PPIs",
                Filter = "PPIs file|*.csv",
                FileName = ""
            };

            if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            try
            {
                XLFilteredResultsSaver.SavePPIsCSV(sfd.FileName, ControlPPIResultsNew.GuiPPIs, WindowResultsBrowser._post.PostParams);
                System.Windows.Forms.MessageBox.Show("The results have been exported successfully!", "Scout :: Information", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            }
            catch (Exception exc)
            {
                Console.WriteLine("ERROR: " + exc.Message);
                System.Windows.Forms.MessageBox.Show("Failed to save!", "Scout :: Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
            }
        }
    }

    internal class ExportScoutRawCSMsCSVKey : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var sfd = new SaveFileDialog()
            {
                Title = "Export unfiltered CSMs",
                Filter = "CSMs file|*.csv",
                FileName = ""
            };

            if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            try
            {
                XLFilteredResultsSaver.SaveCSMsPythonCSV(sfd.FileName, WindowResultsBrowser._post.PackageCSMs, WindowResultsBrowser._post.SearchParams, false, false);
                System.Windows.Forms.MessageBox.Show("The results have been exported successfully!", "Scout :: Information", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            }
            catch (Exception exc)
            {
                Console.WriteLine("ERROR: " + exc.Message);
                System.Windows.Forms.MessageBox.Show("Failed to save!", "Scout :: Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
            }
        }
    }

    internal class FilterAgainKey : ICommand
    {
        public event EventHandler CanExecuteChanged;
        private WindowResultsBrowser _windowResults;

        public FilterAgainKey(WindowResultsBrowser windowResults)
        {
            this._windowResults = windowResults;
        }
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            if (_windowResults != null)
                _windowResults.RunFilter();
        }
    }

    internal class ImportSpectraKey : ICommand
    {
        public event EventHandler CanExecuteChanged;
        private WindowResultsBrowser _windowResults;

        public ImportSpectraKey(WindowResultsBrowser windowResults)
        {
            this._windowResults = windowResults;
        }
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            if (_windowResults != null)
                _windowResults.ImportSpectra();
        }
    }

    internal class ReadMeKey : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            try
            {
                System.Diagnostics.Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/diogobor/Scout#readme",
                    UseShellExecute = true,
                });
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show(
                        "Visit the Scout website for more usability information.",
                        "Scout :: Read Me",
                        (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                        (System.Windows.MessageBoxImage)MessageBoxIcon.Information);
                throw;
            }
        }
    }

    internal class ExportParametersKey : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var sfd = new SaveFileDialog()
            {
                Title = "Export Scout parameters",
                Filter = "Scout parameters|*.json",
                FileName = ""
            };

            if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            try
            {
                XLFilteredResultsSaver.SaveScoutParameters(sfd.FileName, WindowResultsBrowser._post.SearchParams, WindowResultsBrowser._post.PostParams);
                System.Windows.Forms.MessageBox.Show("Parameters have been exported successfully!", "Scout :: Information", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            }
            catch (Exception exc)
            {
                Console.WriteLine("ERROR: " + exc.Message);
                System.Windows.Forms.MessageBox.Show("Failed to save!", "Scout :: Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
            }
        }
    }

    internal class ExportDatabaseKey : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var sfd = new SaveFileDialog()
            {
                Title = "Export database",
                Filter = "Database|*.fasta",
                FileName = ""
            };

            if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            try
            {
                XLFilteredResultsSaver.SaveScoutDatabase(sfd.FileName, WindowResultsBrowser._post.SearchParams);
                System.Windows.Forms.MessageBox.Show("Database has been exported successfully!", "Scout :: Information", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            }
            catch (Exception exc)
            {
                Console.WriteLine("ERROR: " + exc.Message);
                System.Windows.Forms.MessageBox.Show("Failed to save!", "Scout :: Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
            }
        }
    }
    internal class CheckTruthKey : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            if (!(
               Environment.MachineName.ToLower().Contains("milan") == false
               && Environment.MachineName.ToLower().Contains("dalton") == false
               && Environment.MachineName.ToLower().Contains("pcn141") == false
                && Environment.MachineName.ToLower().Contains("pcn-2022-046") == false
               ))
            {
                var w = new WindowSelectTruthFile(WindowResultsBrowser._post);
                w.ShowDialog();
            }
        }
    }

    internal class CheckStatisticsKey : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var w = new StatisticsWindow(WindowResultsBrowser._post);
            w.ShowDialog();
        }
    }

    public class WindowResultsBrowserCommandContext
    {
        private WindowResultsBrowser _windowResultsBrowser;
        public WindowResultsBrowserCommandContext(WindowResultsBrowser windowResultsBrowser)
        {
            this._windowResultsBrowser = windowResultsBrowser;
        }
        public ICommand CloseWindowCommand
        {
            get
            {
                return new CloseWindowKey();
            }
        }

        public ICommand ExportXlinkCyNETCommand
        {
            get
            {
                return new ExportXlinkCyNETKey();
            }
        }

        public ICommand ExportQuantitationValuesCommand
        {
            get
            {
                return new ExportQuantitationValuesKey();
            }
        }

        public ICommand ExportScoutCSMsCSVCommand
        {
            get
            {
                return new ExportScoutCSMsCSVKey();
            }
        }

        public ICommand ExportScoutResPairsCSVCommand
        {
            get
            {
                return new ExportScoutResPairsCSVKey();
            }
        }

        public ICommand ExportScoutPPIsCSVCommand
        {
            get
            {
                return new ExportScoutPPIsCSVKey();
            }
        }

        public ICommand ExportScoutRawCSMsCSVCommand
        {
            get
            {
                return new ExportScoutRawCSMsCSVKey();
            }
        }

        public ICommand FilterAgainCommand
        {
            get
            {
                return new FilterAgainKey(_windowResultsBrowser);
            }
        }

        public ICommand ImportSpectraCommand
        {
            get
            {
                return new ImportSpectraKey(_windowResultsBrowser);
            }
        }

        public ICommand ReadMeCommand
        {
            get
            {
                return new ReadMeKey();
            }
        }

        public ICommand ExportParametersCommand
        {
            get
            {
                return new ExportParametersKey();
            }
        }
        public ICommand ExportDatabaseCommand
        {
            get
            {
                return new ExportDatabaseKey();
            }
        }

        public ICommand CheckTruthCommand
        {
            get
            {
                return new CheckTruthKey();
            }
        }

        public ICommand CheckStatisticsCommand
        {
            get
            {
                return new CheckStatisticsKey();
            }
        }

    }
}
