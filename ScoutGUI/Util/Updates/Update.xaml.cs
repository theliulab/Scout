using CheckForUpdates.Core;
using CheckForUpdates.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;

namespace Scout.Util.Updates
{
    /// <summary>
    /// Interaction logic for Update.xaml
    /// </summary>
    public partial class Update : Window
    {
        private string current_version { get; set; }
        private ReleaseInfo stable_release { get; set; }
        private string download_file { get; set; }
        public Update()
        {
            InitializeComponent();

        }

        public void Load()
        {
            Load_info();

            (List<ReleaseInfo> new_releases, List<ReleaseInfo> history_releases) = GetReleaseInfo();
            FillInfo(new_releases, history_releases);
        }

        private void Load_info()
        {
            try
            {
                current_version = ScoutCore.Utils.GetAppVersion();
                ScoutVersion.Text = "Version: " + current_version;
            }
            catch (Exception exception)
            {
                //Unable to retrieve version number
                Console.WriteLine("", exception.Message);
            }
        }

        public ReadRelease Connect(string _version = "")
        {
            if (!String.IsNullOrEmpty(_version))
                current_version = _version;
            return new ReadRelease(current_version);
        }

        private (List<ReleaseInfo> new_releases, List<ReleaseInfo> history_releases) GetReleaseInfo()
        {
            var info = Connect();
            List<ReleaseInfo> new_releases = info.new_releasess();
            List<ReleaseInfo> history_releases = info.history_releases();
            return (new_releases, history_releases);
        }

        private void FillInfo(List<ReleaseInfo> new_releases, List<ReleaseInfo> history_releases)
        {
            if (new_releases != null)
            {
                stable_release = new_releases.FirstOrDefault(x => x.is_stable);
                if (stable_release != null)
                {

                    LatestTextLog.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                    {
                        foreach (var item in Regex.Split(friendly_ver_string(stable_release).ToString(), "\r\n"))
                        {
                            LatestTextLog.Items.Add(item);
                        }
                    }));
                    DownloadLatestVersion.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { DownloadLatestVersion.Visibility = Visibility.Visible; }));
                }
                else
                {
                    LatestTextLog.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { LatestTextLog.Items.Add("Congratulations! You have the latest stable version."); }));
                    DownloadLatestVersion.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { DownloadLatestVersion.Visibility = Visibility.Collapsed; }));
                }
            }

            if (history_releases != null)
            {
                HistoryTextLog.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                {
                    foreach (var item in Regex.Split(concatenate(history_releases.Select(friendly_ver_string), "\r\n").ToString(), "\r\n"))
                    {
                        HistoryTextLog.Items.Add(item);
                    }
                }));
            }
        }

        private StringBuilder friendly_ver_string(ReleaseInfo version)
        {
            StringBuilder sb = new();
            sb.Append("[" + version.version + "] ");
            sb.Append(version.short_description + " ");

            if (!version.is_stable && !version.is_beta)
                // not beta, nor stable
                sb.Append("(in development)");

            sb.AppendLine();
            sb.Append(concatenate(version.features.Select(x => "* " + x), "\r\n"));
            return sb;
        }

        private static StringBuilder concatenate<T>(IEnumerable<T> vals, string between)
        {
            StringBuilder sb = new();
            foreach (var val in vals)
            {
                if (sb.Length > 0)
                    sb.Append(between);
                sb.Append(val);
            }
            return sb;
        }

        private void HyperlinkDownload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (stable_release != null)
                {
                    DownloadProgressBar.Visibility = Visibility.Visible;
                    download_file = Util.KnownFolders.GetPath(Util.KnownFolder.Downloads) + "\\Scout_" + stable_release.version.Replace('.', '_') + "_64bit.msi";
                    WebClient webClient = new WebClient();
                    webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
                    webClient.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(WebClient_DownloadFileCompleted);
                    webClient.DownloadFileAsync(new Uri(stable_release.download_64bit_url), download_file);
                }
            }
            catch { }
        }

        private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            DownloadProgressBar.Value = e.ProgressPercentage;
        }

        private void WebClient_DownloadFileCompleted(object? sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            System.Windows.MessageBox.Show(
                        "Setup file has been downloaded successfully at\n\n'" +
                        download_file +
                        "'\n\nScout will be closed and the setup file will be started.",
                        "Scout :: Check for updates",
                        (System.Windows.MessageBoxButton)MessageBoxButtons.OK,
                        (System.Windows.MessageBoxImage)MessageBoxIcon.Information);

            DownloadProgressBar.Visibility = Visibility.Collapsed;

            Process proc = new Process();
            proc.StartInfo.FileName = "msiexec";
            proc.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(download_file);
            proc.StartInfo.Arguments = " /i " + System.IO.Path.GetFileName(download_file);
            proc.StartInfo.Verb = "runas";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;

            proc.Start();

            System.Environment.Exit(1);
        }
    }

}
