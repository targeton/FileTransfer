using System;
using System.Management;

namespace FileTransfer.Permission
{
    public class MachineCode
    {
        public string GetMachineCode()
        {
            string machineCodeString = string.Empty;
            machineCodeString = string.Format("PC.FileTransfer.{0}.{1}.{2}", GetCpuInfo(), GetHDId(), GetMoAddress());
            return machineCodeString;
        }

        private string GetCpuInfo()
        {
            string cpuInfo = string.Empty;
            try
            {
                using (ManagementClass mc = new ManagementClass("Win32_Processor"))
                {
                    ManagementObjectCollection moc = mc.GetInstances();
                    foreach (ManagementObject mo in moc)
                    {
                        cpuInfo += mo.Properties["ProcessorId"].Value.ToString();
                        mo.Dispose();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return cpuInfo;
        }

        private string GetHDId()
        {
            string hdId = string.Empty;
            try
            {
                using (ManagementClass mc = new ManagementClass("Win32_DiskDrive"))
                {
                    ManagementObjectCollection moc = mc.GetInstances();
                    foreach (ManagementObject mo in moc)
                    {
                        hdId += mo.Properties["Model"].Value.ToString();
                        mo.Dispose();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return hdId;
        }

        private string GetMoAddress()
        {
            string moAddress = string.Empty;
            using (ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration"))
            {
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (ManagementObject mo in moc)
                {
                    if ((bool)mo["IPEnabled"] == true)
                    {
                        moAddress += mo["MacAddress"].ToString();
                    }
                    mo.Dispose();
                }
            }
            return moAddress;
        }

    }
}
