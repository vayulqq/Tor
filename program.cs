using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Security.Principal;

[SupportedOSPlatform("windows")]
class Program
{
    static void Main(string[] args)
    {
        string osVersion = GetWindowsVersion();
        string osName = DetermineWindowsName(osVersion);

        if (RequiresAdminCheck(osVersion))
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
                catch (Exception ex)
                {
                    Console.WriteLine($"Error copying file: {ex.Message}");
                }
            }
        }

        if (IsServiceRunning("Tor Win32 Service"))
        {
            Process.Start("service-manager.cmd");
            System.Threading.Thread.Sleep(2000);
        }

        StartTorProcess();
    }

    static string GetWindowsVersion()
    {
        var os = Environment.OSVersion;
        return $"{os.Version.Major}.{os.Version.Minor}.{os.Version.Build}";
    }

    static string DetermineWindowsName(string osVersion)
    {
        if (osVersion.StartsWith("10.0.22000") || osVersion.StartsWith("10.0.2")) 
            return "Windows 11";
        if (osVersion.StartsWith("10.0")) 
            return "Windows 10";
        if (osVersion == "6.3") 
            return "Windows 8.1";
        if (osVersion == "6.2") 
            return "Windows 8";
        if (osVersion == "6.1") 
            return "Windows 7";
        if (osVersion == "6.0") 
            return "Windows Vista";
        if (osVersion == "5.2") 
            return "Windows XP x64 or Server 2003";
        if (osVersion == "5.1") 
            return "Windows XP";
        if (osVersion == "5.0") 
            return "Windows 2000";
        if (Convert.ToInt32(osVersion.Split('.')[0]) < 5) 
            return "Windows ME or 98 or less";
        
        return "Unknown Windows";
    }

    static bool RequiresAdminCheck(string osVersion)
    {
        var parts = osVersion.Split('.');
        int major = Convert.ToInt32(parts[0]);
        int minor = parts.Length > 1 ? Convert.ToInt32(parts[1]) : 0;
        
        return major < 6 || (major == 6 && minor < 2);
    }

    static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    static void Elevate()
    {
        var exePath = Environment.ProcessPath ?? 
                      Process.GetCurrentProcess().MainModule?.FileName ??
                      throw new InvalidOperationException("Could not determine executable path");

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
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
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "sc",
            Arguments = $"query \"{serviceName}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return output.Contains("RUNNING");
    }

    static void StartTorProcess()
    {
        var torPath = Path.Combine(Environment.CurrentDirectory, "tor");
        if (!Directory.Exists(torPath)) return;

        var torProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                WorkingDirectory = torPath,
                FileName = "tor",
                Arguments = "-f ../torrc.txt",
                WindowStyle = ProcessWindowStyle.Minimized
            }
        };

        torProcess.Start();
    }
}
