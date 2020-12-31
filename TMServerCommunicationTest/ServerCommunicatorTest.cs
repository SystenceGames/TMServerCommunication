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
using Newtonsoft.Json;
using System.Net;

namespace TMServerCommunicationTest
{
    [TestClass]
    public class ServerCommunicatorTest
    {
        Mock<WebClientWrapper> webClientMock;
        Mock<WebClientWrapper> postAsyncWebClient;
        string exampleAddress = "http://example.com";
        string examplePayload = "payload";
        private const string EXPECTED_CONTENT_TYPE = "application/x-www-form-urlencoded";

        [TestInitialize()]
        public void Initialize()
        {
            TMServerComm.responseMap.Clear();
            webClientMock = new Mock<WebClientWrapper>(MockBehavior.Strict);
            postAsyncWebClient = new Mock<WebClientWrapper>(MockBehavior.Strict);
            postAsyncWebClient.Setup(item => item.setContentType(EXPECTED_CONTENT_TYPE));
        }

        [TestMethod]
        public void ReturnHiReturnsHi()
        {
            string response = string.Empty;
            response = TMServerComm.ReturnHi();
            int ret = string.Compare(response, "Hi");
            Assert.AreEqual(ret, 0);
        }

        [TestMethod]
        public void GetReturnsExpectedValueFromDownloadString()
        {
            string expected = "Hello";
            string ret = "";

            webClientMock.Setup(foo => foo.DownloadString(exampleAddress)).Returns(expected);

            ret = TMServerComm.Get(webClientMock.Object, exampleAddress);
            
            Assert.AreEqual(ret, expected);
        }

        [TestMethod]
        public void GetResponseDoesntReturnNullIfRequestReturnsNull()
        {
            TMServerComm.responseMap.Add(69, null);
            string response = TMServerComm.GetResponse(69);
            Assert.AreNotEqual(response, null);
        }

        [TestMethod]
        public void GettingAResponseNumThatDoesntExistDoesntPassNull()
        {
            string resp = TMServerComm.GetResponse(10000000);
            Assert.AreNotEqual(resp, null);
        }

        [TestMethod]
        public void PostAsyncDoesntCrashOnBadAddress()
        {
            string badAddress = "badAddress";

            int result = TMServerComm.PostAsync(postAsyncWebClient.Object, badAddress, examplePayload);

            Assert.AreEqual<int>(TMServerComm.nextRequestNumber - 1, result);
            // check for the error showing up in the response?
        }

        [TestMethod]
        public void PostAsyncAddsPrettyErrorMessageToResponseMapOnWebException()
        {
            WebException webException = new WebException();
            postAsyncWebClient.Setup(item => item.UploadStringAsync(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Object>())).Throws(webException);

            int result = TMServerComm.PostAsync(postAsyncWebClient.Object, exampleAddress, examplePayload);

            postAsyncWebClient.Verify(m => m.UploadStringAsync(new Uri(exampleAddress), "POST", examplePayload, It.IsAny<Object>()));
            Assert.AreEqual<string>(TMServerComm.jsonifyErrorMessage(TMServerComm.COULDNT_CONTACT_SERVER_ERROR_MESSAGE), TMServerComm.responseMap[result]);
        }

        [TestMethod]
        public void PostAsyncDoesntCrashOnWebException()
        {
            postAsyncWebClient.Setup(item => item.UploadStringAsync(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Object>())).Throws(new WebException());

            int result = TMServerComm.PostAsync(postAsyncWebClient.Object, exampleAddress, examplePayload);

            postAsyncWebClient.Verify(m => m.UploadStringAsync(new Uri(exampleAddress), "POST", examplePayload, It.IsAny<Object>()));
            Assert.AreEqual<int>(TMServerComm.nextRequestNumber - 1, result); // useless line
        }
    }
}