using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nSecTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var bResponse = nSecBrokerClient.GetCredentials();
            nSecCredentials tCredentials = bResponse.Result;
        }
    }
}
