using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using static System.Windows.Forms.ListBox;
using Path = System.IO.Path;

namespace StandLaunchpad
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Stand.EnsureStandDirExists();
            StaticWindow = this;

            timer = new Timer(timerTick, injectBtn, 500, Timeout.Infinite);
        }

        public static MainWindow StaticWindow;
        public bool advancedMode = false;
        public string standDll;

        public List<string> dlls = new List<string>();

        public Timer timer;

        public static string SettingsFile = Path.Combine(Stand.StandPath(), "Launchpad.txt");
        public static Settings settings = Settings.ReadFile(SettingsFile);

        private void timerTick(object _)
        {
            bool gtaRunning = Stand.IsGtaRunning();

            Visibility visibility = gtaRunning ?
                    Visibility.Visible :
                    Visibility.Hidden;

            injectBtn.Dispatcher.Invoke(
                () => {
                    injectBtn.Visibility = visibility;
                    autoInject.IsEnabled = !gtaRunning;
                },
                DispatcherPriority.Normal
            );
        }

        private void UseSettings(Settings settings)
        {
            advancedMode = settings.AdvancedMode;
            SetAdvancedMode(advancedMode);
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        public void SetProgress(int prog)
        {
            progressBar.Visibility = (prog == 0 || prog == 100) ?
                Visibility.Hidden : Visibility.Hidden;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (launcherSelect.SelectedIndex == -1)
            {
                MessageBox.Show("pwease choose a launcher tanks");
                return;
            }

            await Stand.LaunchGTA((LauncherType)launcherSelect.SelectedIndex);

            if (autoInject.IsChecked.Value)
            {
                await Stand.AsyncTimeout(Stand.WaitUntilGtaStarts(), 60);
                await Stand.AsyncTimeout(Stand.WaitUntilGtaStarts(), 5);  // for some reason GTA5.exe closes and restarts itself so we must check twice

                if (Stand.IsGtaRunning())
                {
                    Console.WriteLine("gta is now running; injecting now");

                    uint pid = Stand.GetProcessId();
                    string dllPath = await Stand.GetDllPath(); // check stand dll and download if required

                    if (Stand.Inject(dllPath, pid))
                    {
                        statusText.Content = $"Injected Stand into PID {pid}";
                        MessageBox.Show("You're a [Stand User] now! :D");
                    }
                    else
                    {
                        statusText.Content = $"Failed to inject Stand into PID {pid}";
                    }
                }
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            launcherSelect.SelectedIndex = 1;
            this.HideMinimizeAndMaximizeButtons();
            SetAdvancedMode(false);

            standDll = await Stand.GetDllPath();

            UseSettings(settings);
        }

        private void autoInject_Click(object sender, RoutedEventArgs e)
        {
        }

        private void SetAdvancedMode(bool am)
        {
            advancedOptions.Width = am ?
                new GridLength(1, GridUnitType.Star) :
                new GridLength(0, GridUnitType.Pixel);

            mainOptions.Width = am ?
                new GridLength(241, GridUnitType.Pixel) :
                new GridLength(1, GridUnitType.Star);

            Width = am ? 650 : 263;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            advancedMode = !advancedMode;

            settings.AdvancedMode = advancedMode;
            settings.Save(SettingsFile);

            SetAdvancedMode(advancedMode);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            Process.Start(Stand.StandPath());
        }

        private async void Button_Click_3(object sender, RoutedEventArgs e)
        {
            // check for updates


            // get dll path and remove path and extension
            // (Stand.GetDllPath downloads updates)
            string p = await Stand.GetDllPath(); // C:\...\Stand vX.X.X.dll
            string fn = Path.GetFileName(p); // Stand vX.X.X.dll
            string[] spl = fn.Split('.'); // [ "Stand vX", "X", "X", "dll" ]
            string name = string.Join(".", spl.Take(spl.Length - 1)); // Stand vX.X.X

            statusText.Content = $"You are now using {name}";

            MessageBox.Show($"You are now using {name}");
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            // changelog
            Stand.OpenChangelog();
        }

        //void injectAll()
        //{
        //    var selectedItems = dllCheckboxes.SelectedItems;

        //    for (int i = selectedItems.Count - 1; i >= 0; i--)
        //    {
        //        var item = selectedItems[i] as ListBoxItem;
        //        Console.WriteLine(item.IsSelected);
        //    }
        //}

        private async void injectBtn_Click(object sender, RoutedEventArgs e)
        {
            uint pid = Stand.GetProcessId();
            string dllPath = await Stand.GetDllPath(); // check stand dll and download if required

            //injectAll();
            //return;

            if (Stand.Inject(dllPath, pid))
            {
                statusText.Content = $"Injected Stand into PID {pid}";
                MessageBox.Show("You're a [Stand User] now! :D");
            }
            else
            {
                statusText.Content = $"Failed to inject Stand into PID {pid}";
            }
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            dllCheckboxes.Items.Add(new CheckBox()
            {
                Content = "new checkbox :o",

            });

            //OpenFileDialog fg = new OpenFileDialog();
            //fg.Multiselect = true;
            //fg.Filter = "DLL Files|*.dll";

            //if (fg.ShowDialog().Value)
            //{
            //    foreach (var file in fg.FileNames)
            //    {
            //        dllCheckboxes.Items.Add(new CheckBox()
            //        {
            //            Content = file,

            //        });
            //        dlls.Add(file);
            //    }
            //}
        }

        //private void UpdateDlls(List<string> dlls)
        //{
        //    dllCheckboxes.Items.Clear();
        //    foreach (var dll in dlls)
        //    {
        //        dllCheckboxes.Items.Add(new ListBoxItem().Children);
        //    }
        //}

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            var selectedItems = dllCheckboxes.SelectedItems;

            if (dllCheckboxes.SelectedIndex != -1)
            {
                for (int i = selectedItems.Count - 1; i >= 0; i--)
                {
                    dllCheckboxes.Items.Remove(selectedItems[i]);
                    //dlls.RemoveAt(i);
                }
            }
            else
            {
                SystemSounds.Asterisk.Play();
            }
        }
    }
}
