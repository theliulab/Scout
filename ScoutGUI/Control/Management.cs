using Microsoft.Win32;
using Scout.Util.Updates;
using ScoutPostProcessing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Scout.Control
{
    public static class Management
    {
        public static void CheckSystemLanguage()
        {
            #region Setting Language
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (!Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\International", "LocaleName", null).ToString().ToLower().Equals("en-us"))
                {
                    DialogResult answer = System.Windows.Forms.MessageBox.Show("The system default language is not English. Do you want to change it to English ?\nThis tool works if only the system default language is English.", "Scout :: Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (answer == System.Windows.Forms.DialogResult.Yes)
                    {
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "Locale", "00000409");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "LocaleName", "en-US");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sCountry", "United States");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sCurrency", "$");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sDate", "/");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sDecimal", ".");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sGrouping", "3;0");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sLanguage", "ENU");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sList", ",");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sLongDate", "dddd, MMMM dd, yyyy");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sMonDecimalSep", ".");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sMonGrouping", "3;0");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sMonThousandSep", ",");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sNativeDigits", "0123456789");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sNegativeSign", "-");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sPositiveSign", "");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sShortDate", "M/d/yyyy");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sThousand", ",");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sTime", ":");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sTimeFormat", "h:mm:ss tt");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sShortTime", "h:mm tt");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sYearMonth", "MMMM, yyyy");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iCalendarType", "1");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iCountry", "1");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iCurrDigits", "2");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iCurrency", "0");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iDate", "0");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iDigits", "2");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "NumShape", "1");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iFirstDayOfWeek", "6");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iFirstWeekOfYear", "0");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iLZero", "1");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iMeasure", "1");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iNegCurr", "0");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iNegNumber", "1");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iPaperSize", "1");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iTime", "0");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iTimePrefix", "0");
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\International", "iTLZero", "0");
                        System.Windows.Forms.MessageBox.Show("Scout will be restarted!", "Scout :: Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        System.Windows.Forms.Application.Restart();
                        System.Windows.Application.Current.Shutdown();
                    }
                    else
                    {
                        System.Windows.Forms.MessageBox.Show("Scout will be closed!", "Scout :: ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        System.Environment.Exit(0);
                        System.Windows.Forms.Application.Exit();
                    }
                }
            }
            #endregion
        }

        public static bool CheckInternetConnection()
        {
            if (!InternetCS.CanConnectToURL())
                return false;
            return true;
        }
        public static void CheckForUpdates(string current_version)
        {
            Update update = new();
            var info = update.Connect(current_version);

            if (info.HasNewRelease())
            {
                var r = System.Windows.Forms.MessageBox.Show(
                    "Scout current version is not the latest release.\nDo you want to update it ?",
                    "Scout :: Check for updates",
                    System.Windows.Forms.MessageBoxButtons.YesNo,
                    System.Windows.Forms.MessageBoxIcon.Warning);

                if (r != System.Windows.Forms.DialogResult.Yes) { return; }

                update.Load();
                update.ShowDialog();

            }
        }

        public static void CheckPython()
        {
            if (!PythonScout.VerifyPython())
            {
                System.Windows.Forms.MessageBox.Show(
                    "Python or dependecies have not been detected. Please check them and start Scout later. The software will be closed!",
                    "Scout :: Check dependencies",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);

                System.Environment.Exit(0);
                System.Windows.Forms.Application.Exit();
            }
        }

        public static void RegisterScout(string Extension, string OpenWith, string ExecutableName)
        {
            try
            {
                using (RegistryKey? User_Classes = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Classes\\", true))
                using (RegistryKey? User_Ext = User_Classes?.CreateSubKey("." + Extension))
                using (RegistryKey? User_AutoFile = User_Classes?.CreateSubKey(Extension + "_auto_file"))
                using (RegistryKey? User_AutoFile_Command = User_AutoFile?.CreateSubKey("shell").CreateSubKey("open").CreateSubKey("command"))
                using (RegistryKey? ApplicationAssociationToasts = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\ApplicationAssociationToasts\\", true))
                using (RegistryKey? User_Classes_Applications = User_Classes?.CreateSubKey("Applications"))
                using (RegistryKey? User_Classes_Applications_Exe = User_Classes_Applications?.CreateSubKey(ExecutableName))
                using (RegistryKey? User_Application_Command = User_Classes_Applications_Exe?.CreateSubKey("shell").CreateSubKey("open").CreateSubKey("command"))
                using (RegistryKey? User_Explorer = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FileExts\\." + Extension))
                using (RegistryKey? User_Choice = User_Explorer.OpenSubKey("UserChoice"))
                {
                    string scout_logo_path = System.AppDomain.CurrentDomain.BaseDirectory + "imgs\\scout_logo.ico";
                    User_Ext?.CreateSubKey("DefaultIcon").SetValue("", scout_logo_path);
                    RegistryKey? UserClassesExt = User_Classes?.CreateSubKey(Extension);
                    UserClassesExt?.SetValue("", "Scout Results");
                    User_AutoFile_Command?.SetValue("", "\"" + OpenWith + "\"" + " \"%1\"");
                    ApplicationAssociationToasts?.SetValue("Scout_." + Extension, 0);
                    ApplicationAssociationToasts?.SetValue(@"Applications\" + ExecutableName + "_." + Extension, 0);
                    User_Classes_Applications_Exe?.CreateSubKey("DefaultIcon").SetValue("", scout_logo_path);
                    User_Application_Command?.SetValue("", "\"" + OpenWith + "\"" + " \"%1\"");
                    User_Explorer.CreateSubKey("OpenWithList").SetValue("a", ExecutableName);
                    User_Explorer.CreateSubKey("OpenWithProgids").SetValue(Extension + "_auto_file", "0");
                    if (User_Choice != null) User_Explorer.DeleteSubKey("UserChoice");
                    User_Explorer.CreateSubKey("UserChoice").SetValue("ProgId", @"Applications\" + ExecutableName);
                }
                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception) { }
        }
        [DllImport("Shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
    }
}
