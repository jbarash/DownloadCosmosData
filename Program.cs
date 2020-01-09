using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DownloadCosmosData
{
    using System.Data;
    using System.Data.SqlClient;
    using System.Text.RegularExpressions;
    using VcClient;

    class Program
    {
        public static string vc = string.Empty;
        public static string script_filename = string.Empty;
        public static string startdate = string.Empty;
        public static string enddate = string.Empty;
        public static string updateDate = string.Empty;

        //Pass nType as parameter to determin script lookup
        static void Main(string[] args)
        {

            FileElement fe = null;
            bool bHeader = false;
            string baseStreamPath = string.Empty;



            LogError("Starting " + args[0]);

            if (args.Length == 0) {
                LogError("No parameter passed");
                System.Environment.Exit(1); // no parameters                
            }
            else
            {
                try {
                    fe = cosmosSetup.GetFiles(args[0]);
                    if (fe is null)
                    {
                        LogError("Parameter not found " + args[0]);
                        System.Environment.Exit(1); // parameter not found
                    }
                    if (args.Length == 2)
                    {
                        updateDate = args[1];
                    }
                    else
                    {
                        updateDate = DateTime.Now.ToString("yyyy-MM-dd");
                    }
                    vc = fe.VC;
                    script_filename = fe.script;
                    if (fe.excludeHeader == "1")
                    {
                        bHeader = true;
                    }

                }
                catch (Exception ex)
                {
                    LogError(ex.ToString());
                    System.Environment.Exit(1); // error processing parameters
                }
            } //end else
            try
            {
                VcClient.VC.Setup(vc, VcClient.VC.NoProxy, null);
            }
            catch (Exception ex)
            {
                sendEmail("Download Error Connecting to VC", fe.VC);
                LogError("Error connecting to VC:");
                LogError(ex.ToString());
                System.Environment.Exit(1); // Could not connect to VC
            }

         
            if (fe.filePrefix.Contains("Retail"))
            {
                try
                {
                    DownloadKlondike.DownloadKlondikeFile(fe.streamPath, fe.downloadDirectory, updateDate.Substring(0, 4) + "/" + updateDate.Substring(5, 2) + "/" + updateDate.Substring(8, 2) + "/");
                    sendEmail("Download Cosmos Complete", args[0]);
                }
                catch(Exception ex)
                {
                    sendEmail("DownloadCosmos Error " + args[0], ex.ToString());
                    LogError(ex.ToString());
                    System.Environment.Exit(2);
                }
                System.Environment.Exit(0);
                
            }
            var fileDict = GetFiles.GetAllFiles(fe.streamPath, fe.filePrefix + @"\w+\.ss$");
            DownloadCosmos.DownloadFileFullPath(fileDict.FirstOrDefault().Key, fileDict.FirstOrDefault().Value.Replace(".ss", ""), fe.downloadDirectory, bHeader, fe.incr);
            System.Environment.Exit(0);
            try {

                    baseStreamPath = fe.streamPath + updateDate + '/';
                    JobInfo.JobState jobStat;
                    if (fe.script.Length > 0)
                    {
                        jobStat = runScript.runScopeScript(fe.script);
                        if (jobStat != JobInfo.JobState.Completed && jobStat != JobInfo.JobState.CompletedSuccess)
                        {
                            sendEmail("Scope Failed " + args[0], runScript.errorMessage);
                            LogError("Scope Job Failed");
                            System.Environment.Exit(1);
                        }
                    }


                    IEnumerable<string> directories;
                    directories = GetDirectories(fe.streamPath);
                    bool pathExists = directories.Any(str => str.Contains(updateDate));


                    if (pathExists == false)
                    {
                        sendEmail("Download Cosmos Failed " + args[0], "No Directory " + fe.streamPath + updateDate);
                        LogError("Directory not found " + "No Directory " + fe.streamPath + updateDate);
                        System.Environment.Exit(1);
                    }


                    if (fe.renameFile == "1")
                    {
                        var stream = VC.ReadStream(fe.streamPath + fe.updateFile, false);
                        //var stream = VC.ReadStream(@"https://cosmos15.osdinfra.net/cosmos/dsa.email.segmentation/local/users/MeritDirect/2019-10-11/OneStore_LastUpdate_Order.txt", false);
                        byte[] buffer = new byte[2048];
                        stream.Read(buffer, 0, 2048);
                        stream.Close();
                        string[] dates = Encoding.ASCII.GetString(buffer, 0, buffer.Length).TrimEnd('\r', '\n', '\0').Split('\t');
                        string newLastDate = Encoding.ASCII.GetString(buffer, 0, buffer.Length).TrimEnd('\r', '\n', '\0');
                        // newLastDate = newLastDate.Substring(newLastDate.IndexOf('\t') + 1); removed 20191007 no longer preceding data with file name
                        DateTime dtmNewLastDate;
                        DateTime.TryParse(newLastDate, out dtmNewLastDate);
                        newLastDate = dtmNewLastDate.ToString("yyyyMMdd");
                        IEnumerable<string> streams;
                        streams = GetStreamsRecurse(baseStreamPath, new Regex(fe.filePrefix + @".*\.ss$"));
                        // Add actual last update date to output filename
                        if (streams.Count() > 0)
                        {
                            foreach (var streamPath in streams)
                            {
                                var uri = new Uri(streamPath);
                                var relativeStreamPath = uri.Segments[uri.Segments.Length - 1];
                                var endDate = relativeStreamPath.Substring(relativeStreamPath.LastIndexOf("_") + 1).Replace(".ss", ""); //extract date from stream name. Last token using _ (Underline) strip .ss extension

                                if (newLastDate != endDate && fe.renameFile == "1") //rename stream if actual end date does not match data
                                {
                                    string newStreamPath = relativeStreamPath.Substring(0, relativeStreamPath.LastIndexOf("_") + 1) + dtmNewLastDate.ToString("yyyyMMdd") + ".ss";
                                    var fullCosmosPath = Path.Combine(baseStreamPath, relativeStreamPath);
                                    var newFullCosmosPath = Path.Combine(baseStreamPath, newStreamPath);
                                    VC.Rename(fullCosmosPath, newFullCosmosPath);
                                    DownloadCosmos.DownloadFile(baseStreamPath, newStreamPath.Replace(".ss", ""), fe.downloadDirectory, bHeader, fe.incr);
                                    sendEmail("Download Cosmos Complete " + args[0], DateTime.Now.ToString("MMM dd yyyy hh:mm tt") + " " + relativeStreamPath + " " + DownloadCosmos.recordCount.ToString(" #,### records"));
                                }
                                else
                                {
                                    DownloadCosmos.DownloadFile(baseStreamPath, relativeStreamPath.Replace(".ss", ""), fe.downloadDirectory, bHeader, fe.incr);
                                    sendEmail("Download Cosmos Complete " + args[0], DateTime.Now.ToString("MMM dd yyyy hh:mm tt") + "<br/>" + relativeStreamPath + "<br/>" + DownloadCosmos.recordCount.ToString(" #,### records"));
                                }
                            }

                        }// streams found
                        else
                        {
                            sendEmail("No streams found " + fe.filePrefix, baseStreamPath);
                        }
                    } // end if rename
                    else
                    {
                        IEnumerable<string> streams;
                        streams = GetStreamsRecurse(baseStreamPath, new Regex(fe.filePrefix + @".*\.ss$"));
                        // Add actual last update date to output filename
                        if (streams.Count() > 0)
                        {
                            foreach (var streamPath in GetStreamsRecurse(baseStreamPath, new Regex(fe.filePrefix + @".*\.ss$")))
                            {
                                var uri = new Uri(streamPath);
                                var relativeStreamPath = uri.Segments[uri.Segments.Length - 1];
                                try
                                {
                                    DownloadCosmos.DownloadFile(baseStreamPath, relativeStreamPath.Replace(".ss", ""), fe.downloadDirectory, bHeader, fe.incr);
                                }
                                catch (VcClientExceptions.VcClientException ex)
                                {
                                    if (ex.ToString().Contains("throttled") || ex.ToString().Contains("ExportResetException"))
                                    {
                                        DownloadCosmos.DownloadFile(baseStreamPath, relativeStreamPath.Replace(".ss", ""), fe.downloadDirectory, bHeader, fe.incr);
                                    }
                                    else
                                    {
                                        sendEmail("DownloadCosmos Error " + args[0], ex.ToString());
                                        LogError(ex.ToString());
                                        System.Environment.Exit(2);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    if (ex.ToString().Contains("throttled") || ex.ToString().Contains("ExportResetException"))
                                    {
                                        DownloadCosmos.DownloadFile(baseStreamPath, relativeStreamPath.Replace(".ss", ""), fe.downloadDirectory, bHeader, fe.incr);
                                    }
                                    else
                                    {
                                        sendEmail("DownloadCosmos Error " + args[0], ex.ToString());
                                        LogError(ex.ToString());
                                        System.Environment.Exit(2);
                                    }

                                }
                                sendEmail("Download Cosmos Complete " + args[0], DateTime.Now.ToString("MMM dd yyyy hh:mm tt") + "<br/>" + relativeStreamPath + "<br/>" + DownloadCosmos.recordCount.ToString(" #,### records"));
                            }

                        }
                        else
                        {
                            sendEmail("No streams found " + args[0], baseStreamPath);
                        }
                    }


                    LogError("Completed " + args[0]);


                }
                catch (VcClientExceptions.VcClientException ex)
                {
                    sendEmail("DownloadCosmos Error " + args[0], ex.ToString());
                    LogError(ex.ToString());
                    System.Environment.Exit(2);
                }
                catch (Exception ex)
                {
                    sendEmail("DownloadCosmos Error " + args[0], ex.ToString());
                    LogError(ex.ToString());
                    System.Environment.Exit(2);
                }

          
            
            System.Environment.Exit(0);
        } //end Main

        private static IEnumerable<string> GetStreamsRecurse(string baseDirectory, Regex regex)
        {
            foreach (var streamInfo in VC.GetDirectoryInfo(baseDirectory, true))
            {
                if (streamInfo.IsDirectory)
                {
                    foreach (var subStream in GetStreamsRecurse(streamInfo.StreamName, regex))
                    {
                        yield return subStream;
                    }
                }
                else if (regex.IsMatch(streamInfo.StreamName))
                {
                    yield return streamInfo.StreamName;
                }
            }
        }

        private static IEnumerable<string> GetDirectories(string baseDirectory) {
            foreach (var streamInfo in VC.GetDirectoryInfo(baseDirectory, true))
            {
                if (streamInfo.IsDirectory)
                {
                    yield return streamInfo.StreamName;
                }
            }
        }




        private static void LogError(string sText)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(DateTime.Now.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss") + ' ' + sText);
            File.AppendAllText(Properties.Settings.Default.logPath + "DownloadCosmosData.txt", sb.ToString() + "\r\n");
            sb.Clear();
        }
        
       private static void getDates(string streamPath, ref string minDate, ref string maxDate)
        {
            var stream = VC.ReadStream(streamPath, false);
            byte[] buffer = new byte[2048];
            stream.Read(buffer, 0, 2048);
            stream.Close();
            string[] dates = Encoding.ASCII.GetString(buffer, 0, buffer.Length).TrimEnd('\r', '\n', '\0').Split('\t');
            DateTime dtmMinDate;
            DateTime.TryParse(dates[0], out dtmMinDate);
            DateTime dtmMaxDate;
            DateTime.TryParse(dates[1], out dtmMaxDate);

            minDate = dtmMinDate.ToString("yyyyMMdd");
            maxDate = dtmMaxDate.ToString("yyyyMMdd");
        }
        public static void sendEmail(string Subject, string Message)
        {
            try
            {
                string connectionString = Properties.Settings.Default.ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand()
                    {
                        CommandText = "msdb.dbo.sp_send_dbmail",
                        CommandType = CommandType.StoredProcedure,
                        Connection = conn
                    })
                    {
                        cmd.Parameters.AddWithValue("@profile_name", Properties.Settings.Default.Profile);
                        cmd.Parameters.AddWithValue("@recipients", Properties.Settings.Default.Recipients);
                        cmd.Parameters.AddWithValue("@subject", Subject);
                        cmd.Parameters.AddWithValue("@body", Message);
                        cmd.Parameters.AddWithValue("@body_format", "HTML");
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    } // end using cmd
                } // end using conn
            }
            catch(Exception ex)
            {

            }

        }
    } // end Class
    
} // end Namespace
