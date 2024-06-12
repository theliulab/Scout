using ScoutCore;
using Scout.Properties;
using Scout.Results;
using Scout.Util;
using Scout.Util.AboutWindow;
using ScoutPostProcessing;
using SpectrumWizard;
using SpectrumWizard.Predictors.CleavableXL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Data;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;
using Scout.Util.Updates;
using System.ComponentModel;
using System.Diagnostics;
using Scout.Control;

namespace Scout
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        /// <summary>
        /// Private variables
        /// </summary>
        private TextWriter _writer = null; // That's our custom TextWriter class
        private System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        private double _time = 0;
        private string[] args { get; set; }
        private bool _hasInternetConn { get; set; }

        /// <summary>
        /// Public variables
        /// </summary>
        public static List<CleaveReagent> CleaveReagents { get; set; }
        public static List<Digestor.Enzyme> Enzymes { get; set; }
        public static List<AminoacidMod> VariableMods { get; set; }
        public static List<AminoacidMod> StaticMods { get; set; }

        static void Application_ThreadException(Object sender, ThreadExceptionEventArgs e)
        {
            Console.WriteLine("Application.ThreadException");
        }

        static void AppDomain_UnhandledException(Object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine("AppDomain.UnhandledException");
        }

        public MainWindow(string[] args)
        {
            InitializeComponent();
            this.args = args;

            StartApp();
            ControlFilterGUI.SetControlBasicSearch(ControlSearchGUI);

            #region debug

            #endregion
        }

        private async void StartApp()
        {
            this.Closing += MainWindow_Closing;
            AppDomain.CurrentDomain.UnhandledException += AppException;
            _hasInternetConn = Management.CheckInternetConnection();
            if (_hasInternetConn)
                Management.CheckForUpdates(ScoutCore.Utils.GetAppVersion());
            Management.CheckSystemLanguage();

            ActiveListBoxControl();
            InitializeScoutParametersFromResourceFile();
            InitializeModificationsLibraryFromResourceFile();
            InitializeReagentsLibraryFromResourceFile();
            InitializeEnzymeLibraryFromResourceFile();

            this.args = args;
            this.Loaded += MainWindow_Loaded;
            this.DataContext = new MainWindowCommandContext(this);

            #region Check python
            if (_hasInternetConn)
            {
                var wait_screen = Util.Util.CallWaitWindow("Welcome to Scout", "Checking Python...");
                MainGrid.Children.Add(wait_screen);
                Grid.SetRowSpan(wait_screen, 4);
                Grid.SetRow(wait_screen, 0);
                var rows = MainGrid.RowDefinitions;
                rows[2].Height = new GridLength(2, GridUnitType.Star);

                try
                {
                    await Task.Run(() => CheckInitialParameters());
                }
                catch (Exception e)
                {
                    StringBuilder sb = new();
                    foreach (var item in ControlSearchGUI.TextLog.Items)
                        sb.AppendLine(item.ToString());
                    string msg = String.Join("\n", sb);
                    throw new Exception($"ERROR: Try restarting Scout.\n\nPython output:\n{msg}\nStackTrace:\n{e.Message}\n{e.StackTrace}");
                }

                MainGrid.Children.Remove(wait_screen);
                rows[2].Height = GridLength.Auto;
            }
            #endregion

            TimeSpan dt = TimeSpan.FromSeconds(_time);
            string processing_time = " All rights reserved® - Processing time: " + dt.Days + " Day(s) "
            + dt.Hours + " Hour(s) "
            + dt.Minutes + " Minute(s) "
            + dt.Seconds + " Second(s).";
            AddHyperlink(ProcessingTimeLabel, processing_time);

            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();

            PrintHeader();

        }

        private void PrintHeader()
        {
            ControlSearchGUI.TextLog.Items.Clear();
            String? version = ScoutCore.Utils.GetAppVersion();
            Console.WriteLine("###################################################");
            Console.WriteLine("                                        Scout - v. " + version);
            Console.WriteLine(" Engineered by Clasen et al. - LPEC / Brazil & The Liu Lab / Germany");
            Console.WriteLine("###################################################");
            Console.WriteLine();

            if (!_hasInternetConn)
                Console.WriteLine("WARNING: No internet connection has been detected.\nUnable to check for Scout and Python updates.\nSome features may not work well.");

            this.Title = "Scout (beta version) :: v. " + version;
        }

        [STAThread]
        private void CheckInitialParameters()
        {
            Management.CheckPython();
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            new CloseWindowKey().Execute(null);
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Management.RegisterScout("scout", System.Windows.Forms.Application.ExecutablePath, "Scout.exe");
            if (args != null && args.Length > 0)
            {
                string[] myAppData = args;

                #region Disable fields
                ControlSearchGUI.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { ControlSearchGUI.IsEnabled = false; }));
                ControlFilterGUI.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { ControlFilterGUI.IsEnabled = false; }));
                ControlBasicSearch.SearchStatus = true;

                #endregion

                await runAsync(new string[1] { myAppData[0] });
                ControlFilterGUI.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { PrintHeader(); }));
            }
        }

        private void TestRaws()
        {
            string path = @"D:\OneDrive2\OneDrive\PPI_folder\SyntheticNetwork\Raws";

            var files = Directory.GetFiles(path).Where(a =>
                System.IO.Path.GetExtension(a).Contains("raw")
                ).ToList();
            int ctt = 0;
            foreach (var file in files)
            {
                List<PatternTools.MSParserLight.MSUltraLight>? ms = ScoutCore.FileManagement.SpectrumParser.ParseRaw(file, 2, 10);

                ctt += ms.Count;
            }

        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            TimeSpan dt = TimeSpan.FromSeconds(_time);
            if (ControlBasicSearch.SearchStatus)
            {
                _time++;
                BlockImage.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { BlockImage.Visibility = Visibility.Collapsed; }));
                mainTab.SelectedIndex = 0;
            }
            else
            {
                _time = 0;
                dt = TimeSpan.FromSeconds(_time);
            }

            if (ControlBasicFilter.HasError)
            {
                mainTab.SelectedIndex = 0;
                ControlBasicFilter.HasError = false;
            }

            if (_time > 0)
                BlockImage.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { BlockImage.Visibility = Visibility.Visible; }));
            else
                BlockImage.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { BlockImage.Visibility = Visibility.Collapsed; }));

            string processing_time = " All rights reserved® - Processing time: " + dt.Days + " Day(s) "
            + dt.Hours + " Hour(s) "
            + dt.Minutes + " Minute(s) "
            + dt.Seconds + " Second(s).";
            AddHyperlink(ProcessingTimeLabel, processing_time);


        }
        private void AddHyperlink(TextBlock textBlock, string processing_time)
        {
            textBlock.Inlines.Clear();

            // Create a new Hyperlink
            Hyperlink hyperlink = new Hyperlink();
            hyperlink.Inlines.Add("cite us");
            hyperlink.NavigateUri = new System.Uri("https://doi.org/10.1101/2023.11.30.569448");
            hyperlink.RequestNavigate += Hyperlink_RequestNavigate;
            // Add the Hyperlink to the TextBlock
            textBlock.Inlines.Add(processing_time);
            textBlock.Inlines.Add(" Please ");
            textBlock.Inlines.Add(hyperlink);
            textBlock.Inlines.Add(".");
        }
        // Event handler for when the hyperlink is clicked
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            // Open the link in the default web browser
            try
            {
                System.Diagnostics.Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri.ToString(),
                    UseShellExecute = true,
                });
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show(
                        "Visit the Scout website for more usability information.",
                        "Scout :: Manuscript",
                        (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                        (System.Windows.MessageBoxImage)MessageBoxIcon.Information);
                throw;
            }
        }
        private void ActiveListBoxControl()
        {
            // Instantiate the writer
            _writer = new ListBoxStreamWriter(ControlSearchGUI.TextLog);
            // Redirect the out Console stream

            // Close previous output stream and redirect output to standard output.
            Console.Out.Close();
            Console.SetOut(_writer);
        }

        public static void InitializeReagentsLibraryFromResourceFile()
        {
            try
            {
                if (String.IsNullOrEmpty(Settings.Default.ReagentsJson) == false && !Settings.Default.ReagentsJson.Equals("null"))
                {
                    CleaveReagents = ScoutCore.FileManagement.Serializer.FromJson<List<CleaveReagent>>(Settings.Default.ReagentsJson, true);

                }
                else
                {
                    CleaveReagents = new List<CleaveReagent>() { CleaveReagent.DSSO, CleaveReagent.DSSO_KSYT, CleaveReagent.DSBU, CleaveReagent.DSBU_KSYT, CleaveReagent.DSBSO, CleaveReagent.DSBSO_KSYT, CleaveReagent.DSAU };
                }
            }
            catch
            {

            }
        }

        public static void InitializeEnzymeLibraryFromResourceFile()
        {
            try
            {
                if (String.IsNullOrEmpty(Settings.Default.EnzymesJson) == false && !Settings.Default.EnzymesJson.Equals("null"))
                {
                    Enzymes = ScoutCore.FileManagement.Serializer.FromJson<List<Digestor.Enzyme>>(Settings.Default.EnzymesJson, true);
                }
                else
                {
                    Enzymes = new List<Digestor.Enzyme>() {
                    Digestor.Enzyme.Trypsin,
                    Digestor.Enzyme.TrypsinIgnoreP,
                    Digestor.Enzyme.GluC,
                    Digestor.Enzyme.LysN,
                    Digestor.Enzyme.LysC,
                    Digestor.Enzyme.ArgC,
                    Digestor.Enzyme.Chymotrypsin,
                    Digestor.Enzyme.ChymotrypsinIgnoreP,
                    Digestor.Enzyme.CNBr,
                    Digestor.Enzyme.AspN,
                    Digestor.Enzyme.Thermolysin };
                }
            }
            catch
            {

            }
        }

        public static void InitializeModificationsLibraryFromResourceFile()
        {
            try
            {
                if (String.IsNullOrEmpty(Settings.Default.VariableModsJson) == false && !Settings.Default.VariableModsJson.Equals("null"))
                {
                    VariableMods = ScoutCore.FileManagement.Serializer.FromJson<List<AminoacidMod>>(Settings.Default.VariableModsJson, true);
                }
                else
                {
                    VariableMods = new() { AminoacidMod.MethinineOxidation, AminoacidMod.Acetylation, AminoacidMod.Deamidation_NQ, AminoacidMod.Phosphorylation, AminoacidMod.Ubiquitination };
                }
            }
            catch
            {

            }

            try
            {
                if (String.IsNullOrWhiteSpace(Settings.Default.StaticModsJson) == false && !Settings.Default.StaticModsJson.Equals("null"))
                {
                    StaticMods = ScoutCore.FileManagement.Serializer.FromJson<List<AminoacidMod>>(Settings.Default.StaticModsJson, true);
                }
                else
                {
                    StaticMods = new()
                    {
                        AminoacidMod.CysteinCarbamidomethylation
                    };
                }
            }
            catch
            {

            }
        }

        public static void InitializeScoutParametersFromResourceFile()
        {
            try
            {
                if (String.IsNullOrEmpty(Settings.Default.ScoutParamsJson) == true)
                {
                    ControlBasicSearch.SearchParameters = new ScoutParameters();
                }
                else
                {
                    var scoutParams = ScoutParameters.LoadFromString(Settings.Default.ScoutParamsJson);
                    if (scoutParams == null) { throw new Exception(); }

                    ControlBasicSearch.SearchParameters = scoutParams;
                }
            }
            catch
            {
                ControlBasicSearch.SearchParameters = new ScoutParameters();
                Console.WriteLine(@"Error loading saved Scout Search Parameters. Setting to original default.");
            }


            if (String.IsNullOrWhiteSpace(Settings.Default.PostParamsJson) == true)
            {
                ControlBasicSearch.PostParameters = new PostProcessingParameters();
            }
            else
            {
                try
                {
                    var postParams = PostProcessingParameters.LoadFromString(Settings.Default.PostParamsJson);
                    if (postParams == null) { throw new Exception(); }

                    ControlBasicSearch.PostParameters = postParams;
                }
                catch
                {
                    ControlBasicSearch.PostParameters = new PostProcessingParameters();
                    Console.WriteLine(@"Error loading saved Post Processing Parameters. Setting to original default.");
                }
            }
        }

        private void AppException(object sender, FirstChanceExceptionEventArgs e)
        {
            Exception exception;

            try
            {
                exception = e.Exception as Exception;
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show("Unexpected exception.");
                return;
            }

            OpenExceptionWindow(exception);
        }

        private void AppException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception exception;

            try
            {
                exception = e.ExceptionObject as Exception;
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show("Unexpected exception.");
                return;
            }

            OpenExceptionWindow(exception);
        }

        private void OpenExceptionWindow(Exception exception)
        {

            //this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render,
            //    new Action(delegate ()
            //    {

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            WindowAppException window = new WindowAppException(exception)
            {
                Title = "Scout :: Exception (beta version)",
                Height = 400,
                Width = 450
            };

            window.ShowDialog();
            //}));




        }

        [STAThread]
        async void MenuItemOpenResults_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog()
            {
                Filter = "Scout files|*.scout",
                Multiselect = true
            };

            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            #region Disable fields
            ControlBasicSearch.SearchStatus = true;
            ControlSearchGUI.IsEnabled = false;
            ControlFilterGUI.IsEnabled = false;
            #endregion
            await runAsync(ofd.FileNames);

        }

        [STAThread]
        Task runAsync(string[] fileNames)
        {
            var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            XLFilteredResults? post = null;
            string name_to_show = "";
            return Task.Factory.StartNew(
            () =>
            {
                try
                {
                    post = XLFilteredResults.Load(fileNames);
                    //XLFilteredResults.LoadAsync(fileName);

                    //if (Environment.MachineName.ToLower().Contains("pcn141"))
                    //{
                    //    FileInfo fileInfo = new FileInfo(fileName);
                    //    string path = fileInfo.DirectoryName
                    //        + "/" +
                    //        $"{fileInfo.Directory.Name}_ppi_results.csv";
                    //    //XLFilteredResultsSaver.SavePPIsCSV(
                    //    //    path,
                    //    //    post.PackagePPIs.AllPPIs,
                    //    //    post.PostParams);
                    //    path = fileInfo.DirectoryName
                    //        + "/" +
                    //        $"{fileInfo.Directory.Name}_respairs_results.csv";
                    //    //XLFilteredResultsSaver.SaveResiduePairsCSV(
                    //    //    path,
                    //    //    post.PackageResPairs.AllResPairs,
                    //    //    post.PostParams);
                    //}
                    if (fileNames.Length > 1)
                        name_to_show = "Combined_file";
                    else
                        name_to_show = System.IO.Path.GetFileName(fileNames[0]);

                }
                catch (Exception e)
                {
                    #region Enable fields
                    this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                    new Action(delegate ()
                    {
                        ControlBasicSearch.SearchStatus = false;
                        ControlSearchGUI.IsEnabled = true;
                        ControlFilterGUI.IsEnabled = true;
                    }));
                    #endregion
                    Console.WriteLine("ERROR: It's not possible to load the file.\n" + e.StackTrace + "\n" + e.Message);
                    throw;
                }
            }
            ).ContinueWith(r => HandleFinishedThread(post, name_to_show), scheduler);
            //).ContinueWith(r => HandleFinishedThread(fileName), scheduler);
        }

        //private static void HandleFinishedLoadFile()
        //{

        //    if (XLFilteredResults.AllTasksReadFile == null || XLFilteredResults.AllTasksReadFile.Count == 0) return;

        //    int set_processed = 0;
        //    int old_progress = 0;
        //    int total_split_files = XLFilteredResults.AllTasksReadFile.Count;

        //    List<ScoredCSM> AllCSMs = new List<ScoredCSM>(total_split_files * ScoutCore.FileManagement.Serializer.MAX_ITEMS_TO_BE_SAVED);
        //    List<ScoredCSM> AllCSMsLoop = new List<ScoredCSM>(total_split_files * ScoutCore.FileManagement.Serializer.MAX_ITEMS_TO_BE_SAVED);
        //    List<ResPair> AllResPairs = new List<ResPair>();
        //    List<PPI> AllPPIs = new List<PPI>();

        //    Console.Write("Loading file: 50%");
        //    foreach (var task in XLFilteredResults.AllTasksReadFile)
        //    {
        //        var toDeserialize = (XLFilteredResults)task.Result;
        //        if (toDeserialize != null)
        //        {
        //            if (toDeserialize.PackageCSMs.AllCSMs != null)
        //                AllCSMs.AddRange(toDeserialize.PackageCSMs.AllCSMs);
        //            if (toDeserialize.PackageCSMsLoopLinks.AllCSMs != null)
        //                AllCSMsLoop.AddRange(toDeserialize.PackageCSMsLoopLinks.AllCSMs);
        //            if (toDeserialize.PackageResPairs.AllResPairs != null)
        //                AllResPairs.AddRange(toDeserialize.PackageResPairs.AllResPairs);
        //            if (toDeserialize.PackagePPIs.AllPPIs != null)
        //                AllPPIs.AddRange(toDeserialize.PackagePPIs.AllPPIs);
        //        }
        //        #region progress bar
        //        set_processed++;
        //        int new_progress = (((int)((double)set_processed / (total_split_files) * 100)) / 2) + 50;
        //        if (new_progress > old_progress)
        //        {
        //            old_progress = new_progress;
        //            Console.Write("Loading file: " + old_progress + "%");
        //        }
        //        #endregion
        //    }

        //    XLFilteredResults.Results.PackageCSMs = new CSMPackage(AllCSMs, XLFilteredResults.Results.PostParams.FDRMode, XLFilteredResults.Results.PostParams.CSM_FDR);
        //    XLFilteredResults.Results.PackageCSMsLoopLinks = new CSMPackage(AllCSMsLoop, XLFilteredResults.Results.PostParams.FDRMode, XLFilteredResults.Results.PostParams.CSM_FDR);
        //    XLFilteredResults.Results.PackageResPairs = new ResiduePairPackage(AllResPairs, XLFilteredResults.Results.PostParams.FDRMode, XLFilteredResults.Results.PostParams.ResPair_FDR);
        //    XLFilteredResults.Results.PackagePPIs = new PPIPackage(AllPPIs, XLFilteredResults.Results.PostParams.FDRMode, XLFilteredResults.Results.PostParams.PPI_FDR);


        //}

        private void HandleFinishedThread(XLFilteredResults? post, string fileName)
        //private void HandleFinishedThread(string fileName)
        {
            if (post != null)
            //if (XLFilteredResults.Results != null)
            {
                //HandleFinishedLoadFile();

                #region Enable fields
                ControlBasicSearch.SearchStatus = false;
                ControlSearchGUI.IsEnabled = true;
                ControlFilterGUI.IsEnabled = true;
                #endregion

                var w = new WindowResultsBrowser(post, fileName);
                //var w = new WindowResultsBrowser(XLFilteredResults.Results, fileName);
                w.ShowDialog();
            }
        }

        private void MenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            new CloseWindowKey().Execute(sender);
        }

        private void MenuItem_SearchParams(object sender, RoutedEventArgs e)
        {
            new SearchParamsKey().Execute(sender);
        }

        private void MenuItem_PostParams(object sender, RoutedEventArgs e)
        {
            new PostParamsKey().Execute(sender);
        }

        private void MenuItem_About(object sender, RoutedEventArgs e)
        {
            AboutWindow info = new AboutWindow();
            info.ShowDialog();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.B && Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftShift))
            {
                Properties.Settings.Default.DebugMode = !Properties.Settings.Default.DebugMode;
                //Properties.Settings.Default.Save();

                if (Properties.Settings.Default.DebugMode)
                {
                    MenuItem_SearchParamsDebug.Visibility = Visibility.Visible;
                    MenuItem_PostParamsDebug.Visibility = Visibility.Visible;
                }
                else
                {
                    MenuItem_SearchParamsDebug.Visibility = Visibility.Collapsed;
                    MenuItem_PostParamsDebug.Visibility = Visibility.Collapsed;
                }

                if (Properties.Settings.Default.DebugMode == true)
                    System.Windows.MessageBox.Show("Debug Mode Activated! Deactivate it by pressing Ctrl + Shift + B");
                else
                    System.Windows.MessageBox.Show("Debug Mode Deactivated! Re-activate it by pressing Ctrl + Shift + B");
            }
        }

        private void CommandBindingOpen_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CommandBindingOpen_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MenuItemOpenResults_Click(sender, null);
        }

        private void CommandBindingClose_CanExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            MenuItemExit_Click(sender, null);
        }

        private void MenuItem_ReadMe(object sender, RoutedEventArgs e)
        {
            new ReadMeKey().Execute(sender);
        }

        private void MenuItemExportLogFile_Click(object sender, RoutedEventArgs e)
        {
            new ExportLogKey(this).Execute(sender);
        }

        private void MenuItem_CheckForUpdates(object sender, RoutedEventArgs e)
        {
            Update winUpdate = new();
            winUpdate.Load();
            winUpdate.ShowDialog();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            System.Environment.Exit(1);
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
                        "Scout :: Discussion Forum",
                        (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                        (System.Windows.MessageBoxImage)MessageBoxIcon.Information);
                throw;
            }
        }

        private void MenuItem_SearchParams_DebugMode(object sender, RoutedEventArgs e)
        {
            Xceed.Wpf.Toolkit.PropertyGrid.PropertyGrid pg = new();

            pg.SelectedObject = ControlBasicSearch.SearchParameters;
            System.Windows.Window w = new()
            {
                Content = pg
            };
            w.ShowDialog();
        }

        private void MenuItem_PostParams_DebugMode(object sender, RoutedEventArgs e)
        {
            Xceed.Wpf.Toolkit.PropertyGrid.PropertyGrid pg = new();

            pg.SelectedObject = ControlBasicSearch.PostParameters;
            System.Windows.Window w = new()
            {
                Content = pg
            };
            w.ShowDialog();


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

    internal class SearchParamsKey : ICommand
    {
        /// <summary>
        /// Public variables
        /// </summary>
        public event EventHandler CanExecuteChanged;
        public List<CleaveReagent> CleaveReagents { get; private set; }
        public List<AminoacidMod> VariableMods { get; private set; }
        public List<AminoacidMod> StaticMods { get; private set; }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        private Parameters.WindowScoutParametersEdit OpenWindow()
        {
            Parameters.WindowScoutParametersEdit w = new Parameters.WindowScoutParametersEdit(
                ControlBasicSearch.SearchParameters,
                MainWindow.CleaveReagents,
                MainWindow.Enzymes,
                MainWindow.VariableMods,
                MainWindow.StaticMods
                );

            w.ShowDialog();

            return w;
        }


        public void Execute(object parameter)
        {

            var w = OpenWindow();

            if (w.AcceptedChanges)
            {
                ControlBasicSearch.SearchParameters = w.ScoutParams;


                Settings.Default.ReagentsJson = ScoutCore.FileManagement.Serializer.ToJSON(MainWindow.CleaveReagents);
                Settings.Default.EnzymesJson = ScoutCore.FileManagement.Serializer.ToJSON(MainWindow.Enzymes);
                Settings.Default.VariableModsJson = ScoutCore.FileManagement.Serializer.ToJSON(MainWindow.VariableMods);
                Settings.Default.StaticModsJson = ScoutCore.FileManagement.Serializer.ToJSON(MainWindow.StaticMods);

                Settings.Default.Save();

            }
            else if (w.IsRestore)
            {
                MainWindow.InitializeScoutParametersFromResourceFile();
                MainWindow.InitializeReagentsLibraryFromResourceFile();
                MainWindow.InitializeEnzymeLibraryFromResourceFile();
                MainWindow.InitializeModificationsLibraryFromResourceFile();
            }
        }
    }

    internal class PostParamsKey : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var w = new WindowPostParams(ControlBasicSearch.PostParameters);

            w.ShowDialog();

            if (w.Accepted)
            {
                ControlBasicSearch.PostParameters = w.PostParams;
            }
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

    internal class ExportLogKey : ICommand
    {
        public event EventHandler CanExecuteChanged;
        private MainWindow _mainWindow;

        public ExportLogKey(MainWindow mainWindow)
        {
            this._mainWindow = mainWindow;
        }
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var sfd = new SaveFileDialog()
            {
                Title = "Export log",
                Filter = "Log|*.csv",
                FileName = "Scout_log.csv"
            };

            if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            try
            {
                SaveLog(sfd.FileName, _mainWindow);
                System.Windows.Forms.MessageBox.Show("The log has been exported successfully!", "Information", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            }
            catch (Exception exc)
            {
                Console.WriteLine("ERROR: " + exc.Message);
                System.Windows.Forms.MessageBox.Show("Failed to save!", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
            }
        }

        private void SaveLog(string savePath, MainWindow _mainWindow)
        {
            try
            {
                var log = GetLogInfo(_mainWindow);
                File.WriteAllText(savePath, log.ToString());
                Console.WriteLine($"Log exported to {savePath}!");
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: Could not save log. " + e.Message);
                throw;
            }
        }

        private StringBuilder GetLogInfo(MainWindow _mainWindow)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var drv in _mainWindow.ControlSearchGUI.TextLog.Items)
            {
                var line = drv.ToString();
                sb.AppendLine(line);
            }
            return sb;
        }
    }

    public class MainWindowCommandContext
    {
        private MainWindow _mainWindow;

        public MainWindowCommandContext(MainWindow mainWindow)
        {
            this._mainWindow = mainWindow;
        }
        public ICommand CloseWindowCommand
        {
            get
            {
                return new CloseWindowKey();
            }
        }

        public ICommand SearchParamsCommand
        {
            get
            {
                return new SearchParamsKey();
            }
        }

        public ICommand PostParamsCommand
        {
            get
            {
                return new PostParamsKey();
            }
        }

        public ICommand ReadMeCommand
        {
            get
            {
                return new ReadMeKey();
            }
        }

        public ICommand ExportLogCommand
        {
            get
            {
                return new ExportLogKey(_mainWindow);
            }
        }
    }
}
