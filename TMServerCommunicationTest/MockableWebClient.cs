using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace TMServerCommunicationTest
{
    class MockableWebClient 
    {
        WebClient webclient = new WebClient();

        virtual public string DownloadString(string address)
        {
            return webclient.DownloadString(address);
        }
    }
}
