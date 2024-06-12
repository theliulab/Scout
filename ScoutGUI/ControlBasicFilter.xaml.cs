using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Scout.Results;
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
using System.Windows.Shapes;

namespace Scout
{
    /// <summary>
    /// Interaction logic for ControlBasicFilter.xaml
    /// </summary>
    public partial class ControlBasicFilter : UserControl
    {
        public static bool HasError { get; set; }

        private string BufFiles { get; set; }
        private string RawFiles { get; set; }
        private string FastaFile { get; set; }
        private ControlBasicSearch _ControlBasicSearch { get; set; }

        public ControlBasicFilter()
        {
            InitializeComponent();
            LoadLog();

            ScoutVersion.Text = "Scout - v. " + ScoutCore.Utils.GetAppVersion();
        }

        public void SetControlBasicSearch(ControlBasicSearch controlBasicSearch)
        {
            _ControlBasicSearch = controlBasicSearch;
        }

        private void LoadLog()
        {
            String? version = ScoutCore.Utils.GetAppVersion();

            TextLog.Items.Add("###################################################");
            TextLog.Items.Add("                                        Scout - v. " + version);
            TextLog.Items.Add(" Engineered by Clasen et al. - LPEC / Brazil & The Liu Lab / Germany");
            TextLog.Items.Add("###################################################");
        }

        private void ButtonBrowseFasta_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog()
            {
                Filter = "Fasta file|*.fasta"
            };

            if (ofd.ShowDialog() != true) return;

            TextFasta.Text = ofd.FileName;
        }

        private void ButtonBrowseBuf_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new CommonOpenFileDialog()
            {
                IsFolderPicker = true
            };

            if (ofd.ShowDialog() != CommonFileDialogResult.Ok) return;

            TextBufDir.Text = ofd.FileName;
        }

        private void ButtonBrowseRaw_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new CommonOpenFileDialog()
            {
                IsFolderPicker = true
            };

            if (ofd.ShowDialog() != CommonFileDialogResult.Ok) return;

            //TextRawDir.Text = ofd.FileName;
        }

        async void ButtonFilter_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(TextFasta.Text))
            {
                HasError = true;
                Console.WriteLine("ERROR: 'Filter tab' => No database file has been found. Please select one.");
                return;
            }

            if (String.IsNullOrEmpty(TextBufDir.Text))
            {
                HasError = true;
                Console.WriteLine("ERROR: 'Filter tab' => 'Results folder' is empty. Please set a directory where the buf files are.");
                return;
            }

            //if (String.IsNullOrEmpty(TextRawDir.Text))
            //    throw new Exception("No RAW dir has been defined.");


            BufFiles = TextBufDir.Text;
            FastaFile = TextFasta.Text;
            //RawFiles = TextRawDir.Text;

            if (ControlBasicSearch.mainProgramGUI != null)
                ControlBasicSearch.mainProgramGUI = new Program();
            ControlBasicSearch.setInitialParams();

            ControlBasicSearch.mainProgramGUI.SearchParameters.FastaFile = TextFasta.Text;

            ControlBasicSearch.SearchStatus = true;//Start search
            #region disable fields
            TextFasta.IsEnabled = false;
            TextBufDir.IsEnabled = false;
            //TextRawDir.IsEnabled = false;

            _ControlBasicSearch.ChangeStartButtonLabel(1);
            #endregion

            if (ControlBasicSearch._tokenSource == null)
                ControlBasicSearch._tokenSource = new CancellationTokenSource();

            ControlBasicSearch._tokenSource = new CancellationTokenSource();
            var token = ControlBasicSearch._tokenSource.Token;
            try
            {
                await runAsync(token);
            }
            catch (Exception)
            {
                ControlBasicSearch.SearchStatus = false;//Search has been finished.
                Console.WriteLine("WARN: Task has been cancelled!");
            }
            finally
            {
                ControlBasicSearch._tokenSource.Dispose();
            }
            ControlBasicSearch.SearchStatus = false;//Search has been finished.

        }

        Task runAsync(CancellationToken token)
        {
            var scheduler = TaskScheduler.FromCurrentSynchronizationContext();

            return Task.Factory.StartNew(
            () =>
            {
                ControlBasicSearch.PostParameters.PrintToConsole();
                DirectoryInfo folder = new DirectoryInfo(BufFiles);
                List<FileInfo> packs = folder.GetFiles("*.buf", SearchOption.AllDirectories).ToList();

                string _warningMsg = "";

                ControlBasicSearch.mainProgramGUI.PostProcessing(
                    token,
                    FastaFile,
                    out _warningMsg,
                    packs.ToArray());

                ControlBasicSearch.mainProgramGUI.WarningMSG = _warningMsg;
            }
            ).ContinueWith(r => HandleFinishedThread(), scheduler);
        }

        private void HandleFinishedThread()
        {
            #region enable fields
            TextFasta.IsEnabled = true;
            TextBufDir.IsEnabled = true;
            //TextRawDir.IsEnabled = true;
            #endregion
            if (ControlBasicSearch.mainProgramGUI.PostResults != null)
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

                string[] rawFolder = BufFiles.Split(System.IO.Path.DirectorySeparatorChar);
                string fileName = rawFolder[rawFolder.Length - 1] + ".scout";

                WindowResultsBrowser w = new WindowResultsBrowser(ControlBasicSearch.mainProgramGUI.PostResults, fileName);
                w.Show();
            }
            else
                Console.WriteLine("WARNING: There are no results to display.");

            _ControlBasicSearch.ChangeStartButtonLabel(0);
        }
    }
}
