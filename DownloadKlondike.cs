using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using VcClient;

namespace DownloadCosmosData
{
    class DownloadKlondike
    {
        public static void DownloadKlondikeFile(string cosmosPath, string downloadPath, string updateDate)
        {
           
           
            var baseDiskPath = downloadPath;
            var baseCosmosPath = cosmosPath;
            baseCosmosPath = baseCosmosPath + updateDate;
            foreach (var streamPath in GetStreamsRecurse(baseCosmosPath, new Regex(@"\.csv$")))
            {
                var relativeStreamPath = streamPath.Replace(baseCosmosPath, string.Empty);
                var fullCosmosPath = Path.Combine(baseCosmosPath, relativeStreamPath);
                //var fullDiskPath = Path.Combine(baseDiskPath, relativeStreamPath);
                var fullDiskPath = baseDiskPath + relativeStreamPath.Replace("/","");
                /*
                var directory = Path.GetDirectoryName(fullDiskPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                */
                //VC.Download(fullCosmosPath, fullDiskPath, true, DownloadMode.OverWrite);
                VC.Download(streamPath, fullDiskPath, true, DownloadMode.OverWrite);
            }
        }

        private static IEnumerable<string>  GetStreamsRecurse(string baseDirectory, Regex regex)
        {
            foreach (var streamInfo in VC.GetDirectoryInfo(baseDirectory, true))
            {
                if (streamInfo.IsDirectory)
                {
                    /*
                    foreach (var subStream in GetStreamsRecurse(streamInfo.StreamName, regex))
                    {
                        yield return subStream;
                    }
                    */
                }
                else if (regex.IsMatch(streamInfo.StreamName))
                {
                    yield return streamInfo.StreamName;
                }
            }
        }
    }
}
