using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TMServerCommunication;
using System.Reflection;
using System.Diagnostics;
using Moq;
using Moq.Properties;

namespace TMServerCommunicationTest
{
    /// <summary>
    /// Summary description for TMServerCommunicationFT
    /// </summary>
    [TestClass]
    public class TMServerCommunicationFT
    {
        const string platformBaseURL = "http://maestros-lobbies.cloudapp.net:10200/v1/";
        const int MAX_GAME_PLAYERS = 9;
        const int SPAM_NUMBER = 100;

        [TestMethod]
        public void RetHiDLL()
        {
            // this is bad, but if you change this to your path, it will work
            var DLL = Assembly.LoadFile(@"C:\TheMaestros\TheMaestros\Binaries\Win32\UserCode\TMServerCommunication.dll");

            var theType = DLL.GetType("TMServerCommunication.TMServerComm");
            var methods = theType.GetMethods();
            string str = (string)methods[0].Invoke(theType, null);
            int ret = string.Compare(str, "Hi");
            Assert.AreEqual(ret, 0);
        }

        [TestMethod]
        public void HTTPPostAsyncALoginReturnsSessionToken()
        {
            string response = null;
             //This is janky but if you point it at your directory, it will work.
            var DLL = Assembly.LoadFile(@"C:\TheMaestros\TheMaestros\Binaries\Win32\UserCode\TMServerCommunication.dll");
            String url = "https://maestros-accounts-1.cloudapp.net/login";
            string payload = "playerName=TMTMServerCommFT&password=letmein";
            object[] parameters = new object[2] { url, payload };

            var theType = DLL.GetType("TMServerCommunication.TMServerComm");
            var methods = theType.GetMethods();
            var HTTPPostAsyncMethod = theType.GetMethod("HTTPPostAsync");
            var requestNumber = HTTPPostAsyncMethod.Invoke(theType, parameters);
            
            var GetResponseMethod = theType.GetMethod("GetResponse");
            while (string.IsNullOrEmpty(response))
            {
                response = (string)GetResponseMethod.Invoke(theType, new object[] {requestNumber});
            }

            Assert.IsTrue(response.Contains("sessionToken"));
        }

        [TestMethod]
        public void HTTP10PostNoResponseDoesntFailOnRepeatedCalls()
        {
            /* I acknowledge there is almost no good testing here because the method doesn't verify it's success */
            /* however, I verified it with fiddler and it's getting 202 as expected from Graylog */
            WebClientWrapper webClient = new WebClientWrapper();
            string url = "http://example.com";
            string exampleAnalyticsPayload = "{\"version\": \"1.1\",\"host\": \"example.org\",\"short_message\": \"A short message that helps you identify what is going on\",\"full_message\": \"cats\",\"timestamp\": 1385053862.3072,\"level\": 1,\"_commander_\":\"TinkerMeister\"}";

            for (int i = 0; i < MAX_GAME_PLAYERS; ++i)
            {
                DateTime start = DateTime.Now;
                TMServerComm.HTTP10PostNoResponse(url, exampleAnalyticsPayload);
                DateTime end = DateTime.Now;

                Assert.IsTrue((end - start).TotalSeconds < 30);
            }
        }

        [TestMethod]
        public void SpammingRefreshDoesntCrash()
        {
            WebClientWrapper webClient = new WebClientWrapper();
            string loginURL = platformBaseURL + "login";
            string loginPayload = "playerName=TMServerCommFT&password=letmein";
            int loginReqNumber = -1;
            string loginResponse = "";

            loginReqNumber = TMServerComm.PostAsync(webClient, loginURL, loginPayload);
            while (string.IsNullOrEmpty(loginResponse))
            {
                loginResponse = TMServerComm.GetResponse(loginReqNumber);
            }

            var jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(loginResponse);
            string url = platformBaseURL + "listGames";
            string payload = "playerName=TMServerCommFT&sessionToken=" + jsonResponse["sessionToken"];

            List<int> reqNums = new List<int>();
            List<string> responses = new List<string>();

            for (int i = 0; i < SPAM_NUMBER; ++i)
            {
                reqNums.Add(TMServerComm.HTTPPostAsync(url, payload));
            }

            while (reqNums.Count != 0)
            {
                reqNums.RemoveAll(reqNum => !string.IsNullOrEmpty(TMServerComm.GetResponse(reqNum)));
            }

            // Assert that the function actually finishes >.<
        }

        [TestMethod]
        public void PostRequestAsyncReturnsASessionToken()
        {
            string ret = "";
            int responseNumber = 0;
            WebClientWrapper webClient = new WebClientWrapper();
            String url = platformBaseURL + "login";

            responseNumber = TMServerComm.PostAsync(webClient, url, "playerName=TMServerCommFT&password=letmein");
            while (string.IsNullOrEmpty(ret))
            {
                ret = TMServerComm.GetResponse(responseNumber);
            }

            Assert.IsTrue(ret.Contains("sessionToken"));
        }
    }
}
