using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace UpIpsecVPN
{
    class NetworkAdapter
    {
        private string nameAdapter;
        public NetworkAdapter(string nameAdapter) {

            this.nameAdapter = nameAdapter;
        }

        public bool IsInterfaceUP() {
           
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            NetworkInterface adapter = adapters.Where(a => a.Name == this.nameAdapter).Where(a => a.OperationalStatus == OperationalStatus.Up).FirstOrDefault();            
            if (adapter == null)
                return false;
            else
                return true;
        }

        public List<string> ShowIPv4Address() {

            List<string> IpAddress = new List<string>();
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            NetworkInterface adapter = adapters.Where(a => a.Name == this.nameAdapter).Where(a => a.OperationalStatus == OperationalStatus.Up).FirstOrDefault();
            if (adapter != null) {
                foreach (UnicastIPAddressInformation ip in adapter.GetIPProperties().UnicastAddresses) {
                    if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
                        //Console.WriteLine($"{ip.Address}");
                        IpAddress.Add(ip.Address.ToString());
                    }
                }
            }
            return IpAddress;
        }
    }
}
