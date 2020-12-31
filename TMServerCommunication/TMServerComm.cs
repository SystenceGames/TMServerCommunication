using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;
using RGiesecke.DllExport;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace TMServerCommunication
{
    public static class TMServerComm
    {
        public static string GENERIC_ERROR_MESSAGE = "Error communicating with server";
        public static string COULDNT_CONTACT_SERVER_ERROR_MESSAGE = "Couldn't contact server, check your connection";
        public static string WRONG_SERVER_ADDRESS_MESSAGE = "Incorrect server address";
        
#if DEBUG
        public static string CERT_FILE_NAME = "MaestrosSelfSignedCert.cer";
#else
        public static string CERT_FILE_NAME = "UserCode\\MaestrosSelfSignedCert.cer";
#endif
        public static Dictionary<int, string> responseMap = new Dictionary<int, string>();
        public static int nextRequestNumber;
        public static byte[] acceptableSelfSignedHash;

        [DllExport("ReturnHi", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        public static string ReturnHi()
        {
            return "Hi";
        }

        [DllExport("ForceWindowOpenAndFront", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static bool ForceWindowOpenAndFront()
        {
            
            IntPtr mainWindowHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            bool success = FlashWindow.ShowWindow(mainWindowHandle, FlashWindow.SW_SHOWDEFAULT);
            success = success && FlashWindow.ShowWindow(mainWindowHandle, FlashWindow.SW_SHOW);
            success = success && FlashWindow.SetForegroundWindow(mainWindowHandle);
            return success;
        }

        [DllExport("ShowWindow", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static bool ShowWindow(int nCmdShow)
        {
            IntPtr mainWindowHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            return FlashWindow.ShowWindow(mainWindowHandle, nCmdShow);
        }

        [DllExport("ShowWindowAsync", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static bool ShowWindowAsync(int nCmdShow)
        {
            IntPtr mainWindowHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            return FlashWindow.ShowWindowAsync(mainWindowHandle, nCmdShow);
        }

        [DllExport("SetForegroundWindow", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static bool SetForegroundWindow()
        {
            IntPtr mainWindowHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            return FlashWindow.SetForegroundWindow(mainWindowHandle);
        }

        [DllExport("StartFlashingWindow", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static bool StartFlashingWindow()
        {
            IntPtr mainWindowHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            return FlashWindow.Start(mainWindowHandle);
        }

        [DllExport("Flash", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static bool Flash()
        {
            IntPtr mainWindowHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            return FlashWindow.Flash(mainWindowHandle);
        }

        [DllExport("FlashCount", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static bool FlashCount(int count)
        {
            IntPtr mainWindowHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            uint unsignedCount = (uint) count;
            return FlashWindow.Flash(mainWindowHandle, unsignedCount);
        }

        [DllExport("StopFlashingWindow", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static bool StopFlashingWindow()
        {
            IntPtr mainWindowHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            return FlashWindow.Stop(mainWindowHandle);
        }

        [DllExport("HTTPGet", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        static public string HTTPGet([MarshalAs(UnmanagedType.LPWStr)] String urlEndQuery)
        {
            return Get(new WebClientWrapper(), urlEndQuery);
        }

        [DllExport("HTTPPost", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        static public string HTTPPost([MarshalAs(UnmanagedType.LPWStr)] string urlQuery, [MarshalAs(UnmanagedType.LPWStr)] string jsonPayload)
        {
            return Post(urlQuery, jsonPayload);
        }

        [DllExport("HTTPPostAsync", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.I4)]
        static public int HTTPPostAsync([MarshalAs(UnmanagedType.LPWStr)] string urlQuery, [MarshalAs(UnmanagedType.LPWStr)] string jsonPayload)
        {
            return PostAsync(new WebClientWrapper(), urlQuery, jsonPayload);
        }

        [DllExport("HTTP10PostNoResponse", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        static public string HTTP10PostNoResponse([MarshalAs(UnmanagedType.LPWStr)] string urlQuery, [MarshalAs(UnmanagedType.LPWStr)] string jsonPayload)
        {
            return PostNoResponseHTTP10(new WebClientWrapper(), urlQuery, jsonPayload);
        }

        [DllExport("GetResponse", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        public static string GetResponse(int requestNumber)
        {
            string response;
            bool gotResponse;
            gotResponse = responseMap.TryGetValue(requestNumber, out response);
            if (gotResponse && response != null)
            {
                return response;
            }
            else
            {
                return string.Empty;
            }
        }

        [DllExport("UploadCurrentUDKLog", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        public static string UploadCurrentUDKLog([MarshalAs(UnmanagedType.LPWStr)] string graylogUrl, [MarshalAs(UnmanagedType.LPWStr)] string playerName, int maxLogLengthPerBlob, int maxNumberofBlobs)
        {
            /* UploadCurrentUDKLog
             * Uploads a UDK log to our graylog analytics server.
             * In order to reduce the size of our entry on graylog, we send the logs over in a series of blobs. Each blob has a length which is set by maxLogLengthPerBlob.
             * 
             * graylogURL: url to our graylog server
             * playerName: name of the player who is submitting the log (this is only ever called on the client)
             * maxLogLengthPerBlob: how many bytes of log we put into each blob
             * maxNumberOfBlobs: maximum number of log blobs we should send
            */
            string logFilepath = "..\\..\\..\\UDKGame\\Logs\\Launch.log";   // latest game log

            return UploadUDKLog(logFilepath, graylogUrl, playerName, maxLogLengthPerBlob, maxNumberofBlobs, "BugReportLog");
        }

        [DllExport("CheckForCrashLogs", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        public static string CheckForCrashLogs([MarshalAs(UnmanagedType.LPWStr)] string graylogUrl, [MarshalAs(UnmanagedType.LPWStr)] string playerName, int maxLogLengthPerBlob, int maxNumberofBlobs)
        {
            /* CheckForCrashLogs
             * Checks the local logs folder for a .dmp file (indicates a crash occurred.)
             * Move .dmp files to the CrashDumps folder (so that it won't trigger again in the future.)
             * Figure out which log file corresponds to the given crash. Should be the latest log file.
             * Upload the log file.
             * Move the log file to it's own CrashLogs folder (prevents a potential issue if a bad named log could be stuck always for crash logs.
             * 
             * graylogURL: url to our graylog server
             * playerName: name of the player who is submitting the log (this is only ever called on the client)
             * maxLogLengthPerBlob: how many bytes of log we put into each blob
             * maxNumberOfBlobs: maximum number of log blobs we should send
            */
            string[] crashFiles = System.IO.Directory.GetFiles("..\\..\\..\\UDKGame\\Logs\\", "*.dmp");     // crash files end with .dmp

            if (crashFiles.Length == 0)
            {
                return "No crashes found.";
            }

            // Move all of the crash files into their own directory so they won't get found again
            System.IO.Directory.CreateDirectory("..\\..\\..\\UDKGame\\Logs\\CrashDumps");  // if CrashDumps folder already exist C# won't do anything

            foreach (string crashFile in crashFiles)
            {
                string newFileLocation = crashFile.Replace("..\\..\\..\\UDKGame\\Logs", "..\\..\\..\\UDKGame\\Logs\\CrashDumps");
                System.IO.File.Move(crashFile, newFileLocation);
            }

            // Find the latest log file and upload it
            string[] logFiles = System.IO.Directory.GetFiles("..\\..\\..\\UDKGame\\Logs\\", "*.log");

            if (logFiles.Length <= 1)
            {
                return "No log files to match crash found.";
            }

            List<string> logFilesList = new List<string>();
            logFilesList.AddRange(logFiles);
            logFilesList.Sort();    // because of the way logs are named, sorting them alphanumerically will cause the "newest" log to have the highest alphanumeric value

            string crashLogFile = logFilesList[logFilesList.Count - 1];     // this is the log file that corresponds to our dmp file

            UploadUDKLog(crashLogFile, graylogUrl, playerName, maxLogLengthPerBlob, maxNumberofBlobs, "CrashReportLog");

            // Move the crash log to the crash logs folder (so we can't be stuck with a zLaunch.log forever)
            System.IO.Directory.CreateDirectory("..\\..\\..\\UDKGame\\Logs\\CrashLogs");
            string newLogLocation = crashLogFile.Replace("..\\..\\..\\UDKGame\\Logs", "..\\..\\..\\UDKGame\\Logs\\CrashLogs");
            System.IO.File.Move(crashLogFile, newLogLocation);

            return crashLogFile;
        }

        static public string Post(string urlQuery, string payload)
        {
            string result = "";

            try
            {
                result = PerformPost(urlQuery, payload);
            }
            catch (Exception e)
            {
                result = e.Message;
            }

            return result;
        }

        static public string Get(WebClientWrapper web, string urlQuery)
        {
            string result = "";

            try
            {
                result = web.DownloadString(urlQuery);
            }
            catch (Exception e)
            {
               result = e.Message;
            }

            return result;
        }
        
        static public int PostAsync(WebClientWrapper web, string urlQuery, string payload)
        {
            string result = string.Empty;
            int requestNumber;

            ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;

            requestNumber = nextRequestNumber;
            nextRequestNumber++;

            try
            {
                web.setContentType("application/x-www-form-urlencoded");
                web.UploadStringAsync(new Uri(urlQuery), "POST", payload, requestNumber);
            }
            catch (WebException e)
            {
                Debug.WriteLine(e.Message);
                PutResponseInResponseMap(requestNumber, jsonifyErrorMessage(COULDNT_CONTACT_SERVER_ERROR_MESSAGE));          
            }
            catch (UriFormatException e)
            {
                Debug.WriteLine(e.Message);
                PutResponseInResponseMap(requestNumber, jsonifyErrorMessage(WRONG_SERVER_ADDRESS_MESSAGE));          
            }

            return requestNumber;
        }

        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            if (acceptableSelfSignedHash == null)
            {
                if (!File.Exists(CERT_FILE_NAME))
                {
                    return false;
                }

                try 
                {
                    X509Certificate acceptableSelfSignedCertificate = new X509Certificate();
                    acceptableSelfSignedCertificate.Import(CERT_FILE_NAME);
                    acceptableSelfSignedHash = acceptableSelfSignedCertificate.GetCertHash();
                }
                catch (Exception e)
                {
                    return false;
                }
            }

            byte[] receivedHash = certificate.GetCertHash();
            
            if (acceptableSelfSignedHash.Length != receivedHash.Length)
            {
                return false;
            }

            for (int i = 0; i < receivedHash.Length; ++i)
            {
                if (acceptableSelfSignedHash[i] != receivedHash[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Fire and forget HTTP 1.0 Post message
        /// </summary>
        /// <param name="web"></param>
        /// <param name="urlQuery"></param>
        /// <param name="payload"></param>
        /// <returns>Either "no error" or the error message</returns>
        static public string PostNoResponseHTTP10(WebClientWrapper web, string urlQuery, string payload)
        {
            try
            {
                web.HTTP10PostNoResponse(urlQuery, payload);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return e.Message;
            }

            return "no error";
        }

        public static string PerformPost(string address, string payload)
        {
            string response = "";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);
            request.Accept = "application/json";
            request.ContentType = "application/json";
            request.Method = "POST";
            //request.KeepAlive = false;

            Byte[] bytes = Encoding.ASCII.GetBytes(payload);
            request.ContentLength = bytes.Length;


            Stream requestStream = request.GetRequestStream();
            requestStream.Write(bytes, 0, bytes.Length);
            requestStream.Flush();
            requestStream.Close();

            WebResponse webResponse = request.GetResponse();

            Stream responseStream = webResponse.GetResponseStream();
            StreamReader streamReader = new StreamReader(responseStream);

            response = streamReader.ReadToEnd();

            return response;
        }

        public static string jsonifyErrorMessage(string error)
        {
            string json;
            error.Replace("{", "{{");
            error.Replace("}", "}}");
            try
            {
                json = string.Format("{{\"error\":\"{0}\"}}", error);
            }
            catch (FormatException e)
            {
                Debug.WriteLine(e.Message);
                return "";
            }
            
            return json;
        }

        static void PutResponseInResponseMap(int responseNumber, string response)
        {
            TMServerComm.responseMap[responseNumber] = response;
        }

        internal static void PostRequestAsyncCompleted(object sender, UploadStringCompletedEventArgs eventArgs)
        {
            if (eventArgs.Error == null && eventArgs.Result != null)
            {
                PutResponseInResponseMap((int)eventArgs.UserState, eventArgs.Result);
            }
            else if (eventArgs.Error is WebException)
            {
                PutResponseInResponseMap((int)eventArgs.UserState, jsonifyErrorMessage(COULDNT_CONTACT_SERVER_ERROR_MESSAGE));
            }
            else
            {
                Debug.WriteLine(eventArgs.Error);
                PutResponseInResponseMap((int)eventArgs.UserState, jsonifyErrorMessage(GENERIC_ERROR_MESSAGE));
            }
        }

        [return: MarshalAs(UnmanagedType.LPWStr)]
        public static string UploadUDKLog([MarshalAs(UnmanagedType.LPWStr)] string logFilepath, [MarshalAs(UnmanagedType.LPWStr)] string graylogUrl, [MarshalAs(UnmanagedType.LPWStr)] string playerName, int maxLogLengthPerBlob, int maxNumberofBlobs, [MarshalAs(UnmanagedType.LPWStr)] string hostName)
        {
            /* UploadUDKLog
             * Uploads a UDK log to our graylog analytics server.
             * In order to reduce the size of our entry on graylog, we send the logs over in a series of blobs. Each blob has a length which is set by maxLogLengthPerBlob.
             * 
             * graylogURL: url to our graylog server
             * playerName: name of the player who is submitting the log (this is only ever called on the client)
             * maxLogLengthPerBlob: how many bytes of log we put into each blob
             * maxNumberOfBlobs: maximum number of log blobs we should send
            */
            string logText = "";

            // Calculate how much of the log we should read
            int maxTotalLogLength = maxLogLengthPerBlob * maxNumberofBlobs;

            if (File.Exists(logFilepath))
            {
                // Open the file using FileShare.ReadWrite, since this file is still being accessed by UDK
                using (FileStream stream = File.Open(logFilepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    // Have different behavior depending on how long the log file is.
                    // If our log is shorter than we're allowing us to read, just read the full log.
                    // Otherwise the log is longer than our max total log length, read half from the start and half from the end of the log
                    if (stream.Length < maxTotalLogLength)
                    {
                        int numBytesToRead;
                        try
                        {
                            numBytesToRead = Convert.ToInt32(stream.Length);    // this should never fail since we know the stream length is less than our int
                        }
                        catch (Exception e)
                        {
                            numBytesToRead = 1;
                        }

                        // We can just read the whole log file
                        byte[] fileContent = new byte[numBytesToRead];

                        try
                        {
                            stream.Read(fileContent, 0, numBytesToRead);
                            logText = System.Text.Encoding.Default.GetString(fileContent);
                        }
                        catch (Exception e)
                        {
                            logText = "Couldn't read log file that was shorter than our max length.";
                        }
                    }
                    else
                    {
                        // We need to read half of our text from the start and half from the end of the log file
                        int numBytesToRead = maxTotalLogLength / 2;

                        // Read front of log
                        try
                        {
                            byte[] frontContent = new byte[numBytesToRead];
                            stream.Read(frontContent, 0, numBytesToRead);
                            logText = System.Text.Encoding.Default.GetString(frontContent);
                        }
                        catch (Exception e)
                        {
                            logText = "Couldn't read front of log file.";
                        }

                        // Move the file head to the end of the file where we want to read
                        stream.Position = stream.Length - (numBytesToRead + 1);
                        logText = logText + "\nLOG BREAK\n";

                        // Read end of log
                        try
                        {
                            byte[] backContent = new byte[numBytesToRead];
                            stream.Read(backContent, 0, numBytesToRead);
                            logText = logText + System.Text.Encoding.Default.GetString(backContent);
                        }
                        catch (Exception e)
                        {
                            logText = logText + "Couldn't read end of log file.";
                        }
                    }
                }

                logText = logText.Replace("\\", "\\\\");    // replace the backslashes with an escaped backslash
                logText = logText.Replace("\"", "'");   // make our double quotes be single quotes
            }
            else
            {
                logText = "NO FILE LOADED";
            }

            // Using a raw JSON string here since I couldn't get our JSON library to work properly
            /*
             * Special string formatting:
             *  short_message: has log blob sequence number
             *  full_message: contains log blob
             *  playername: the name of the player who is submitting the log
            */
            string jsonBlobFormat = "{{\"version\": \"1.1\",\"host\": \"{3}\",\"short_message\": \"Log {0}\",\"full_message\": \"{1}\",\"level\": 6,\"_playername\": \"{2}\"}}";

            // Send a log blob for each section of log
            for (int i = 0; i < maxNumberofBlobs; i++)
            {
                int frontIndex = i * maxLogLengthPerBlob;
                int backIndex = (i + 1) * maxLogLengthPerBlob - 1;

                // Our last blob might be shorter length
                if (backIndex > logText.Length)
                {
                    backIndex = logText.Length - 1;
                }

                // Only send a blob if we still have log text to send
                if (frontIndex < logText.Length)
                {
                    // Add a portion of the log text to a json blob
                    string jsonBlob = string.Format(jsonBlobFormat, i, logText.Substring(frontIndex, backIndex - frontIndex), playerName, hostName);

                    // Upload to graylog
                    PostAsync(new WebClientWrapper(), graylogUrl, jsonBlob);
                }
            }

            return "Upload finished.";
        }
    }
}
