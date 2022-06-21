using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PfizerVPNTest
{
    public static class Logging
    {
        private static string LogPath { get; set; }

        private static string LocalLogPath { get; set; }
        private static string OfflineLogPath { get; set; }

        public static void WriteCapture(PulseCapture capture)
        {
            string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            LogPath = string.Format("{0}\\DigitalPulse", AppDataPath);

            // create log directory if it doesn't exist
            Directory.CreateDirectory(LogPath);

            LocalLogPath = String.Format("{0}\\DigitalPulseLog-{1}.txt", LogPath, capture.TimeStamp.ToString("yyyyMM"));
            OfflineLogPath = String.Format("{0}\\DigitalPulseLogOffline.txt", LogPath, capture.TimeStamp.ToString("yyyyMM"));

            // step 1: write to local log file
            WriteToLocalLog(capture);

            // step 2: send events to CloudWatch, cache locally if that fails
            // create list of current event and any pending events in offline cache
            List<InputLogEvent> logEvents = new List<InputLogEvent>();
            logEvents.AddRange(CreateInputLogEvents(new List<PulseCapture>() { capture }));

            List<PulseCapture> cachedPulseCaptures = ReadOfflineLog();
            int cacheCount = cachedPulseCaptures.Count;

            if (cacheCount > 0)
            {
                List<InputLogEvent> cachedLogEvents = CreateInputLogEvents(cachedPulseCaptures);
                logEvents.AddRange(cachedLogEvents);
                logEvents = logEvents.OrderBy(x => x.Timestamp).ToList();
            }

            // send list to 
            Credentials credentials = AWSBroker.GetCloudWatchCredentials();
            // if credentials failed, write to offline log
            if (credentials == null || credentials.State != CredentialsState.obtained)
            {
                WriteToOfflineLog(capture);
            }
            else
            {
                //bool cwSuccess = SendLogEventsToCloudWatch(logEvents, credentials);

                var task = Task.Run(() => SendLogEventsToCloudWatch(logEvents, credentials));
                if (task.Wait(TimeSpan.FromSeconds(10)))
                {
                    if (cacheCount > 0) ClearOfflineLog();
                }

                // if success, clear cache
                //if (cwSuccess)
                //{
                //    if (cwSuccess && cacheCount > 0) ClearOfflineLog();
                //}
                // else if failure, write to offline log
                else WriteToOfflineLog(capture);
            }
        }

        

        public static bool SendLogEventsToCloudWatch(List<InputLogEvent> logEvents, Credentials credentials)
        {
            //string s1 = "AKIA42L4NUXPPWJQXHOZ";
            //string s2 = "l7iIHnc32GSTXXw+lAMsMkXThvRYSjQwMM5Jw4Ai";
            string s1 = credentials.s1;
            string s2 = credentials.s2;
            string ForRegion = "us-east-1";

            using (AmazonCloudWatchLogsClient cloudWatchClient = new AmazonCloudWatchLogsClient(s1, s2, Amazon.RegionEndpoint.GetBySystemName(ForRegion)))
            {
                int retry = 0; bool success = false;
                string sequenceToken = "";
                DescribeLogStreamsRequest dReq = new DescribeLogStreamsRequest("anonCloudWatch");

                do
                {
                    retry++;
                    try
                    {
                        DescribeLogStreamsResponse dRes = cloudWatchClient.DescribeLogStreams(dReq);
                        if (dRes.HttpStatusCode == System.Net.HttpStatusCode.OK)
                        {
                            LogStream stream = dRes.LogStreams.SingleOrDefault(x => x.LogStreamName.ToLower() == "digitalpulse");
                            if (stream != null) success = true;
                            sequenceToken = stream.UploadSequenceToken;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteErrorToLocalLog(ex);
                        return false;
                    }

                    Thread.Sleep(1000);

                } while (retry <= 10 & !success);


                try
                {
                    PutLogEventsRequest plReq = new PutLogEventsRequest("anonCloudWatch", "digitalPulse", logEvents)
                    {
                        SequenceToken = sequenceToken
                    };

                    PutLogEventsResponse plRes = cloudWatchClient.PutLogEvents(plReq);

                    if (plRes.HttpStatusCode.Equals(System.Net.HttpStatusCode.OK)) return true;
                    else return false;
                }
                catch (Exception ex)
                {
                    WriteErrorToLocalLog(ex);
                    return false;
                }
            }
        }

        //public List<InputLogEvent> CreateInputLogEvents(PulseCapture capture)
        //{
        //    List<InputLogEvent> logEvents = new List<InputLogEvent>();
        //    InputLogEvent thisEvent = new InputLogEvent
        //    {
        //        Timestamp = capture.TimeStamp,
        //        Message = JsonConvert.SerializeObject(capture)
        //    };
        //    logEvents.Add(thisEvent);

        //    return logEvents;
        //}

        public static List<InputLogEvent> CreateInputLogEvents(List<PulseCapture> captures)
        {
            List<InputLogEvent> logEvents = new List<InputLogEvent>();

            foreach (PulseCapture capture in captures)
            {
                logEvents.Add(new InputLogEvent
                {
                    Timestamp = capture.TimeStamp,
                    Message = JsonConvert.SerializeObject(capture)
                });
            }

            return logEvents;
        }

        #region File I/O
        public static void WriteToLocalLog(PulseCapture capture)
        {
            // write to local log file - "DigitalPulseLog-YYYYMM.txt"
            using (StreamWriter sw = File.AppendText(LocalLogPath))
            {
                sw.WriteLine(JsonConvert.SerializeObject(capture));
            }
        }

        public static void WriteErrorToLocalLog(Exception ex)
        {
            string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            LogPath = string.Format("{0}\\DigitalPulse", AppDataPath);

            // create log directory if it doesn't exist
            Directory.CreateDirectory(LogPath);

            // write to local log file - "DigitalPulseLog-YYYYMM.txt"
            LocalLogPath = String.Format("{0}\\DigitalPulseLog-{1}.txt", LogPath, DateTime.Now.ToString("yyyyMM"));

            using (StreamWriter sw = File.AppendText(LocalLogPath))
            {
                sw.WriteLine(JsonConvert.SerializeObject(ex));
            }
        }

        public static void WriteToOfflineLog(PulseCapture capture)
        {
            // write to offline log file
            using (StreamWriter sw = File.AppendText(OfflineLogPath))
            {
                sw.WriteLine(JsonConvert.SerializeObject(capture));
            }
        }

        public static List<PulseCapture> ReadOfflineLog()
        {
            try
            {
                // read offline log file into List<PulseCapture>
                using (StreamReader sr = new StreamReader(OfflineLogPath))
                {
                    List<PulseCapture> pendingCaptures = new List<PulseCapture>();
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        pendingCaptures.Add(JsonConvert.DeserializeObject<PulseCapture>(line));
                    }

                    return pendingCaptures;
                }
            }
            catch (Exception ex)
            {
                Logging.WriteErrorToLocalLog(ex);
                return new List<PulseCapture>();
            }
        }

        public static void ClearOfflineLog()
        {
            // clear offline log file
            File.WriteAllText(OfflineLogPath, string.Empty);
        }
        #endregion
    }
}
