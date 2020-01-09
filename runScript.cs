using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using VcClient;

namespace DownloadCosmosData
{
     class runScript
        
    {
        public static string errorMessage = string.Empty;

        public static JobInfo.JobState runScopeScript(string script_filename) { 
        var subParams = new ScopeClient.SubmitParameters(script_filename);
        ScopeClient.ScopeEnvironment.Instance.WorkingRoot = System.IO.Path.GetTempPath();
                    var jobinfo = ScopeClient.Scope.Submit(subParams);

        // Wait
        WaitUntilJobFinished(jobinfo);
        return VcClient.VC.GetJobInfo(jobinfo.ID, true).State;

        }
        private static void WaitUntilJobFinished(JobInfo jobinfo)
        {
            // The submission is done. Now we wait until the job is done
            bool use_compression = true;
            int seconds_to_sleep = 5;
            var wait_time = new System.TimeSpan(0, 0, 0, seconds_to_sleep);
            while (true)
            {
                jobinfo = VcClient.VC.GetJobInfo(jobinfo.ID, use_compression);
                Console.WriteLine("Job State = {0}", jobinfo.State);
                if (jobinfo.State == VcClient.JobInfo.JobState.Cancelled || jobinfo.State == VcClient.JobInfo.JobState.Completed
                    || jobinfo.State == VcClient.JobInfo.JobState.CompletedFailure
                    || jobinfo.State == VcClient.JobInfo.JobState.CompletedSuccess)
                {
                    Console.WriteLine("Job Stopped Running");
                    errorMessage = jobinfo.Error;
                    break;
                }

                System.Threading.Thread.Sleep(wait_time);
            }
        }

    }
}
