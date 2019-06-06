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
        /// �ж��Ƿ�Ϊ��ȷ��IP��ַ
        /// </summary>
        /// <param name="strIPadd">��Ҫ�жϵ��ַ���</param>
        /// <param name="localIp">����ip�Ƿ���Ϊ��ȷ��ַ��trueΪ��ȷ</param>
        /// <returns>true = �� false = ��</returns>
        public static bool IsRightIPv4(string strIPadd, bool localIp = false)
        {
            //����������ʽ�ж��ַ����Ƿ����IPv4��ʽ
            if (Regex.IsMatch(strIPadd, "[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}"))
            {
                //����С����ֲ��ַ���
                string[] ips = strIPadd.Split('.');
                if (ips.Length == 4 || ips.Length == 6)
                {
                    //�������IPv4����
                    if (System.Int32.Parse(ips[0]) < 256 && System.Int32.Parse(ips[1]) < 256 & System.Int32.Parse(ips[2]) < 256 & System.Int32.Parse(ips[3]) < 256)
                        //��ȷ
                        if (localIp == false && strIPadd == "127.0.0.1")
                            return false;
                        else
                            return true;
                    //���������
                    else
                        //����
                        return false;
                }
                else
                    //����
                    return false;
            }
            else
                //����
                return false;
        }

        /// <summary>
        /// �õ�����IP
        /// </summary>
        public static string GetLocalIP()
        {
            //����IP��ַ
            string strLocalIP = "";
            //�õ��������
            string strPcName = Dns.GetHostName();
            //�õ�����IP��ַ����
            IPHostEntry ipEntry = Dns.GetHostEntry(strPcName);
            //��������
            foreach (var IPadd in ipEntry.AddressList)
            {
                //�жϵ�ǰ�ַ����Ƿ�Ϊ��ȷIP��ַ
                if (IsRightIPv4(IPadd.ToString()))
                {
                    //�õ�����IP��ַ
                    strLocalIP = IPadd.ToString();
                    //����ѭ��
                    break;
                }
            }

            //���ر���IP��ַ
            return strLocalIP;
        }

        /// <summary>
        /// ����Pingָ��IP�Ƿ��ܹ�Pingͨ
        /// </summary>
        /// <param name="strIP">ָ��IP</param>
        /// <returns>true �� false ��</returns>
        public static bool IsPingIP(string strIP)
        {
            try
            {
                //����Ping����
                Ping ping = new Ping();
                //����Ping����ֵ
                PingReply reply = ping.Send(strIP, 1000);
                if (reply.Status == IPStatus.Success)
                    //Pingͨ
                    return true;
                else
                    return false;
            }
            catch
            {
                //Pingʧ��
                return false;
            }
        }

        public static string GetGatewayIP()
        {
            string strGateway = "";
            //��ȡ��������
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            //��������
            foreach (var netWork in nics)
            {

                //����������IP����
                IPInterfaceProperties ip = netWork.GetIPProperties();

                //��ȡ��IP���������
                GatewayIPAddressInformationCollection gateways = ip.GatewayAddresses;
                foreach (var gateWay in gateways)
                {
                    //����ܹ�Pingͨ����
                    if (IsPingIP(gateWay.Address.ToString()) && gateWay.Address.ToString() != "127.0.0.1")
                    {
                        //�õ����ص�ַ
                        strGateway = gateWay.Address.ToString();
                        //����ѭ��
                        break;
                    }
                }

                //����Ѿ��õ����ص�ַ
                if (strGateway.Length > 0)
                {
                    //����ѭ��
                    break;
                }
            }

            //�������ص�ַ
            return strGateway;
        }

        public static string RunCMD(string cmd)
        {
            if (cmd == "") return "";
            Process myProcess = new Process();
            myProcess.StartInfo.FileName = "cmd.exe";
            myProcess.StartInfo.UseShellExecute = false;//�Ƿ�ʹ�ò���ϵͳshell����
            myProcess.StartInfo.RedirectStandardInput = true;//�������Ե��ó����������Ϣ
            myProcess.StartInfo.RedirectStandardOutput = true;//�ɵ��ó����ȡ�����Ϣ
            myProcess.StartInfo.RedirectStandardError = true;//�ض����׼�������
            myProcess.StartInfo.CreateNoWindow = true;//����ʾ���򴰿�
            myProcess.Start();
            myProcess.StandardInput.WriteLine(cmd + "&exit");
            //���׼����д��Ҫִ�е��������ʹ��&������������ķ��ţ���ʾǰ��һ��������Ƿ�ִ�гɹ���ִ�к���(exit)��������ִ��exit����������ReadToEnd()���������
            //ͬ��ķ��Ż���&&��||ǰ�߱�ʾ����ǰһ������ִ�гɹ��Ż�ִ�к����������߱�ʾ����ǰһ������ִ��ʧ�ܲŻ�ִ�к��������
            myProcess.StandardInput.AutoFlush = true;
            //��ȡcmd���ڵ������Ϣ
            string output = myProcess.StandardOutput.ReadToEnd();
            myProcess.WaitForExit();//�ȴ�����ִ�����˳�����
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
            //��÷��񼯺�
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
                //�汾����ʾΪ�����汾��.�ΰ汾��.�ڲ��汾��.ר�ò����š���
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