using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.Text.RegularExpressions;
using System.Reflection;
using PatternTools;
using System.Windows.Controls;
using System.Windows;
using Scout.Util.WaitProcess;

namespace Scout.Util
{
    public class Util
    {
        /// <summary>
        /// public variable
        /// </summary>

        [DllImport("kernel32.dll", SetLastError = false)]
        static extern bool GetProductInfo(
             int dwOSMajorVersion,
             int dwOSMinorVersion,
             int dwSpMajorVersion,
             int dwSpMinorVersion,
             out int pdwReturnedProductType);
        public Util() { }

        public enum WindowsVersion
        {
            None = 0,
            Windows_1_01,
            Windows_2_03,
            Windows_2_10,
            Windows_2_11,
            Windows_3_0,
            Windows_for_Workgroups_3_1,
            Windows_for_Workgroups_3_11,
            Windows_3_2,
            Windows_NT_3_5,
            Windows_NT_3_51,
            Windows_95,
            Windows_NT_4_0,
            Windows_98,
            Windows_98_SE,
            Windows_2000,
            Windows_Me,
            Windows_XP,
            Windows_Server_2003,
            Windows_Vista,
            Windows_Home_Server,
            Windows_7,
            Windows_2008_R2,
            Windows_8,
        }

        public static bool Is64Bits()
        {
            return Environment.Is64BitOperatingSystem;
        }

        public static string GetWindowsVersion()
        {
            string platform = System.Environment.OSVersion.Platform.ToString();
            if (platform.ToLower().Contains("nt"))
            {
                platform = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName", null).ToString();
            }
            else
            {
                platform = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion", "ProductName", null).ToString();
            }
            return platform;
        }

        public static string GetProcessorName()
        {
            string processorName = "?";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_Processor");
            try
            {
                foreach (ManagementObject share in searcher.Get())
                {
                    processorName = share["Name"].ToString();
                    break;
                }
            }
            catch (Exception) { }

            return processorName;
        }

        public static string GetRAMMemory()
        {
            string memory = "?";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_OperatingSystem");
            try
            {
                foreach (ManagementObject share in searcher.Get())
                {
                    double memoryInBytes = Convert.ToDouble(share["TotalVisibleMemorySize"].ToString());
                    memoryInBytes /= (1024 * 1024);
                    memory = Math.Round(memoryInBytes, 2).ToString() + " GB";
                    break;
                }
            }
            catch (Exception) { }

            return memory;
        }

        public static string CleanPeptide(string peptide, bool removeParenthesis)
        {
            string input = Regex.Replace(peptide, "^[A-Z|\\-|\\*]+\\.", "");
            input = Regex.Replace(input, "\\.[A-Z|\\-|\\*]+$", "");
            input = input.Replace("*", "");
            input = input.Replace("#", "");
            if (removeParenthesis)
            {
                input = Regex.Replace(input, "\\([0-9|\\.|\\+|\\-| |a-z|A-Z]*\\)", "");
            }
            return input;
        }

        public static UCWaitScreen CallWaitWindow(string primaryMsg, string secondaryMg)
        {
            UCWaitScreen waitScreen = new UCWaitScreen(primaryMsg, secondaryMg);
            Grid.SetRow(waitScreen, 0);
            Grid.SetRowSpan(waitScreen, 2);
            waitScreen.Margin = new Thickness(0, 0, 0, 0);
            return waitScreen;
        }

        public enum KnownFolder
        {
            Contacts,
            Downloads,
            Favorites,
            Links,
            SavedGames,
            SavedSearches
        }

        public static class KnownFolders
        {
            private static readonly Dictionary<KnownFolder, Guid> _guids = new()
            {
                [KnownFolder.Contacts] = new("56784854-C6CB-462B-8169-88E350ACB882"),
                [KnownFolder.Downloads] = new("374DE290-123F-4565-9164-39C4925E467B"),
                [KnownFolder.Favorites] = new("1777F761-68AD-4D8A-87BD-30B759FA33DD"),
                [KnownFolder.Links] = new("BFB9D5E0-C6A9-404C-B2B2-AE6DB6AF4968"),
                [KnownFolder.SavedGames] = new("4C5C32FF-BB9D-43B0-B5B4-2D72E54EAAA4"),
                [KnownFolder.SavedSearches] = new("7D1D3A04-DEBB-4115-95CF-2F29DA2920DA")
            };

            public static string GetPath(KnownFolder knownFolder)
            {
                return SHGetKnownFolderPath(_guids[knownFolder], 0);
            }

            [DllImport("shell32",
                CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
            private static extern string SHGetKnownFolderPath(
                [MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags,
                nint hToken = 0);
        }

    }
}
