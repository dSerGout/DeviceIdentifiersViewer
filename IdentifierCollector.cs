using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using DeviceIdentifiersViewer.Models;

namespace DeviceIdentifiersViewer.Services;

public sealed class IdentifierCollector
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetComputerNameExW(int nameType, StringBuilder lpBuffer, ref uint nSize);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetUserNameW(StringBuilder lpBuffer, ref uint pcbBuffer);

    private const int ComputerNamePhysicalDnsHostname = 5;

    public (List<IdentifierItem> Items, List<string> Warnings) CollectAll()
    {
        var items = new List<IdentifierItem>();
        var warnings = new List<string>();

        SafeRun("System", warnings, () => CollectSystem(items));
        SafeRun("BIOS", warnings, () => CollectBios(items));
        SafeRun("CPU", warnings, () => CollectCpu(items));
        SafeRun("Motherboard", warnings, () => CollectMotherboard(items));
        SafeRun("Disk", warnings, () => CollectDisks(items));
        SafeRun("Network", warnings, () => CollectNetwork(items));
        SafeRun("Windows", warnings, () => CollectWindows(items));
        SafeRun("GPU", warnings, () => CollectGpu(items));
        SafeRun("TPM", warnings, () => CollectTpm(items));

        return (items, warnings);
    }

    private static void SafeRun(string category, List<string> warnings, Action action)
    {
        try { action(); }
        catch (Exception ex) { warnings.Add($"[{category}] {ex.GetType().Name}: {ex.Message}"); }
    }

    private void CollectSystem(List<IdentifierItem> items)
    {
        var sb = new StringBuilder(256);
        uint size = (uint)sb.Capacity;
        if (GetComputerNameExW(ComputerNamePhysicalDnsHostname, sb, ref size))
            Add(items, "System", "Computer Name (DNS)", sb.ToString(), "WinAPI GetComputerNameExW");

        var userSb = new StringBuilder(256);
        uint userSize = (uint)userSb.Capacity;
        if (GetUserNameW(userSb, ref userSize))
            Add(items, "System", "Current User", userSb.ToString(), "WinAPI GetUserNameW");

        using var searcher = new ManagementObjectSearcher(
            "SELECT Manufacturer, Model, Name, Domain, TotalPhysicalMemory FROM Win32_ComputerSystem");
        foreach (ManagementObject mo in searcher.Get())
        {
            Add(items, "System", "Manufacturer", Str(mo, "Manufacturer"), "WMI Win32_ComputerSystem");
            Add(items, "System", "Model", Str(mo, "Model"), "WMI Win32_ComputerSystem");
            Add(items, "System", "Domain", Str(mo, "Domain"), "WMI Win32_ComputerSystem");
            Add(items, "System", "Total RAM (bytes)", Str(mo, "TotalPhysicalMemory"), "WMI Win32_ComputerSystem");
        }

        using var prod = new ManagementObjectSearcher(
            "SELECT IdentifyingNumber, Name, Vendor, UUID FROM Win32_ComputerSystemProduct");
        foreach (ManagementObject mo in prod.Get())
        {
            Add(items, "System", "System UUID (SMBIOS)", Str(mo, "UUID"), "WMI Win32_ComputerSystemProduct");
            Add(items, "System", "Identifying Number", Str(mo, "IdentifyingNumber"), "WMI Win32_ComputerSystemProduct");
        }
    }

    private void CollectBios(List<IdentifierItem> items)
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT Manufacturer, Name, Version, SerialNumber, ReleaseDate, SMBIOSBIOSVersion FROM Win32_BIOS");
        foreach (ManagementObject mo in searcher.Get())
        {
            Add(items, "BIOS", "Manufacturer", Str(mo, "Manufacturer"), "WMI Win32_BIOS");
            Add(items, "BIOS", "SMBIOS Version", Str(mo, "SMBIOSBIOSVersion"), "WMI Win32_BIOS");
            Add(items, "BIOS", "Version", Str(mo, "Version"), "WMI Win32_BIOS");
            Add(items, "BIOS", "Serial Number", Str(mo, "SerialNumber"), "WMI Win32_BIOS");
            Add(items, "BIOS", "Release Date", Str(mo, "ReleaseDate"), "WMI Win32_BIOS");
        }
    }

    private void CollectCpu(List<IdentifierItem> items)
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT Name, Manufacturer, ProcessorId, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed FROM Win32_Processor");
        foreach (ManagementObject mo in searcher.Get())
        {
            Add(items, "CPU", "Name", Str(mo, "Name").Trim(), "WMI Win32_Processor");
            Add(items, "CPU", "ProcessorId", Str(mo, "ProcessorId"), "WMI Win32_Processor");
            Add(items, "CPU", "Cores", Str(mo, "NumberOfCores"), "WMI Win32_Processor");
            Add(items, "CPU", "Logical Processors", Str(mo, "NumberOfLogicalProcessors"), "WMI Win32_Processor");
            Add(items, "CPU", "Max Clock (MHz)", Str(mo, "MaxClockSpeed"), "WMI Win32_Processor");
        }
    }

    private void CollectMotherboard(List<IdentifierItem> items)
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT Manufacturer, Product, SerialNumber, Version FROM Win32_BaseBoard");
        foreach (ManagementObject mo in searcher.Get())
        {
            Add(items, "Motherboard", "Manufacturer", Str(mo, "Manufacturer"), "WMI Win32_BaseBoard");
            Add(items, "Motherboard", "Product", Str(mo, "Product"), "WMI Win32_BaseBoard");
            Add(items, "Motherboard", "Serial Number", Str(mo, "SerialNumber"), "WMI Win32_BaseBoard");
            Add(items, "Motherboard", "Version", Str(mo, "Version"), "WMI Win32_BaseBoard");
        }
    }

    private void CollectDisks(List<IdentifierItem> items)
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT Model, SerialNumber, Size, MediaType, InterfaceType, Index FROM Win32_DiskDrive");
        int i = 0;
        foreach (ManagementObject mo in searcher.Get())
        {
            string prefix = $"Disk #{i}";
            Add(items, "Disk", $"{prefix} Model", Str(mo, "Model").Trim(), "WMI Win32_DiskDrive");
            Add(items, "Disk", $"{prefix} Serial Number", Str(mo, "SerialNumber").Trim(), "WMI Win32_DiskDrive");
            Add(items, "Disk", $"{prefix} Size (bytes)", Str(mo, "Size"), "WMI Win32_DiskDrive");
            Add(items, "Disk", $"{prefix} Interface", Str(mo, "InterfaceType"), "WMI Win32_DiskDrive");
            i++;
        }
    }

    private void CollectNetwork(List<IdentifierItem> items)
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT Name, MACAddress, PhysicalAdapter, NetEnabled, Speed FROM Win32_NetworkAdapter");
        foreach (ManagementObject mo in searcher.Get())
        {
            bool physical = mo["PhysicalAdapter"] is bool b && b;
            if (!physical) continue;
            string name = Str(mo, "Name").Trim();
            if (string.IsNullOrWhiteSpace(name)) continue;
            Add(items, "Network", $"{name} — MAC", Str(mo, "MACAddress"), "WMI Win32_NetworkAdapter");
        }

        using var cfg = new ManagementObjectSearcher(
            "SELECT Description, IPAddress, IPSubnet, DefaultIPGateway, DNSServerSearchOrder, DHCPEnabled FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = TRUE");
        foreach (ManagementObject mo in cfg.Get())
        {
            string desc = Str(mo, "Description").Trim();
            var ips = mo["IPAddress"] as string[];
            if (ips != null && ips.Length > 0)
                Add(items, "Network", $"{desc} — IP", string.Join(", ", ips), "WMI Win32_NetworkAdapterConfiguration");
            var dns = mo["DNSServerSearchOrder"] as string[];
            if (dns != null && dns.Length > 0)
                Add(items, "Network", $"{desc} — DNS", string.Join(", ", dns), "WMI Win32_NetworkAdapterConfiguration");
        }
    }

    private void CollectWindows(List<IdentifierItem> items)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            var guid = key?.GetValue("MachineGuid")?.ToString();
            if (!string.IsNullOrEmpty(guid))
                Add(items, "Windows", "MachineGuid", guid!, @"HKLM\SOFTWARE\Microsoft\Cryptography");
        }
        catch { }

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key != null)
            {
                Add(items, "Windows", "ProductName", key.GetValue("ProductName")?.ToString() ?? "", @"HKLM\...\CurrentVersion");
                Add(items, "Windows", "EditionID", key.GetValue("EditionID")?.ToString() ?? "", @"HKLM\...\CurrentVersion");
                Add(items, "Windows", "DisplayVersion", key.GetValue("DisplayVersion")?.ToString() ?? "", @"HKLM\...\CurrentVersion");
                Add(items, "Windows", "CurrentBuild", key.GetValue("CurrentBuild")?.ToString() ?? "", @"HKLM\...\CurrentVersion");
                Add(items, "Windows", "BuildLabEx", key.GetValue("BuildLabEx")?.ToString() ?? "", @"HKLM\...\CurrentVersion");
                Add(items, "Windows", "InstallDate (unix)", key.GetValue("InstallDate")?.ToString() ?? "", @"HKLM\...\CurrentVersion");
                Add(items, "Windows", "RegisteredOwner", key.GetValue("RegisteredOwner")?.ToString() ?? "", @"HKLM\...\CurrentVersion");
                Add(items, "Windows", "RegisteredOrganization", key.GetValue("RegisteredOrganization")?.ToString() ?? "", @"HKLM\...\CurrentVersion");
                Add(items, "Windows", "ProductId", key.GetValue("ProductId")?.ToString() ?? "", @"HKLM\...\CurrentVersion");
                Add(items, "Windows", "SystemRoot", key.GetValue("SystemRoot")?.ToString() ?? "", @"HKLM\...\CurrentVersion");
            }
        }
        catch { }

        using var searcher = new ManagementObjectSearcher(
            "SELECT Caption, Version, BuildNumber, OSArchitecture, SerialNumber, InstallDate, LastBootUpTime FROM Win32_OperatingSystem");
        foreach (ManagementObject mo in searcher.Get())
        {
            Add(items, "Windows", "OS Caption", Str(mo, "Caption").Trim(), "WMI Win32_OperatingSystem");
            Add(items, "Windows", "OS Version", Str(mo, "Version"), "WMI Win32_OperatingSystem");
            Add(items, "Windows", "Build Number", Str(mo, "BuildNumber"), "WMI Win32_OperatingSystem");
            Add(items, "Windows", "Architecture", Str(mo, "OSArchitecture"), "WMI Win32_OperatingSystem");
            Add(items, "Windows", "Install Date (WMI)", Str(mo, "InstallDate"), "WMI Win32_OperatingSystem");
            Add(items, "Windows", "Last Boot Up", Str(mo, "LastBootUpTime"), "WMI Win32_OperatingSystem");
        }
    }

    private void CollectGpu(List<IdentifierItem> items)
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT Name, DeviceID, DriverVersion, VideoProcessor, AdapterRAM FROM Win32_VideoController");
        int i = 0;
        foreach (ManagementObject mo in searcher.Get())
        {
            string prefix = $"GPU #{i}";
            Add(items, "GPU", $"{prefix} Name", Str(mo, "Name").Trim(), "WMI Win32_VideoController");
            Add(items, "GPU", $"{prefix} DeviceID", Str(mo, "DeviceID"), "WMI Win32_VideoController");
            Add(items, "GPU", $"{prefix} Driver Version", Str(mo, "DriverVersion"), "WMI Win32_VideoController");
            i++;
        }
    }

    private void CollectTpm(List<IdentifierItem> items)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                @"root\CIMV2\Security\MicrosoftTpm",
                "SELECT SpecVersion, ManufacturerIdTxt, ManufacturerVersion, IsEnabled_InitialValue, IsActivated_InitialValue FROM Win32_Tpm");
            bool any = false;
            foreach (ManagementObject mo in searcher.Get())
            {
                any = true;
                Add(items, "TPM", "Spec Version", Str(mo, "SpecVersion").Trim(), @"WMI Win32_Tpm");
                Add(items, "TPM", "Manufacturer", Str(mo, "ManufacturerIdTxt").Trim(), @"WMI Win32_Tpm");
                Add(items, "TPM", "Manufacturer Version", Str(mo, "ManufacturerVersion").Trim(), @"WMI Win32_Tpm");
                Add(items, "TPM", "Enabled", Str(mo, "IsEnabled_InitialValue"), @"WMI Win32_Tpm");
                Add(items, "TPM", "Activated", Str(mo, "IsActivated_InitialValue"), @"WMI Win32_Tpm");
            }
            if (!any)
                Add(items, "TPM", "Status", "not present or not accessible", @"WMI Win32_Tpm");
        }
        catch
        {
            Add(items, "TPM", "Status", "not accessible (no TPM or no permission)", @"WMI Win32_Tpm");
        }
    }

    private static void Add(List<IdentifierItem> items, string category, string name, string value, string source)
    {
        if (string.IsNullOrWhiteSpace(value)) value = "(empty)";
        items.Add(new IdentifierItem(category, name, value, source));
    }

    private static string Str(ManagementObject mo, string prop)
    {
        try { return mo[prop]?.ToString() ?? string.Empty; }
        catch { return string.Empty; }
    }
}
