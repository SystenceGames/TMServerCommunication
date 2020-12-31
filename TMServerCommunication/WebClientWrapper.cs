using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace TMServerCommunication
{
    public class WebClientWrapper 
    {
        WebClient webclient;
        const int MAX_NUM_CONNECTIONS = 10;

        public WebClientWrapper()
        {
            this.webclient = new WebClient();
            webclient.UploadStringCompleted += TMServerComm.PostRequestAsyncCompleted;
        }

        public virtual string DownloadString(string address)
        {
            return webclient.DownloadString(address);
        }

        public virtual void UploadStringAsync(Uri address, string method, string data, Object userToken)
        {
            webclient.UploadStringAsync(address, "POST", data, userToken);
        }

        /// <summary>
        /// Fire and forget method for sending up data via HTTP 1.0
        /// (used for graylog2 analytics server in TheMaestros)
        /// </summary>
        /// <param name="address">full url string</param>
        /// <param name="payload">Payload to be added as the POST body</param>
        /// <returns>no return, it fires and forgets</returns>
        public virtual void HTTP10PostNoResponse(string address, string payload)
        {
            if (ServicePointManager.DefaultConnectionLimit < MAX_NUM_CONNECTIONS)
            {
                ServicePointManager.DefaultConnectionLimit = MAX_NUM_CONNECTIONS;
            }

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);
            request.Accept = "application/json";
            request.ContentType = "application/json";
            request.Method = "POST";
            request.ProtocolVersion = HttpVersion.Version10;
            request.KeepAlive = true;

            Byte[] bytes = Encoding.ASCII.GetBytes(payload);
            request.ContentLength = bytes.Length;

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(bytes, 0, bytes.Length);
                requestStream.Flush();
                requestStream.Close();
            }
        }

        public virtual void setContentType(string contentType)
        {
            webclient.Headers.Set(HttpRequestHeader.ContentType, contentType);
        }
    }
}
