using System;
using System.Text;
using System.Linq;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace UpIpsecVPN
{
    class Program
    {

        #region  DLL import for Hidden Window
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("Kernel32")]
        private static extern IntPtr GetConsoleWindow();

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;
        #endregion

        static void Main(string[] args) {

            #region Anable Hidden windows
            IntPtr hwnd;
            hwnd = GetConsoleWindow();
            ShowWindow(hwnd, SW_HIDE);
            #endregion


            while (true) {


                // 1. If connection is good  sleep 20 sec.
                while (ConnectionISGood("10.127.255.33"))
                    System.Threading.Thread.Sleep(20000);

                if (ConnectionISGood("ipsecvpn.omsu.vmr")) {
                    if (IsInterfaceUP("Ipsec VPN"))
                        IpsecVPNIntUP(false);
                    IpsecVPNIntUP(true);
                }
            }

        }

        public static bool ConnectionISGood(string host) {              //host is ip or dns name

            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();

            options.DontFragment = false;

            // Create a buffer of 32 bytes of data to be transmitted.
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            int timeout = 120;
            try {

                PingReply reply = pingSender.Send(host, timeout, buffer, options);
                if (reply.Status == IPStatus.Success)
                    return true;
                else
                    return false;
            }
            catch {
                return false;
            }

        }

        public static void IpsecVPNIntUP(bool up) {

            if (up) {
                Process.Start(new ProcessStartInfo { FileName = "rasdial", Arguments = "\"Ipsec VPN\"", WindowStyle = ProcessWindowStyle.Hidden }).WaitForExit();
            } else {
                Process.Start(new ProcessStartInfo { FileName = "rasdial", Arguments = "\"Ipsec VPN\" /DISCONNECT", WindowStyle = ProcessWindowStyle.Hidden }).WaitForExit();
            }
        }


        public static bool IsInterfaceUP(string nameInterface) {

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            NetworkInterface adapter = adapters.Where(a => a.Name == nameInterface).Where(a => a.OperationalStatus == OperationalStatus.Up).FirstOrDefault();
            if (adapter == null)
                return false;
            else
                return true;
        }

    }
}
