using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;

class Program
{
    static void Main(string[] args)
    {
        string osName = "Unknown Windows";
        string osVersion = GetWindowsVersion();

        if (osVersion.StartsWith("10.0.22000") || osVersion.StartsWith("10.0.2")) 
            osName = "Windows 11";
        else if (osVersion.StartsWith("10.0")) 
            osName = "Windows 10";
        else if (osVersion == "6.3") 
            osName = "Windows 8.1";
        else if (osVersion == "6.2") 
            osName = "Windows 8";
        else if (osVersion == "6.1") 
            osName = "Windows 7";
        else if (osVersion == "6.0") 
            osName = "Windows Vista";
        else if (osVersion == "5.2") 
            osName = "Windows XP x64 or Server 2003";
        else if (osVersion == "5.1") 
            osName = "Windows XP";
        else if (osVersion == "5.0") 
            osName = "Windows 2000";
        else if (Convert.ToInt32(osVersion.Split('.')[0]) < 5) 
            osName = "Windows ME or 98 or less";

        if (Convert.ToInt32(osVersion.Split('.')[0]) < 6 || 
           (Convert.ToInt32(osVersion.Split('.')[0]) == 6 && Convert.ToInt32(osVersion.Split('.')[1]) < 2))
        {
            if (!File.Exists(Path.Combine(Environment.SystemDirectory, "acryptprimitives.dll")))
            {
                if (!IsAdministrator())
                {
                    Elevate();
                    return;
                }

                Console.WriteLine();
                Console.WriteLine("=============================");
                Console.WriteLine("Running Admin shell");
                Console.WriteLine("=============================");

                try
                {
                    File.Copy(
                        Path.Combine(Environment.CurrentDirectory, "oldwin", "acryptprimitives.dll"),
                        Path.Combine(Environment.SystemDirectory, "acryptprimitives.dll")
                    );
                }
                catch { }
            }
        }

        if (IsServiceRunning("Tor Win32 Service"))
        {
            Process.Start("service-manager.cmd");
            System.Threading.Thread.Sleep(2000);
        }

        Process torProcess = new Process();
        torProcess.StartInfo.WorkingDirectory = Path.Combine(Environment.CurrentDirectory, "tor");
        torProcess.StartInfo.FileName = "tor";
        torProcess.StartInfo.Arguments = "-f ../torrc.txt";
        torProcess.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
        torProcess.Start();
    }

    static string GetWindowsVersion()
    {
        return Environment.OSVersion.Version.Major + "." + 
               Environment.OSVersion.Version.Minor + "." + 
               Environment.OSVersion.Version.Build;
    }

    static bool IsAdministrator()
    {
        return new WindowsPrincipal(WindowsIdentity.GetCurrent())
            .IsInRole(WindowsBuiltInRole.Administrator);
    }

    static void Elevate()
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = Process.GetCurrentProcess().MainModule.FileName,
            Verb = "runas",
            UseShellExecute = true
        };

        try
        {
            Process.Start(startInfo);
            Environment.Exit(0);
        }
        catch
        {
            Environment.Exit(1);
        }
    }

    static bool IsServiceRunning(string serviceName)
    {
        Process process = new Process();
        process.StartInfo.FileName = "sc";
        process.StartInfo.Arguments = $"query \"{serviceName}\"";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return output.Contains("RUNNING");
    }
}
