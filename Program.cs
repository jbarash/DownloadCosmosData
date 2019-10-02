using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace RunCosmosScript
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

        //Pass nType as parameter to determin script lookup
        static void Main(string[] args)
        {

            FileElement fe = null;
            bool bHeader = false;


            LogError("Starting " + args[0]);
           
            if (args.Length == 0) {
                LogError("No parameter passed");
                System.Environment.Exit(1); // no parameters                
            }
            else
            {
                try { 
                fe = cosmosSetup.GetFiles(args[0]);                   
                vc = fe.VC;
                script_filename = fe.script;
                if (fe.excludeHeader == "1")
                {
                    bHeader = true;
                }
                  
                }
                    catch(Exception ex)
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
                    LogError("Error connecting to VC:");
                    LogError(ex.ToString());
                    System.Environment.Exit(1); // Could not connect to VC
                }

                try
                {
                
                    string baseStreamPath = fe.streamPath + DateTime.Now.ToString("yyyy-MM-dd") + '/';

                //var stream = VC.ReadStream(fe.streamPath + fe.filePrefix + "LastUpdate.txt", false);
                var stream = VC.ReadStream(fe.streamPath + fe.updateFile, false);
                byte[] buffer = new byte[2048];
                stream.Read(buffer, 0, 2048);
                stream.Close();
                string newLastDate = Encoding.ASCII.GetString(buffer, 0, buffer.Length).TrimEnd('\r', '\n', '\0');
                DateTime dtmNewLastDate;
                if(fe.renameFile == "1") { 
                if (DateTime.TryParse(newLastDate, out dtmNewLastDate) == true)
                {
                    newLastDate = dtmNewLastDate.ToString("yyyyMMdd");
                    if (newLastDate != enddate.Replace("-", ""))
                    {
                        VC.Rename(baseStreamPath + fe.filePrefix + startdate.Replace("-", "") + '_' + enddate.Replace("-", "") + ".ss", baseStreamPath + fe.filePrefix + startdate.Replace("-", "") + '_' + newLastDate + ".ss");
                    }
                }
                }

                // Add actual last update date to output filename
                foreach (var streamPath in GetStreamsRecurse(baseStreamPath, new Regex(fe.filePrefix + @".*\.ss$")))
                {
                    var uri = new Uri(streamPath);
                    var relativeStreamPath = uri.Segments[uri.Segments.Length - 1];
                    var fullCosmosPath = Path.Combine(baseStreamPath, relativeStreamPath);
                    
                    DownloadCosmos.DownloadFile(baseStreamPath, relativeStreamPath.Replace(".ss",""), fe.downloadDirectory, bHeader);

                    //VC.Download(fullCosmosPath, fullDiskPath, true, DownloadMode.OverWrite);
                }
                //DownloadCosmos.DownloadFile(fe.streamPath + DateTime.Now.ToString("yyyyMMdd") + '/', fe.filePrefix + DateTime.Now.ToString("yyyyMMdd"), "C:\\temp\\");
            }
            catch (VcClientExceptions.VcClientException ex)
            {
                LogError(ex.ToString());
            }
            catch (Exception ex)
            {
                LogError(ex.ToString());
            }
            
            File.Delete(script_filename); //delete temporary file
          //Call proc for success or failure
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

       
       
        private static void LogError(string sText)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(DateTime.Now.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss") + ' ' + sText);
            File.AppendAllText(Properties.Settings.Default.logPath + "RunCosmosScript.txt", sb.ToString() + "\r\n");
            sb.Clear();
        }
        
      
        
    } // end Class
    
} // end Namespace
