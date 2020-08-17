using Microsoft.Win32;
using System.Diagnostics;

namespace MuteMyMic
{
    public static class StartupManager
    {
        private static RegistryKey StartupPrograms { get => 
                Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true); }
        public static bool WillRunOnStartup 
        {
            get
            {
                return StartupPrograms.GetValue("MuteMyMic") != null;
            }
                
            set
            {
                if (value)
                    StartupPrograms.SetValue("MuteMyMic", Process.GetCurrentProcess().MainModule.FileName);
                else
                    StartupPrograms.DeleteValue("MuteMyMic", false);
            }
        }
    }
}
