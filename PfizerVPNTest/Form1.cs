using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PfizerVPNTest
{
    public partial class Form1 : Form
    {
        //public static string Mode = ConfigurationManager.AppSettings["Mode"];
        //public static string DebugStep = ConfigurationManager.AppSettings["DebugStep"];

        //public static string VPNURL = ConfigurationManager.AppSettings["VPNURL"];
        //public static string InternalURL = ConfigurationManager.AppSettings["InternalURL"];

        //public bool debug = false;
        //public string debugStep = "internet";

        public int timerCount = 0;
        public bool checkDone = false;
        //public bool externalNetworkConnection = false;
        //public bool vpnConnection = false;
        //public bool internalNetworkConnection = false;
        //public TimeSpan upTime = new TimeSpan();

        string errorStatus_ExternalNetwork = "Status: Not Connected to a Network";
        string errorMessage_ExternalNetwork = "Try restarting your PC and router. If that is unsuccessful, contact your Internet provider.";

        string errorStatus_VPN = "Status: Not Connected to the Internet";
        string errorMessage_VPN = "Try restarting your router. If that is unsuccessful, contact your Internet provider.";

        string errorStatus_InternalNetwork = "Status: Not Connected to the Pfizer Network";
        string errorMessage_InternalNetwork = "Ensure that your AnyConnect client is connected.";

        PulseCapture capture = new PulseCapture();

        public Form1()
        {
            InitializeComponent();
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MinimizeBox = false;
            MaximizeBox = false;

            //Button_Retry.Visible = false;
            PictureBox_Loading1.Visible = false;
            PictureBox_Loading2.Visible = false;
            PictureBox_Loading3.Visible = false;

            Label_Connected.Visible = false;
            Label_Error1.Visible = false;
            Label_ErrorMessage.Visible = false;
            Label_UpTime.Visible = false;

            Label_PleaseVisit.Visible = false;
            LinkLabel_DigitalOnDemand.Visible = false;

            Label_DebugMode.Visible = false;

            Label_IP.Visible = false;
            //Label_IP.Text = "IP Address: " + GetIP();

            //var bResponse = nSecBrokerClient.GetCredentials();
            //nSecCredentials tCredentials = bResponse.Result;

            // code to run capture on startup
            //capture.Refresh();
            //checkDone = true;
            //timerCount = 0;

            //if (Mode.Equals("prod"))
            //{
            //    capture.Refresh();
            //    checkDone = true;
            //    timerCount = 0;
            //}
            //else if (Mode.Equals("test"))
            //{

            //}

            //VPNTest();
        }

        //public string GetIP()
        //{
        //    IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
        //    foreach (IPAddress ip in host.AddressList)
        //    {
        //        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        //        {
        //            return ip.ToString();
        //        }
        //    }
        //    return "invalid";
        //}

        //public void VPNTest()
        //{
        //    //if (!debug)
        //    //{
        //    //    // Step 1 - Check for network connection
        //    //    externalNetworkConnection = CheckExternalNetwork();

        //    //    // Step 2 - Check connection to Pfizer VPN gateway (anyconnect.pfizer.com)
        //    //    vpnConnection = CheckVPN();

        //    //    // Step 3 - Check connection to internal network (world.pfizer.com)
        //    //    internalNetworkConnection = CheckInternalNetwork();

        //    //    upTime = GetUpTime();
        //    //}
        //    //else
        //    //{
        //    //    if (debugStep.Equals("router"))
        //    //    {
        //    //        externalNetworkConnection = false;
        //    //        vpnConnection = false;
        //    //        internalNetworkConnection = false;
        //    //    }
        //    //    else if (debugStep.Equals("internet"))
        //    //    {
        //    //        externalNetworkConnection = true;
        //    //        vpnConnection = false;
        //    //        internalNetworkConnection = false;
        //    //    }
        //    //    else if (debugStep.Equals("pfizer"))
        //    //    {
        //    //        externalNetworkConnection = true;
        //    //        vpnConnection = true;
        //    //        internalNetworkConnection = false;
        //    //    }
        //    //    else if (debugStep.Equals("connected"))
        //    //    {
        //    //        externalNetworkConnection = true;
        //    //        vpnConnection = true;
        //    //        internalNetworkConnection = true;
        //    //    }
        //    //}

        //    checkDone = true;
        //    timerCount = 0;
        //}

        //public bool CheckExternalNetwork()
        //{
        //    return NetworkInterface.GetIsNetworkAvailable();
        //}

        //public bool CheckVPN()
        //{
        //    try
        //    {
        //        string PfizerVPN = VPNURL;

        //        Ping ping = new Ping();
        //        PingReply reply = ping.Send(PfizerVPN, 1000);
        //        if (reply != null)
        //        {
        //            if (reply.Status == IPStatus.Success)
        //            {
        //                return true;
        //            }
        //        }
        //        return false;
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.StackTrace);
        //    }

        //    return false;
        //}

        //public bool CheckInternalNetwork()
        //{
        //    try
        //    {
        //        string PfizerSite = InternalURL;

        //        Ping ping = new Ping();
        //        PingReply reply = ping.Send(PfizerSite, 1000);
        //        if (reply != null)
        //        {
        //            if (reply.Status == IPStatus.Success)
        //            {
        //                return true;
        //            }              
        //        }
        //        return false;
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.StackTrace);
        //    }

        //    return false;
        //}

        //public TimeSpan GetUpTime()
        //{
        //    using (var uptime = new PerformanceCounter("System", "System Up Time"))
        //    {
        //        uptime.NextValue();
        //        return TimeSpan.FromSeconds(uptime.NextValue());
        //    }
        //}

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (checkDone && timerCount <= 2)
            {
                switch (timerCount)
                {
                    case 0:
                        if (capture.ExternalNetworkConnection)
                        {
                            Panel_Step1.BackColor = Color.Green;
                            PictureBox_Loading1.Visible = false;
                        }
                        else
                        {
                            Panel_Step1.BackColor = Color.Red;
                            Label_Error1.Text = errorStatus_ExternalNetwork;
                            Label_Error1.Visible = true;
                            Label_ErrorMessage.Text = errorMessage_ExternalNetwork;
                            Label_ErrorMessage.Visible = true;
                            Button_Retry.Visible = true;
                            PictureBox_Loading1.Visible = false;
                            PictureBox_Loading2.Visible = false;
                            PictureBox_Loading3.Visible = false;
                            timerCount = 3;
                        }
                        break;
                    case 1:
                        if (capture.VPNConnection || capture.InternalNetworkConnection)
                        {
                            Panel_Step2.BackColor = Color.Green;
                            PictureBox_Loading2.Visible = false;
                        }
                        else
                        {
                            Panel_Step2.BackColor = Color.Red;
                            Label_Error1.Text = errorStatus_VPN;
                            Label_Error1.Visible = true;
                            Label_ErrorMessage.Text = errorMessage_VPN;
                            Label_ErrorMessage.Visible = true;
                            Button_Retry.Visible = true;
                            PictureBox_Loading1.Visible = false;
                            PictureBox_Loading2.Visible = false;
                            PictureBox_Loading3.Visible = false;
                            timerCount = 3;
                        }
                        break;
                    case 2:
                        if (capture.InternalNetworkConnection)
                        {
                            Panel_Step3.BackColor = Color.Green;
                            PictureBox_Loading3.Visible = false;
                            Label_Connected.Visible = true;
                            Button_Retry.Visible = true;
                        }
                        else
                        {
                            Panel_Step3.BackColor = Color.Red;
                            Label_Error1.Text = errorStatus_InternalNetwork;
                            Label_Error1.Visible = true;
                            Label_ErrorMessage.Text = errorMessage_InternalNetwork;
                            Label_ErrorMessage.Visible = true;
                            Label_PleaseVisit.Visible = true;
                            LinkLabel_DigitalOnDemand.Visible = true;
                            Button_Retry.Visible = true;
                            PictureBox_Loading1.Visible = false;
                            PictureBox_Loading2.Visible = false;
                            PictureBox_Loading3.Visible = false;
                            timerCount = 3;
                        }
                        //Label_UpTime.Text = "PC last restarted " + capture.UpTime.TotalDays + " days ago.";
                        //Label_UpTime.Visible = true;
                        break;
                }

                timerCount++;
            }
        }

        private void Button_Retry_Click(object sender, EventArgs e)
        {
            Button_Retry.Visible = false;
            //Label_IP.Text = "IP Address: " + GetIP();
            //VPNTest();

            Panel_Step1.BackColor = Color.Gray;
            Panel_Step2.BackColor = Color.Gray;
            Panel_Step3.BackColor = Color.Gray;
            PictureBox_Loading1.Visible = true;
            PictureBox_Loading2.Visible = true;
            PictureBox_Loading3.Visible = true;
            Label_Connected.Visible = false;
            Label_Error1.Visible = false;
            Label_ErrorMessage.Visible = false;
            Label_UpTime.Visible = false;
            Label_PleaseVisit.Visible = false;
            LinkLabel_DigitalOnDemand.Visible = false;

            capture.Refresh();
            checkDone = true;
            timerCount = 0;
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void LinkLabel_DigitalOnDemand_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://digitalondemand.pfizer.com");
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
