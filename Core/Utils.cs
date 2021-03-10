using System;
using System.Management;

namespace Arco.Core
{
    public class Utils
    {
        public static bool IsNumberInRange(string number, int from, int to)
        {
            int n;
            try
            {
                n = Convert.ToInt32(number);
                return n >= from && n <= to;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return false;
        }
        public static string SysInfo()
        {
            string sysinfo = "";

            string cpuInfo = "";
            ManagementObjectCollection mocWin32_Processor = (new ManagementClass("Win32_Processor")).GetInstances();
            foreach (ManagementObject mo in mocWin32_Processor)
            {
                cpuInfo += mo.Properties["ProcessorId"].Value.ToString();
            }
            sysinfo += cpuInfo+"/";

            String HDid = "";
            ManagementClass mc = new ManagementClass("Win32_DiskDrive");
            ManagementObjectCollection moc = mc.GetInstances();
            foreach (ManagementObject mo in moc)
            {
                HDid += (string)mo.Properties["Model"].Value;
            }
            sysinfo += HDid;

            sysinfo = sysinfo.ToMD5();

            return sysinfo;
        }
    }
}
