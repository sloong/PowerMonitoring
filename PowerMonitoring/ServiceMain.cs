using Microsoft.Win32;
using Sloong;
using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;

namespace PowerMonitoring
{
    public partial class ServiceMain : ServiceBase
    {
        Thread _WorkThread = null;
        string sourceName = "PowerMonitoring";
        EventLog _EventLog = null;
        bool _Running = false;

        string _TargetIP = "";

        int _CheckInterval;
        int _CheckTotal;
        int _ShutdownTime;
        string _SpecialCMD;
        string _PowerOFFOpt;
        RegisterEx reg;
        public ServiceMain()
        {
            InitializeComponent();
            if (!EventLog.SourceExists(sourceName))
            {
                EventLog.CreateEventSource(sourceName, "Application");
            }
            _EventLog = new EventLog();
            _EventLog.Source = sourceName;
            reg = new RegisterEx(Registry.LocalMachine, "SOFTWARE\\SLOONG.COM\\PowerMonitoring");

            WriteLog("PowerMonitioring Initialize.");
        }

        public void WriteLog(string msg, EventLogEntryType type = EventLogEntryType.Information)
        {
            var message = $"{AppDomain.CurrentDomain.BaseDirectory}{Environment.NewLine}";
            message = message + msg;
            _EventLog.WriteEntry(message, type);
        } 

       

        protected override void OnStart(string[] args)
        {
            WriteLog("PowerMonitioring Service Start.");

            // Get param from Registry
            // Check interval time. default is 60 second
            _CheckInterval = Convert.ToInt32(reg.GetValue("CheckInterval", "60"));
            // Check total. default is 100
            _CheckTotal = Convert.ToInt32(reg.GetValue("CheckTotal", "100"));
            // Shutdown time 
            _ShutdownTime = Convert.ToInt32(reg.GetValue("ShutdownTime", "60"));
            _PowerOFFOpt = reg.GetValue("PowerOFFOpt", "Shutdown");
            _SpecialCMD = reg.GetValue("SpecialCMD", "");
            // target ip .
            _TargetIP = reg.GetValue("TargetIP", "");
            if (_TargetIP == "")
            {
                _TargetIP = Utility.GetGatewayIP();
                WriteLog("System Geteway IP: " + _TargetIP);
            }

            WriteLog(string.Format("Work Param. Check Interval[{0}]; CheckTotal[{1}]; TargetIP[{2}]", _CheckInterval, _CheckTotal, _TargetIP));

            _CheckInterval = _CheckInterval * 1000;

            _Running = true;
            _WorkThread = new Thread(WorkLoop);
            _WorkThread.Name = "Work Thread";
            _WorkThread.Start();
        }

        protected override void OnStop()
        {
            WriteLog("PowerMonitoring Stopped.");
            _Running = false;
            _WorkThread.Abort();
        }

        public void WorkLoop()
        {
            WriteLog("Work thread is starting.");
#if DEBUG
            Thread.Sleep(10000);
#endif
            while (_Running)
            {
                int RetryNum = 0;

                RetryPing:
                // 这里减去其他检测操作的耗时，暂定100毫秒
                Thread.Sleep(800);
                if (Utility.IsPingIP(_TargetIP))
                {
                    // 成功
                    Thread.Sleep(_CheckInterval);
                }
                else
                {
                    if( RetryNum < _CheckTotal)
                    {
                        RetryNum++;
                        if (RetryNum%5==0)
                            WriteLog("ping geteway failed. retry " + RetryNum, EventLogEntryType.Warning);
                        goto RetryPing;
                    }
                    else
                    {
                        WriteLog("ping geteway failed. shutdown system.", EventLogEntryType.Error);
                        DoPowerOFFOpt();
                    }
                }
            }
        }

        /// <summary>
        /// 运行关机命令
        /// </summary>
        public void DoPowerOFFOpt()
        {
            string cmd;
            if( _PowerOFFOpt == "Shutdown")
            {
                cmd = string.Format("shutdown /s /t {0} /d U:06:12 /c \"Power Off. Shutdown system.\"", _ShutdownTime);
            }
            else if(_PowerOFFOpt == "SpecialCMD")
            {
                cmd = _SpecialCMD;
            }
            else
            {
                cmd = "";
            }
            WriteLog(string.Format("PowerOFF. Run cmd [{0}]" , cmd));
            Utility.RunCMD(cmd);
        }
       
    }
}
