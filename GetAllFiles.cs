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
    class GetFiles
    {
        public static Dictionary<string, string> GetAllFiles(string cosmosPath, string Ext)
        {
            
            var dict = new Dictionary<string, string>();
            var baseCosmosPath = cosmosPath;          
            foreach (var streamPath in GetStreamsRecurse(baseCosmosPath, new Regex(Ext)))
            {
                
                
                var uri = new Uri(streamPath);
                var relativeStreamPath = uri.Segments[uri.Segments.Length - 1];
                dict.Add(streamPath,relativeStreamPath);
                
            }
            return dict;
        }

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
    }
}
