using Microsoft.Win32;
using StandLaunchpad.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StandLaunchpad
{
    public enum LauncherType
    {
        EpicGames = 0,
        Steam = 1,
        RockstarGames = 2,
        None = -1
    }

    internal class Stand
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, int bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, IntPtr dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] buffer, uint size, int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttribute, IntPtr dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        static void Debug(string doing, bool succeeded)
        {
            Console.WriteLine(string.Format("{0} succeeded: {1}", doing, succeeded));
        }

        static Random random = new Random();

        static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZqwertyuiopasdfghjklzxcvbnm0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private static HttpClient GetClient() => new HttpClient();

        public static Task DownloadStandDLL(string version, string file, Action<int> progressChanged)
        {
            var promise = new TaskCompletionSource<bool>();
            MainWindow.StaticWindow.statusText.Content = "Downloading Stand " + version + "...";

            WebClient wc = new WebClient();

            wc.DownloadProgressChanged += (s, e) =>
                progressChanged?.Invoke(e.ProgressPercentage);

            wc.DownloadFileCompleted += (s, e) =>
            {
                promise.TrySetResult(!e.Cancelled);
                MainWindow.StaticWindow.statusText.Content = "Downloaded Stand " + version + "...";
            };

            string uri = $"https://stand.gg/Stand%20{version}.dll";
            wc.DownloadFileAsync(new Uri(uri), file);

            return promise.Task;

            //Console.WriteLine($"Downloading Stand {version}...");
            //var r = await GetClient().GetAsync($"https://stand.gg/Stand%20{version}.dll");

            //if (r.StatusCode == HttpStatusCode.OK)
            //{
            //    byte[] st = await r.Content.ReadAsByteArrayAsync();

            //    File.WriteAllBytes(file, st);
            //}
        }

        public static async Task<(string, string)> GetLatestVersion()
        {
            string versions = await GetClient().GetStringAsync("https://stand.gg/versions.txt");
            string[] vSplit = versions.Split(':');

            string launchpadVersion = vSplit[0];
            string standVersion = vSplit[1];

            return (standVersion, launchpadVersion);
        }

        public static async Task<string[]> GetAllVersions()
        {
            string changelog = await GetClient()
                .GetStringAsync("https://stand.gg/help/changelog");

            return changelog.Split("Stand ")
                .Where(e => e.StartsWith("0."))
                .Select(e => e.Split(" ")[0])
                .ToArray();
        }

        public static string StandPath()
        {
            return Path.Combine(@"C:\Users", Environment.UserName, "AppData", "Roaming", "Stand");
        }

        public static void EnsureStandDirExists()
        {
            if (!Directory.Exists(StandPath()))
                Directory.CreateDirectory(StandPath());

            Directory.CreateDirectory(Path.Combine(StandPath(), "Bin"));
        }

        public static async Task<string> GetDllPath()
        {
            string binFolder = Path.Combine(StandPath(), "Bin");

            (string latestVersion, _) = await GetLatestVersion();

            string bin = Path.Combine(binFolder, $"Stand v{latestVersion}.dll");

            if (!File.Exists(bin))
            {
                await DownloadStandDLL(latestVersion, bin, (prog) =>
                {
                    MainWindow.StaticWindow.SetProgress(prog);
                });
            }

            return bin;
        }

        public static bool Inject(string dllName, uint processId)
        {
            string temp = Path.Combine(Path.GetTempPath(), "Stand");

            if (!Directory.Exists(temp))
                Directory.CreateDirectory(temp);

            string tmp = Path.Combine(temp, RandomString(random.Next(6, 10)) + ".dll");
            File.Copy(dllName, tmp);

            dllName = tmp;

            {
                IntPtr p = OpenProcess(1082U, 1, processId);
                string dllFullPath = Path.GetFullPath(dllName);

                Debug("Process open", p != IntPtr.Zero);

                if (p != IntPtr.Zero)
                {
                    IntPtr modHandle = GetModuleHandle("kernel32.dll");
                    IntPtr pAddress = GetProcAddress(modHandle, "LoadLibraryA");

                    Debug("Get process address", pAddress != IntPtr.Zero);

                    if (pAddress != IntPtr.Zero)
                    {
                        IntPtr address = VirtualAllocEx(
                            p, (IntPtr)null,
                            (IntPtr)dllFullPath.Length,
                            12288U, 64U
                        );

                        Debug("Allocate memory", address != IntPtr.Zero);

                        if (address != IntPtr.Zero)
                        {
                            byte[] dllPathBytes = Encoding.UTF8.GetBytes(dllFullPath);

                            int writeMemResult = WriteProcessMemory(
                                p, address, dllPathBytes,
                                (uint)dllPathBytes.Length, 0
                            );

                            Debug("Write dll file path string", writeMemResult != 0);

                            if (writeMemResult != 0)
                            {
                                IntPtr createThreadResult = CreateRemoteThread(
                                    p, (IntPtr)null,
                                    IntPtr.Zero,
                                    pAddress, address,
                                    0U, (IntPtr)null
                                );

                                Debug("Create remote thread", createThreadResult != IntPtr.Zero);

                                if (createThreadResult != IntPtr.Zero)
                                {
                                    Console.WriteLine("Successfully injected DLL!!!");
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Unable to inject");

            Console.WriteLine("win32 error code : " + Marshal.GetLastWin32Error());

            return false;
        }

        public static Task<bool> LaunchGTA(LauncherType type)
        {
            switch (type)
            {
                case LauncherType.EpicGames:
                    Process.Start("com.epicgames.launcher://apps/9d2d0eb64d5c44529cece33fe2a46482?action=launch&silent=true");
                    break;

                case LauncherType.Steam:
                    Process.Start("steam://run/271590");
                    break;

                case LauncherType.RockstarGames:
                    try
                    {
                        using (RegistryKey rockstar = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Rockstar Games\\Launcher"))
                        {
                            if (rockstar != null)
                                MessageBox.Show("R* games might not be installed");

                            string dir = (string)rockstar.GetValue("InstallFolder");

                            if (!string.IsNullOrEmpty(dir))
                                Process.Start(dir + "\\Launcher.exe", "-minmodeApp=gta5");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }
                    break;

                default:
                    return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        public static bool IsGtaRunning() =>
            Process.GetProcessesByName("GTA5").Any();

        public static uint GetProcessId() =>
            (uint)Process.GetProcessesByName("GTA5").First().Id;

        public static async Task WaitUntilGtaStarts()
        {
            while (!IsGtaRunning())
                await Task.Delay(200);
        }

        public static Task AsyncTimeout(Task task, int timeout)
        {
            return Task.WhenAny(task, Task.Delay(timeout * 1000));
        }

        public static void OpenChangelog()
        {
            Changelog chlog = new Changelog();
            chlog.Show();
        }
    }
}
