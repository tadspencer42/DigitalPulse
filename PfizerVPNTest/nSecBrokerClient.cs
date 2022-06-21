using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace PfizerVPNTest
{
    internal class nSecBrokerRequestAPI
    {
        internal string nSecAuthor { get; set; }
        internal string nSecRequest { get; set; }
        internal string nSecSignature { get; set; }

        internal string ToJson()
        {
            return string.Format(
                "{{\"nSecAuthor\":\"{0}\",\"nSecRequest\":\"{1}\",\"nSecSignature\":\"{2}\"}}",
                this.nSecAuthor, this.nSecRequest, this.nSecSignature);
        }
    }

    public enum nSecCredentialsState { none, obtained, notObtained }
    public class nSecCredentials
    {
        public string s1 { get; internal set; }
        public string s2 { get; internal set; }
        public string s3 { get; internal set; }
        public string ExpiryUTC { get; internal set; }

        public Boolean Expired
        {
            get { return DateTime.UtcNow.Subtract(DateTime.Parse(ExpiryUTC)).TotalSeconds > 0; }
        }

        public nSecCredentialsState State { get; internal set; }

        // Enabling removal of NewtonSoft
        internal void DeSerialize(string szInstance, string JugglingKey)
        {
            // Remove JSON delimeters
            string content = szInstance.Replace("{", string.Empty);
            content = content.Replace("}", string.Empty);

            string[] contentFields = content.Split(',');
            if (contentFields.Length != 4) return;

            s1 = contentFields[0].Replace("\"", string.Empty);
            s2 = contentFields[1].Replace("\"", string.Empty);
            s3 = contentFields[2].Replace("\"", string.Empty);
            ExpiryUTC = contentFields[3].Replace("\"", string.Empty);

            s1 = vdodJuggler.InTheHand(s1.Substring(s1.IndexOf(":") + 1), JugglingKey);
            s2 = vdodJuggler.InTheHand(s2.Substring(s2.IndexOf(":") + 1), JugglingKey);
            s3 = vdodJuggler.InTheHand(s3.Substring(s3.IndexOf(":") + 1), JugglingKey);
            ExpiryUTC = ExpiryUTC.Substring(ExpiryUTC.IndexOf(":") + 1);
        }

        internal nSecCredentials() { this.State = nSecCredentialsState.none; }
    }

    internal static class nSecBrokerClient
    {
        //public static string API_ENDPOINT = ConfigurationManager.AppSettings["NSecURL"];
        //private static string API_ENDPOINT = "https://y6e548qij0.execute-api.us-east-1.amazonaws.com/Production";
        //private static string API_ENDPOINT = "https://y6e548qij0.execute-api.us-east-1.amazonaws.com/PTR";

        public async static Task<nSecCredentials> GetCredentials()
        {
            ConfigurationObject config = Configuration.ReadAppDataConfig();
            string API_ENDPOINT = config.NSecURL;

            LastException = null;
            nSecCredentials credentials = new nSecCredentials();
            DateTime startTime = DateTime.UtcNow;

            try
            {
                try
                {
                    HttpClient nSecClient = new HttpClient();
                    nSecClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    Uri nSecClientEndPoint = new Uri(API_ENDPOINT);

                    nSecBrokerRequestAPI request = new nSecBrokerRequestAPI();
                    request.nSecAuthor = Environment.MachineName.ToUpper();

                    string jKey = typeof(nSecBrokerRequestAPI).ToString();
                    jKey = string.Format("{0}{1}", jKey.Substring(jKey.IndexOf(".")), request.nSecAuthor);

                    request.nSecRequest = vdodJuggler.InTheAir(request.nSecAuthor, jKey);
                    request.nSecSignature = vdodJuggler.InTheAir(DateTime.UtcNow.ToString(CultureInfo.InvariantCulture), jKey);

                    string sRequest = request.ToJson();

                    HttpResponseMessage response = await nSecClient.PostAsync(
                        nSecClientEndPoint,
                        new StringContent(sRequest, Encoding.UTF8, "application/json")).ConfigureAwait(false);

                    string rawResponseContent = await response.Content.ReadAsStringAsync();

                    credentials.DeSerialize(rawResponseContent, jKey);

                    credentials.State = credentials.s1 != null && credentials.s2 != null ?
                        nSecCredentialsState.obtained : nSecCredentialsState.notObtained;
                }
                catch (System.Exception ex)
                {
                    LastException = ex;
                    credentials.State = nSecCredentialsState.notObtained;
                }
            }
            finally { LastRequestDuration = (Int32)DateTime.UtcNow.Subtract(startTime).TotalSeconds; }

            return credentials;
        }

        public static Exception LastException { get; private set; }
        public static Int32 LastRequestDuration { get; private set; }
    }

    internal static class vdodJuggler
    {
        private static string p2(string Content)
        {
            Int32 cHeaderSize = Int32.Parse(Content.Substring(0, 4));
            string cHeader = Content.Substring(4, cHeaderSize);
            string cData = Content.Substring(cHeader.Length + 4);

            string[] cFields = new string[cHeaderSize / 4];

            Int32 dPos = 0;
            for (Int32 hPos = 0; hPos < cHeader.Length - 3; hPos += 4)
            {
                Int32 fIndex = Int32.Parse(cHeader.Substring(hPos, 2));
                Int32 fLength = Int32.Parse(cHeader.Substring(hPos + 2, 2));

                cFields[fIndex] = cData.Substring(dPos, fLength);
                dPos += fLength;
            }

            return String.Join("|", cFields);
        }
        private static string p1(string Content)
        {
            string[] fields = Content.Split('|');
            Dictionary<Int32, string> tTest = new Dictionary<int, string> { };

            Random fChooser = new Random();
            do
            {
                for (Int32 fPos = 0; fPos < fields.Length; fPos++)
                {
                    if (tTest.ContainsKey(fPos) || ((fChooser.Next(0, 10) % 2) == 0)) continue;
                    tTest.Add(fPos, fields[fPos]); break;
                }
            } while (tTest.Count < fields.Length);

            string cHeader = ""; string cData = "";
            foreach (Int32 fPos in tTest.Keys)
            {
                cHeader = string.Format("{0}{1}{2}", cHeader, fPos.ToString("d2"), tTest[fPos].Length.ToString("d2"));
                cData = string.Format("{0}{1}", cData, tTest[fPos]);
            }

            return string.Format("{0}{1}{2}", cHeader.Length.ToString("d4"), cHeader, cData);
        }

        private static Int32 JugglingSeed(string Key)
        {
            Int32 jSeed = 0;
            foreach (char tChar in Key) { jSeed += Convert.ToInt32((byte)tChar); }
            return jSeed;
        }
        internal static string InTheAir(string Content, string Key)
        {
            Int32 jSeed = JugglingSeed(Key);
            Random sGenerator = new Random(jSeed);
            string aContent = "";
            foreach (char tChar in Content)
            {
                aContent = string.Format("{0}|{1}", aContent,
                    (sGenerator.Next(1, 255) + Convert.ToInt32((byte)tChar)));
            }
            return p1(aContent.Substring(1));
        }
        internal static string InTheHand(string Content, string Key)
        {
            Dictionary<string, string> eventAttributes = new Dictionary<string, string>
            { { "synopsis", "vdodJuggler.InTheHand" },
              { "content", Content },
              { "key", Key } };

            string p2Content = "";
            try
            {
                p2Content = p2(Content);
                eventAttributes.Add("p2", p2Content);
            }
            catch (System.Exception ex)
            {
                eventAttributes.Add("p2Exception", ex.ToString());
                //LambdaLogger.Log(JsonSerializer.Serialize(new lLogEvent(eventAttributes, VLE_SEVERITY.trace)));

                return null;
            }

            Int32 jSeed = JugglingSeed(Key);
            eventAttributes.Add("jugglingSeed", jSeed.ToString());
            Random sGenerator = new Random(jSeed);

            string[] jFields = p2Content.Split('|');
            string hContent = "";
            foreach (string jField in jFields)
            {
                Int32 jVal = ((Int32.Parse(jField)) - sGenerator.Next(1, 255));
                hContent = string.Format("{0}{1}", hContent, (char)Convert.ToByte(jVal));
            }

            eventAttributes.Add("h2", hContent);
            //LambdaLogger.Log(JsonSerializer.Serialize(new lLogEvent(eventAttributes, VLE_SEVERITY.trace)));

            return hContent;
        }
    }
}
