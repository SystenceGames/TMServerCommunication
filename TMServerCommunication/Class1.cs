using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;
using RGiesecke.DllExport;
using Newtonsoft.Json;
using System.Net;
using System.IO;

namespace TMServerCommunication
{
    public static class TMServerComm
    {
        static string m_baseURL = "http://platform.maestrosgame.com:1337/v1/";
        
        [DllExport("HostGame", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        public static string HostGame([MarshalAs(UnmanagedType.LPWStr)] String json)
        {
            
            var result = "";
            result =  Post(json, WebRequest.Create("http://google.com"));
            

            return result;
        }

        static public string Post(string json, WebRequest webRequest)
        {
            var result = "";

            HttpWebRequest request;
            try
            {
                request = (HttpWebRequest)WebRequest.Create("some url");
            }
            catch (Exception e)
            {
                return "404";
            }
            request.ContentType = "text/json";
            request.Method = "POST";

            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();
            }
            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (Exception e)
            {
                return "responseFailed";
            }
            using (var streamReader = new StreamReader(response.GetResponseStream()))
            {
                result = streamReader.ReadToEnd();
            }
            return result;
        }

        [DllExport("Get", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        static public string HTTPGet(string urlEndQuery)
        {
            return Get(new WebClient(), urlEndQuery);
        }

        static public string Get(WebClient web, string urlEndQuery)
        {
            string result = "";

            try
            {
                result = web.DownloadString(m_baseURL + urlEndQuery);
            }
            catch (Exception e)
            {
                return "404";
            }

            return result;
        }


        [DllExport("GetGameList", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        public static string GetGameList()
        {
            string result = "";
            result = Get(new WebClient(), "something");
            return result;
        }
    }
}
