using Amazon.S3;
using Amazon.S3.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PfizerVPNTest
{
    public class ConfigurationObject
    {
        [JsonConverter(typeof(VersionConverter))]
        public Version Version { get; set; }
        public DateTime LastUpdated { get; set; }
        public string VPNURL { get; set; }
        public string InternalURL { get; set; }
        public string NSecURL { get; set; }

        public ConfigurationObject() { }
    }

    public static class Configuration
    {
        public static void Refresh()
        {
            // read local configuration first from AppData, if it's not there check local directory
            ConfigurationObject localConfiguration = ReadAppDataConfig();
            if (localConfiguration == null)
            {
                localConfiguration = ReadLocalConfig();
                WriteAppDataConfig(localConfiguration);
            }

            // fetch configuration from S3
            //Configuration s3Config = FetchConfigFromS3();
            try
            {
                ConfigurationObject s3Config = new ConfigurationObject();

                var task = Task.Run(() => FetchConfigFromS3());
                if (task.Wait(TimeSpan.FromSeconds(10)))
                {
                    s3Config = task.Result;

                    if (localConfiguration.Version != s3Config.Version)
                    {
                        //Console.WriteLine("New version of config detected: " + s3Config.Version + ", old version: " + localConfiguration.Version);
                        WriteAppDataConfig(s3Config);
                        localConfiguration = s3Config;
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.WriteErrorToLocalLog(ex);
            }
                

            // if S3 connection failed, use local
            // else compare version number and overwrite if necessary
            //if (s3Config != null)
            //{
            //    if (localConfiguration.Version != s3Config.Version)
            //    {
            //        UpdateLocalConfig(s3Config);
            //        localConfiguration = s3Config;
            //    }
            //}

            //Version = localConfiguration.Version;
            //LastUpdated = localConfiguration.LastUpdated;
            //VPNURL = localConfiguration.VPNURL;
            //InternalURL = localConfiguration.InternalURL;
            //NSecURL = localConfiguration.NSecURL;
        }

        public static ConfigurationObject ReadLocalConfig()
        {
            ConfigurationObject localConfig = new ConfigurationObject
            {
                Version = Version.Parse(ConfigurationManager.AppSettings["Version"]),
                LastUpdated = DateTime.Parse(ConfigurationManager.AppSettings["LastUpdated"]),
                VPNURL = ConfigurationManager.AppSettings["VPNURL"],
                InternalURL = ConfigurationManager.AppSettings["InternalURL"],
                NSecURL = ConfigurationManager.AppSettings["NSecURL"]
            };

            return localConfig;
        }

        public static ConfigurationObject ReadAppDataConfig()
        {
            string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string AppDataConfigPath = string.Format("{0}\\DigitalPulse\\Config.json", AppDataPath);

            try
            {
                using (StreamReader sr = new StreamReader(AppDataConfigPath))
                {
                    string line = sr.ReadToEnd();
                    ConfigurationObject AppDataConfig = JsonConvert.DeserializeObject<ConfigurationObject>(line);
                    return AppDataConfig;
                }
            }
            catch (Exception ex)
            {
                Logging.WriteErrorToLocalLog(ex);
                return null;
            }
        }

        public static void WriteAppDataConfig(ConfigurationObject config)
        {
            string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string AppDataConfigPath = string.Format("{0}\\DigitalPulse\\Config.json", AppDataPath);

            try
            {
                using (StreamWriter sw = new StreamWriter(AppDataConfigPath))
                {
                    sw.WriteLine(JsonConvert.SerializeObject(config));
                }
            }
            catch (Exception ex)
            {
                Logging.WriteErrorToLocalLog(ex);
            }
        }

        public static ConfigurationObject FetchConfigFromS3()
        {
            try
            {
                Credentials credentials = AWSBroker.GetCloudWatchCredentials();

                string jsonConfigResponse = AWSBroker.GetConfigFromS3(credentials);

                ConfigurationObject s3Config = JsonConvert.DeserializeObject<ConfigurationObject>(jsonConfigResponse);

                //Version = s3Config.Version;
                //LastUpdated = s3Config.LastUpdated;
                //VPNURL = s3Config.VPNURL;
                //InternalURL = s3Config.InternalURL;
                //NSecURL = s3Config.NSecURL;

                return s3Config;
            }
            catch (Exception ex)
            {
                Logging.WriteErrorToLocalLog(ex);
                return null;
            }
        }

        public static void UpdateLocalConfig(ConfigurationObject newConfig)
        {
            System.Configuration.Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            configuration.AppSettings.Settings["Version"].Value = newConfig.Version.ToString();

            configuration.Save(ConfigurationSaveMode.Full, true);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}
