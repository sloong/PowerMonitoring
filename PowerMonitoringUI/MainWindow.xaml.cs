using Microsoft.Win32;
using Sloong;
using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace PowerMonitoringUI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        static string SERVICENAME = "PowerMonitoringService";
        static string REGISTERPATH = "SOFTWARE\\SLOONG.COM\\PowerMonitoring";
        string AppFolder = @"\SLOONG.COM\PowerMonitoring";
        string installutil_path = @"\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe";
        RegisterEx reg;
        public MainWindow()
        {
            InitializeComponent();
            AppFolder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + AppFolder;
            installutil_path = Environment.GetFolderPath(Environment.SpecialFolder.Windows) + installutil_path;
            reg = new RegisterEx(Registry.LocalMachine, REGISTERPATH);
            GetDataToUI();
            UpdateServiceStatus();
        }

        public void UpdateServiceStatus()
        {
            var path = Utility.GetServicePath(SERVICENAME);

            var ver = Utility.GetExeVersionInfo(path);
            var status = Utility.GetServiceStatus(SERVICENAME);
            if (status == "")
            {
                _ServiceStatus.Content = "No Install";
                _InstallBtn.Visibility = Visibility.Visible;
                _UninstallBtn.Visibility = Visibility.Hidden;
                _StartServiceBtn.Visibility = Visibility.Hidden;
                _StopServiceBtn.Visibility = Visibility.Hidden;
            }
            else
            {
                _ServiceStatus.Content = string.Format("{0} -- {1}", ver, status);
                _InstallBtn.Visibility = Visibility.Hidden;
                _UninstallBtn.Visibility = Visibility.Visible;
                if( status == "Stopped")
                {
                    _StartServiceBtn.Visibility = Visibility.Visible;
                    _StopServiceBtn.Visibility = Visibility.Hidden;
                }
                else
                {
                    _StopServiceBtn.Visibility = Visibility.Visible;
                    _StartServiceBtn.Visibility = Visibility.Hidden;
                }
                
                
            }
        }


        

        /// <summary>
        /// Get data from register and set to UI .
        /// </summary>
        void GetDataToUI()
        {
            _Interval.Text = reg.GetValue("CheckInterval", "60");
            _RetryNum.Text = reg.GetValue("CheckTotal", "100");
            _TargetIP.Text = reg.GetValue("TargetIP", "");
            var opt = reg.GetValue("PowerOFFOpt", "Shutdown");

            foreach (ComboBoxItem item in _OFFOpt.Items)
            {

                if (item.Tag.ToString() == opt)
                {
                    _OFFOpt.SelectedItem = item;
                    break;
                }
            }
            
            if(_TargetIP.Text=="")
                _TargetIP.Text = Utility.GetGatewayIP();
        }


        /// <summary>
        /// Save UI data to register
        /// </summary>
        void SaveUIToData()
        {
            reg.SetValue("CheckInterval", _Interval.Text);
            reg.SetValue("CheckTotal", _RetryNum.Text);
            reg.SetValue("TargetIP", _TargetIP.Text);
            
            ComboBoxItem item = _OFFOpt.SelectedItem as ComboBoxItem;
            reg.SetValue("PowerOFFOpt", item.Tag.ToString());
            if (item.Tag.ToString() == "Shutdown")
            {
                reg.SetValue("ShutdownTime", _OFFOptValue.Text);
            }
            else if (item.Tag.ToString() == "SpecialCMD")
            {
                reg.SetValue("SpecialCMD", _OFFOptValue.Text);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SaveUIToData();
        }

        private void Button_Install_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists("PowerMonitoring.exe"))
            {
                if (Directory.Exists(AppFolder))
                    Directory.Delete(AppFolder,true);
                
                Directory.CreateDirectory(AppFolder);
                File.Copy("PowerMonitoring.exe", AppFolder + "\\PowerMonitoring.exe");
                File.Copy("PowerMonitoringUI.exe", AppFolder + "\\PowerMonitoringUI.exe");


                Utility.RunCMD(string.Format("{0} \"{1}\"", installutil_path, AppFolder + "\\PowerMonitoring.exe"));
                Thread.Sleep(500);

                UpdateServiceStatus();
            }
        }

        private void Button_Uninstall_Click(object sender, RoutedEventArgs e)
        {
            var path = Utility.GetServicePath(SERVICENAME);
            Utility.RunCMD(string.Format("{0} /u \"{1}\"",installutil_path,path));
            Thread.Sleep(500);
            UpdateServiceStatus();
        }

        private void Button_Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void _OFFOpt_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem item = _OFFOpt.SelectedItem as ComboBoxItem;
            if (item.Tag.ToString() == "Shutdown")
            {
                _OFFOptName.Visibility = Visibility.Visible;
                _OFFOptValue.Visibility = Visibility.Visible;
                _OFFOptName.Content = "关机预留时间";
                _OFFOptValue.Text = reg.GetValue("ShutdownTime", "60");
            }
            else if(item.Tag.ToString() == "SpecialCMD")
            {
                _OFFOptName.Visibility = Visibility.Visible;
                _OFFOptValue.Visibility = Visibility.Visible;
                _OFFOptName.Content = "自定义命令";
                _OFFOptValue.Text = reg.GetValue("SpecialCMD", "");
            }
            else
            {
                _OFFOptName.Visibility = Visibility.Hidden;
                _OFFOptValue.Visibility = Visibility.Hidden;
            }
        }

        private void _StopServiceBtn_Click(object sender, RoutedEventArgs e)
        {
            System.ServiceProcess.ServiceController sc = new System.ServiceProcess.ServiceController(SERVICENAME);
            sc.Stop();
            UpdateServiceStatus();
        }

        private void _StartServiceBtn_Click(object sender, RoutedEventArgs e)
        {
            System.ServiceProcess.ServiceController sc = new System.ServiceProcess.ServiceController(SERVICENAME);
            sc.Start();
            UpdateServiceStatus();
        }

        
        private void Button_Refresh_Click(object sender, RoutedEventArgs e)
        {
            UpdateServiceStatus();
        }
    }
}
