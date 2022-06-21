using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PfizerVPNTest
{
    public class PulseCapture
    {
        public ConfigurationObject Settings { get; set; }
        public DateTime TimeStamp { get; set; }
        public string UserName { get; set; }
        public string MachineName { get; set; }
        public bool ExternalNetworkConnection { get; set; }
        public bool VPNConnection { get; set; }
        public bool InternalNetworkConnection { get; set; }
        //public TimeSpan UpTime { get; set; }

        public PulseCapture() { }

        public void Refresh()
        {
            //Settings.Refresh();
            Configuration.Refresh();
            Settings = Configuration.ReadAppDataConfig();

            TimeStamp = DateTime.Now;
            UserName = Environment.UserName;
            MachineName = Environment.MachineName;
            ExternalNetworkConnection = CheckExternalNetwork();
            VPNConnection = CheckVPN(Settings.VPNURL);
            InternalNetworkConnection = CheckInternalNetwork(Settings.InternalURL);
            //UpTime = GetUpTime();

            Logging.WriteCapture(this);
        }

        public void Test(string username, string machinename, bool extnetconn, bool vpnconn, bool intnetconn, TimeSpan uptime)
        {
            TimeStamp = DateTime.Now;
            UserName = username;
            MachineName = machinename;
            ExternalNetworkConnection = extnetconn;
            VPNConnection = vpnconn;
            InternalNetworkConnection = intnetconn;
            //UpTime = uptime;

            //LocalLogPath = String.Format("logs/DigitalPulseLog-{0}.txt", TimeStamp.ToString("yyyyMM"));
            //OfflineLogPath = "logs/DigitalPulseLogOffline.txt";

            // functionality to test

            // write current event to local log


            // create List<InputLogEvent> from current event


            // send List<InputLogEvent> to CloudWatch


            // read offline log into List<InputLogEvent>


            // clear offline log file


            // write to offline log
        }

        #region Network and Machine Checks
        public bool CheckExternalNetwork()
        {
            return NetworkInterface.GetIsNetworkAvailable();
        }

        public bool CheckVPN(string VPNURL)
        {
            try
            {
                string PfizerVPN = VPNURL;

                Ping ping = new Ping();
                PingReply reply = ping.Send(PfizerVPN, 1000);
                if (reply != null)
                {
                    if (reply.Status == IPStatus.Success)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Logging.WriteErrorToLocalLog(ex);
                //Console.WriteLine(ex.StackTrace);
            }

            return false;
        }

        public bool CheckInternalNetwork(string InternalURL)
        {
            try
            {
                string PfizerSite = InternalURL;

                Ping ping = new Ping();
                PingReply reply = ping.Send(PfizerSite, 1000);
                if (reply != null)
                {
                    if (reply.Status == IPStatus.Success)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Logging.WriteErrorToLocalLog(ex);
                //Console.WriteLine(ex.StackTrace);
            }

            return false;
        }

        public TimeSpan GetUpTime()
        {
            using (var uptime = new PerformanceCounter("System", "System Up Time"))
            {
                uptime.NextValue();
                return TimeSpan.FromSeconds(uptime.NextValue());
            }
        }
        #endregion
    }
}
