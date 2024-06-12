using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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

namespace Scout.Util.AboutWindow
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            load_info();
        }

        private void load_info()
        {
            string citation = "All rights reserved®2024.";
            AddHyperlink(CitationText, citation);

            string platform = System.Environment.OSVersion.Platform.ToString();
            if (platform.Contains("Win"))
            {
                osNameLabel.Text = Util.GetWindowsVersion();
            }
            else
            {
                osNameLabel.Text = "Unix";
            }

            processorNameLabel.Text = Util.GetProcessorName();
            RAMmemoryLabel.Text = Util.GetRAMMemory();

            string version = System.Environment.OSVersion.ServicePack.ToString();
            if (!version.Equals(""))
            {
                if (Util.Is64Bits())
                    versionOSLabel.Text = version + " (64 bits)";
                else
                    versionOSLabel.Text = version + " (32 bits)";
            }
            else
            {
                if (Util.Is64Bits())
                    versionOSLabel.Text = "64 bits";
                else
                    versionOSLabel.Text = "32 bits";
            }
            usrLabel.Text = System.Environment.UserName.ToString();
            machineNameLabel.Text = System.Environment.MachineName.ToString();

            ScoutVersion.Text = "Version: " + ScoutCore.Utils.GetAppVersion();

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
    }
}
