using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.NetworkInformation;
using System.Management;
using System.Diagnostics;
using System.Threading;
using Microsoft.Win32;

namespace GamBanUninstaller
{
    class Program
    {
        public static NetworkInterface GetActiveEthernetOrWifiNetworkInterface()
        {
            var Nic = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(
                a => a.OperationalStatus == OperationalStatus.Up &&
                (a.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || a.NetworkInterfaceType == NetworkInterfaceType.Ethernet) &&
                a.GetIPProperties().GatewayAddresses.Any(g => g.Address.AddressFamily.ToString() == "InterNetwork"));

            return Nic;
        }

        public static void RunCommand(string exeName, string command)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = exeName;
            proc.StartInfo.UseShellExecute = true;
            proc.StartInfo.Verb = "runas";
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.StartInfo.Arguments = command;
            proc.Start();
            proc.WaitForExit();
        }

        public static void UnsetDNS()
        {
            var currentNic = GetActiveEthernetOrWifiNetworkInterface();
            string setIpv4DnsAddress = $"interface ipv4 set dns name=\"{currentNic.Name}\" dhcp";

            RunCommand("netsh.exe", setIpv4DnsAddress);
            Console.WriteLine($"Reset IPv4 DNS for inferface {currentNic.Name}");

            string setIpv6DnsAddress = $"interface ipv6 set dns name=\"{currentNic.Name}\" dhcp";
            RunCommand("netsh.exe", setIpv6DnsAddress);

            Console.WriteLine($"Reset IPv6 DNS for inferface {currentNic.Name}");
        }
        public static bool DoUninstall()
        {
            try
            {
                Console.WriteLine("Killing GamBan processes...\n");

                foreach (Process process in Process.GetProcesses())
                {
                    if (process.Id != Process.GetCurrentProcess().Id)
                    {
                        if (process.ProcessName.ToLower().Contains("gamban"))
                        {
                            process.Kill();
                            process.WaitForExit();
                            Console.WriteLine($"Killed process {process.ProcessName}");
                        }
                    }
                }

                Console.WriteLine("\nDeleting GamBan from disk...\n");

                string gambanDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Gamban");
                if (Directory.Exists(gambanDir))
                {
                    Directory.Delete(gambanDir, true);
                    Console.WriteLine("Deleted GamBan folder");
                }

                string beanstalkDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BeanstalkHPS");
                if (Directory.Exists(beanstalkDir))
                {
                    Directory.Delete(beanstalkDir, true);
                    Console.WriteLine("Deleted BeanstalkHPS folder");
                }

                Console.WriteLine("\nDeleting GamBan registry keys...\n");

                Console.WriteLine("Deleting GamBan's Brave Browser registry keys...");
                Registry.LocalMachine.OpenSubKey("SOFTWARE\\Policies\\BraveSoftware\\Brave", true).DeleteValue("DnsOverHttpsMode", false);
                Registry.LocalMachine.OpenSubKey("SOFTWARE\\Policies\\BraveSoftware\\Brave", true).DeleteValue("DnsOverHttpsTemplates", false);
                Registry.LocalMachine.OpenSubKey("SOFTWARE\\Policies\\BraveSoftware\\Brave", true).DeleteValue("IPFSEnabled", false);
                Registry.LocalMachine.OpenSubKey("SOFTWARE\\Policies\\BraveSoftware\\Brave", true).DeleteValue("TorDisabled", false);

                Console.WriteLine("Deleting GamBan's Chromium Browser registry keys...");
                Registry.LocalMachine.OpenSubKey("SOFTWARE\\Policies\\Chromium", true).DeleteValue("DnsOverHttpsMode", false);
                Registry.LocalMachine.OpenSubKey("SOFTWARE\\Policies\\Chromium", true).DeleteValue("DnsOverHttpsTemplates", false);

                Console.WriteLine("Deleting GamBan's Google Chrome Browser registry keys...");
                Registry.LocalMachine.OpenSubKey("SOFTWARE\\Policies\\Google\\Chrome", true).DeleteValue("DnsOverHttpsMode", false);
                Registry.LocalMachine.OpenSubKey("SOFTWARE\\Policies\\Google\\Chrome", true).DeleteValue("DnsOverHttpsTemplates", false);

                Console.WriteLine("Deleting GamBan's Microsoft Edge Browser registry keys...");
                Registry.LocalMachine.OpenSubKey("SOFTWARE\\Policies\\Microsoft\\Edge", true).DeleteValue("DnsOverHttpsMode", false);
                Registry.LocalMachine.OpenSubKey("SOFTWARE\\Policies\\Microsoft\\Edge", true).DeleteValue("DnsOverHttpsTemplates", false);

                Console.WriteLine("Deleting GamBan's Firefox Browser registry keys...");
                Registry.LocalMachine.OpenSubKey("SOFTWARE\\Policies\\Mozilla\\Firefox", true).DeleteValue("Preferences", false);

                Console.WriteLine("\nUnsetting DNS address...\n");

                UnsetDNS();

                Console.WriteLine("\nFlushing DNS...\n");

                RunCommand("ipconfig", "flushdns");
                Console.WriteLine("Flushed DNS");
            }
            catch
            {
                return false;
            }

            return true;
        }

        static void Main(string[] args)
        {
            Console.Title = "GamBan Uninstaller";

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("WARNING!");
            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine("This program will completely uninstall GamBan. Are you sure?\n\nPress any key to continue...");
            Console.ReadKey();

            Console.Clear();

            bool uninstalled = false;
            do
            {
                uninstalled = DoUninstall();

                if (!uninstalled)
                {
                    Console.WriteLine("Uninstall failed, retrying...\n");
                }
            }
            while (!uninstalled);

            Console.WriteLine("\nDone! Please restart your PC.\nPress any key to continue...");
            Console.ReadKey();

            return;
        }
    }
}
