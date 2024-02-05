using System;
using System.Text;
using System.Linq;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Net;
using UPIpsecVPN;
using System.Collections.Generic;

namespace UpIpsecVPN
{
    class Program
    {

        //#region  DLL import for Hidden Window
        ////[DllImport("user32.dll")]
        ////static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        ////[DllImport("Kernel32")]
        ////private static extern IntPtr GetConsoleWindow();

        ////const int SW_HIDE = 0;
        ////const int SW_SHOW = 5;
        //#endregion

        static void Main(string[] args) {

            //#region Anable Hidden windows
            //IntPtr hwnd;
            //hwnd = GetConsoleWindow();
            //ShowWindow(hwnd, SW_HIDE);
            //#endregion


            NetworkAdapter ipsecvpnadapter = new NetworkAdapter("Ipsec VPN");

            //bool IntIpsecVPNisUP = ipsecvpnadapter.IsInterfaceUP();


            while (true) {
                
                while (ConnectionIpsecVPNIsGood(ipsecvpnadapter, "10.127.255.33")) {
#if DEBUG
                    Console.WriteLine("Ipsec VPN is good. Sleep 20 sec.");
                    Console.WriteLine($"IP address {ipsecvpnadapter.ShowIPv4Address()[0]}");
                    //IntIpsecVPNisUP = true;
#endif
                    System.Threading.Thread.Sleep(20000);                    
                }

                //if (IntIpsecVPNisUP) {
                //    Console.WriteLine("Ipsec VPN is bad. IpsecVPN interface is UP - shutdown IpsecVPN interface");
                //    IpsecVPNIntUP(false);
                //    IntIpsecVPNisUP = ipsecvpnadapter.IsInterfaceUP();
                //}                

                if (ConnectionISGood("ipsecvpn.omsu.vmr")) {
                    if (IsInterfaceUP("Ipsec VPN"))
                        IpsecVPNIntUP(false);
                    IpsecVPNIntUP(true);
                    //IntIpsecVPNisUP = ipsecvpnadapter.IsInterfaceUP();                         
                }

                System.Threading.Thread.Sleep(20000);
            }

        }


        public static PingReplyExt Send(IPAddress srcAddress, IPAddress destAddress, int timeout = 5000, byte[] buffer = null, PingOptions po = null) {
            if (destAddress == null || destAddress.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork || destAddress.Equals(IPAddress.Any))
                throw new ArgumentException();

            //Defining pinvoke args
            var source = srcAddress == null ? 0 : BitConverter.ToUInt32(srcAddress.GetAddressBytes(), 0);
            //Console.WriteLine($"source = {source}");
            var destination = BitConverter.ToUInt32(destAddress.GetAddressBytes(), 0);
            var sendbuffer = buffer ?? new byte[] { };
            var options = new Interop.Option {
                Ttl = (po == null ? (byte)255 : (byte)po.Ttl),
                Flags = (po == null ? (byte)0 : po.DontFragment ? (byte)0x02 : (byte)0) //0x02
            };
            var fullReplyBufferSize = Interop.ReplyMarshalLength + sendbuffer.Length; //Size of Reply struct and the transmitted buffer length.



            var allocSpace = Marshal.AllocHGlobal(fullReplyBufferSize); // unmanaged allocation of reply size. TODO Maybe should be allocated on stack
            try {
                DateTime start = DateTime.Now;
                var nativeCode = Interop.IcmpSendEcho2Ex(
                    Interop.IcmpHandle, //_In_      HANDLE IcmpHandle,
                    default(IntPtr), //_In_opt_  HANDLE Event,
                    default(IntPtr), //_In_opt_  PIO_APC_ROUTINE ApcRoutine,
                    default(IntPtr), //_In_opt_  PVOID ApcContext
                    source, //_In_      IPAddr SourceAddress,
                    destination, //_In_      IPAddr DestinationAddress,
                    sendbuffer, //_In_      LPVOID RequestData,
                    (short)sendbuffer.Length, //_In_      WORD RequestSize,
                    ref options, //_In_opt_  PIP_OPTION_INFORMATION RequestOptions,
                    allocSpace, //_Out_     LPVOID ReplyBuffer,
                    fullReplyBufferSize, //_In_      DWORD ReplySize,
                    timeout //_In_      DWORD Timeout
                    );
                TimeSpan duration = DateTime.Now - start;
                var reply = (Interop.Reply)Marshal.PtrToStructure(allocSpace, typeof(Interop.Reply)); // Parse the beginning of reply memory to reply struct

                byte[] replyBuffer = null;
                if (sendbuffer.Length != 0) {
                    replyBuffer = new byte[sendbuffer.Length];
                    Marshal.Copy(allocSpace + Interop.ReplyMarshalLength, replyBuffer, 0, sendbuffer.Length); //copy the rest of the reply memory to managed byte[]
                }

                if (nativeCode == 0) //Means that native method is faulted.
                    return new PingReplyExt(nativeCode, reply.Status, new IPAddress(reply.Address), duration);
                else
                    return new PingReplyExt(nativeCode, reply.Status, new IPAddress(reply.Address), reply.RoundTripTime, replyBuffer);
            }
            
            finally {
                Marshal.FreeHGlobal(allocSpace); //free allocated space
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
        public static bool ConnectionIpsecVPNIsGood(NetworkAdapter networkAdapter, string destaddress) {              //host is ip or dns name

             List<string> ipv4address = networkAdapter.ShowIPv4Address();
            if (ipv4address.Count() == 0)
                return false;

            PingReplyExt pr = Send(IPAddress.Parse(ipv4address[0]), IPAddress.Parse(destaddress));
            if (pr.Status == IPStatus.Success)
                return true;
            else
                return false;            

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
