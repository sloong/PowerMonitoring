using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.ServiceProcess;
using System.Text.RegularExpressions;

namespace Sloong
{
    public static class Utility
    {
        /// <summary>
        /// 判断是否为正确的IP地址
        /// </summary>
        /// <param name="strIPadd">需要判断的字符串</param>
        /// <param name="localIp">本地ip是否作为正确地址，true为正确</param>
        /// <returns>true = 是 false = 否</returns>
        public static bool IsRightIPv4(string strIPadd, bool localIp = false)
        {
            //利用正则表达式判断字符串是否符合IPv4格式
            if (Regex.IsMatch(strIPadd, "[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}"))
            {
                //根据小数点分拆字符串
                string[] ips = strIPadd.Split('.');
                if (ips.Length == 4 || ips.Length == 6)
                {
                    //如果符合IPv4规则
                    if (System.Int32.Parse(ips[0]) < 256 && System.Int32.Parse(ips[1]) < 256 & System.Int32.Parse(ips[2]) < 256 & System.Int32.Parse(ips[3]) < 256)
                        //正确
                        if (localIp == false && strIPadd == "127.0.0.1")
                            return false;
                        else
                            return true;
                    //如果不符合
                    else
                        //错误
                        return false;
                }
                else
                    //错误
                    return false;
            }
            else
                //错误
                return false;
        }

        /// <summary>
        /// 得到本机IP
        /// </summary>
        public static string GetLocalIP()
        {
            //本机IP地址
            string strLocalIP = "";
            //得到计算机名
            string strPcName = Dns.GetHostName();
            //得到本机IP地址数组
            IPHostEntry ipEntry = Dns.GetHostEntry(strPcName);
            //遍历数组
            foreach (var IPadd in ipEntry.AddressList)
            {
                //判断当前字符串是否为正确IP地址
                if (IsRightIPv4(IPadd.ToString()))
                {
                    //得到本地IP地址
                    strLocalIP = IPadd.ToString();
                    //结束循环
                    break;
                }
            }

            //返回本地IP地址
            return strLocalIP;
        }

        /// <summary>
        /// 尝试Ping指定IP是否能够Ping通
        /// </summary>
        /// <param name="strIP">指定IP</param>
        /// <returns>true 是 false 否</returns>
        public static bool IsPingIP(string strIP)
        {
            try
            {
                //创建Ping对象
                Ping ping = new Ping();
                //接受Ping返回值
                PingReply reply = ping.Send(strIP, 1000);
                if (reply.Status == IPStatus.Success)
                    //Ping通
                    return true;
                else
                    return false;
            }
            catch
            {
                //Ping失败
                return false;
            }
        }

        public static string GetGatewayIP()
        {
            string strGateway = "";
            //获取所有网卡
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            //遍历数组
            foreach (var netWork in nics)
            {

                //单个网卡的IP对象
                IPInterfaceProperties ip = netWork.GetIPProperties();

                //获取该IP对象的网关
                GatewayIPAddressInformationCollection gateways = ip.GatewayAddresses;
                foreach (var gateWay in gateways)
                {
                    //如果能够Ping通网关
                    if (IsPingIP(gateWay.Address.ToString()) && gateWay.Address.ToString() != "127.0.0.1")
                    {
                        //得到网关地址
                        strGateway = gateWay.Address.ToString();
                        //跳出循环
                        break;
                    }
                }

                //如果已经得到网关地址
                if (strGateway.Length > 0)
                {
                    //跳出循环
                    break;
                }
            }

            //返回网关地址
            return strGateway;
        }

        public static string RunCMD(string cmd)
        {
            if (cmd == "") return "";
            Process myProcess = new Process();
            myProcess.StartInfo.FileName = "cmd.exe";
            myProcess.StartInfo.UseShellExecute = false;//是否使用操作系统shell启动
            myProcess.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
            myProcess.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
            myProcess.StartInfo.RedirectStandardError = true;//重定向标准错误输出
            myProcess.StartInfo.CreateNoWindow = true;//不显示程序窗口
            myProcess.Start();
            myProcess.StandardInput.WriteLine(cmd + "&exit");
            //向标准输入写入要执行的命令。这里使用&是批处理命令的符号，表示前面一个命令不管是否执行成功都执行后面(exit)命令，如果不执行exit命令，后面调用ReadToEnd()方法会假死
            //同类的符号还有&&和||前者表示必须前一个命令执行成功才会执行后面的命令，后者表示必须前一个命令执行失败才会执行后面的命令
            myProcess.StandardInput.AutoFlush = true;
            //获取cmd窗口的输出信息
            string output = myProcess.StandardOutput.ReadToEnd();
            myProcess.WaitForExit();//等待程序执行完退出进程
            myProcess.Close();
            return output;
        }

        public static string GetServicePath(string service_name)
        {
            try
            {
                RegistryKey regKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\" + service_name);
                string imagePath = regKey.GetValue("ImagePath").ToString();
                regKey.Close();
                imagePath = imagePath.Substring(1, imagePath.Length - 2);
                return imagePath;
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static string GetServiceStatus(string service_name)
        {
            //获得服务集合
            ServiceController sc = new ServiceController(service_name);
            try
            {

                return sc.Status.ToString();
            }
            catch (InvalidOperationException)
            {
                return "";
            }
        }

        public static string GetExeVersionInfo(string path)
        {
            try
            {
                FileVersionInfo file1 = System.Diagnostics.FileVersionInfo.GetVersionInfo(path);
                //版本号显示为“主版本号.次版本号.内部版本号.专用部件号”。
                var FileVersions = String.Format("{0}.{1}.{2}.{3}", file1.FileMajorPart, file1.FileMinorPart, file1.FileBuildPart, file1.FilePrivatePart);
                return FileVersions;
            }
            catch (Exception)
            {
                return "";
            }
        }
    }

    public class RegisterEx
    {
        string _Path;
        RegistryKey _RegKey;

        public RegisterEx( RegistryKey pos, string path)
        {
            _Path = path;
            _RegKey = pos.CreateSubKey(path);
        }

        ~RegisterEx()
        {
            _RegKey.Close();
        }


        public string GetValue(string key, string def)
        {
            try
            {
                var value = _RegKey.GetValue(key);
                if (value == null)
                    return def;
                return value.ToString();
            }
            catch (Exception)
            {
                return def;
            }
        }

        public void SetValue(string key, string value)
        {
            _RegKey.SetValue(key, value);
        }
    }
}