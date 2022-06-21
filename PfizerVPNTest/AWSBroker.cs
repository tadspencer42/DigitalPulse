using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PfizerVPNTest
{
    static class AWSBroker
    {
        public static Credentials GetCloudWatchCredentials()
        {
            try
            {
                ConfigurationObject config = Configuration.ReadAppDataConfig();
                IamKeyBrokerClient.EndPoint = config.NSecURL;

                int cAttempt = 0;
                do
                {
                    cAttempt++;
                    Credentials sessionCredentials = IamKeyBrokerClient.GetCredentials().Result;
                    if (sessionCredentials.State == CredentialsState.obtained)
                    {
                        return sessionCredentials;
                    }
                    else { Thread.Sleep(500); }
                } while (cAttempt < 4);

                return null;
            }
            catch (Exception ex)
            {
                Logging.WriteErrorToLocalLog(ex);
                return null;
            }

            //var bResponse = IamKeyBrokerClient.GetCredentials();
            //Credentials tCredentials = bResponse.Result;

            //return tCredentials;
        }

        #region Configuration
        public static string GetConfigFromS3(Credentials credentials)
        {
            string s1 = credentials.s1;
            string s2 = credentials.s2;
            string ForRegion = "us-east-1";

            string responseBody = null;

            using (AmazonS3Client s3Client = new AmazonS3Client(s1, s2, Amazon.RegionEndpoint.GetBySystemName(ForRegion)))
            {
                GetObjectRequest getObjectRequest = new GetObjectRequest();
                getObjectRequest.BucketName = "nsb-lowtrustclient";
                getObjectRequest.Key = "DigitalPulseConfig.json";

                try
                {
                    using (GetObjectResponse getObjectResponse = s3Client.GetObjectAsync(getObjectRequest).Result)
                    {
                        using (StreamReader reader = new StreamReader(getObjectResponse.ResponseStream, Encoding.Default))
                        {
                            responseBody = reader.ReadToEnd();
                            return responseBody;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.WriteErrorToLocalLog(ex);
                }
            }

            return "error";
        }
        #endregion

        #region Logging

        #endregion
    }
}
