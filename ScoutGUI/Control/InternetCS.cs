using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Scout.Control
{
    public class InternetCS
    {
        [DllImport("wininet.dll", SetLastError = true)]
        static extern bool InternetCheckConnection(string lpszUrl, int dwFlags, int dwReserved);
        public static bool CanConnectToURL(string url = "http://google.com")
        {
            return InternetCheckConnection(url, 1, 0);
        }
    }
}

