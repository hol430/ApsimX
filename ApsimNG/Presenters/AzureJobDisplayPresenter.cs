﻿using System;
using System.Collections.Generic;
using System.Linq;
using ApsimNG.Cloud;
using ApsimNG.Cloud.Azure;
using UserInterface.Interfaces;
using System.IO;
using System.ComponentModel;
using Models.Core;
using UserInterface.Views;
using System.Threading;

namespace UserInterface.Presenters
{
    public class AzureJobDisplayPresenter : IPresenter, ICloudJobPresenter
    {
        /// <summary>
        /// List of jobs which are currently being downloaded.        
        /// </summary>
        private List<Guid> currentlyDownloading;
        
        /// <summary>
        /// The view displaying the information.
        /// </summary>
        private CloudJobDisplayView view;

        /// <summary>
        /// Class which handles comms with the cloud platform.
        /// </summary>
        private ICloudInterface cloudInterface;

        /// <summary>
        /// This worker repeatedly fetches information about all Azure jobs on the batch account.
        /// </summary>
        private BackgroundWorker fetchJobs;

        /// <summary>
        /// Mutual exclusion semaphore controlling access to the section of code relating to the log file.        
        /// </summary>
        private object logFileMutex;

        /// <summary>
        /// List of all Azure jobs.
        /// </summary>
        private List<JobDetails> jobList;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="primaryPresenter"></param>
        public AzureJobDisplayPresenter(MainPresenter primaryPresenter)
        {
            cloudInterface = new AzureInterface();
            Presenter = primaryPresenter;
            jobList = new List<JobDetails>();
            logFileMutex = new object();            
            currentlyDownloading = new List<Guid>();

            fetchJobs = new BackgroundWorker()
            {
                WorkerSupportsCancellation = true
            };
            fetchJobs.DoWork += FetchJobs_DoWork;
        }

        /// <summary>
        /// The parent presenter.
        /// </summary>
        public MainPresenter Presenter { get; set; }

        /// <summary>
        /// Attach the view to this presenter.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="view"></param>
        /// <param name="explorerPresenter"></param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.view = (CloudJobDisplayView)view;
            this.view.Presenter = this;
            fetchJobs.RunWorkerAsync();
        }

        /// <summary>
        /// Detach the view from this presenter.
        /// </summary>
        public void Detach()
        {
            fetchJobs.CancelAsync();
            view.Detach();
        }

        /// <summary>
        /// Asks the view to update the progress bar of an ongoing download.
        /// </summary>
        /// <param name="progress"></param>
        public void UpdateDownloadProgress(double progress)
        {
            view.DownloadProgress = progress;
        }

        /// <summary>
        /// Checks if the current user owns a job. 
        /// </summary>
        /// <param name="id">ID of the job.</param>
        /// <returns></returns>
        public bool UserOwnsJob(string id)
        {
            return GetJob(id).Owner.ToLower() == Environment.UserName.ToLower();
        }

        /// <summary>
        /// Gets the formatted display name of a job.
        /// </summary>
        /// <param name="id">ID of the job.</param>
        /// <param name="withOwner">If true, the return value will include the job owner's name in brackets.</param>
        /// <returns></returns>
        public string GetFormattedJobName(string id, bool withOwner)
        {
            JobDetails job = GetJob(id);
            return withOwner ? job.DisplayName + " (" + job.Owner + ")" : job.DisplayName;
        }

        /// <summary>
        /// Gets a job with a given ID from the local job list.
        /// </summary>
        /// <param name="id">ID of the job.</param>
        /// <returns>JobDetails object.</returns>
        public JobDetails GetJob(string id)
        {
            return jobList.FirstOrDefault(x => x.Id == id);
        }
        
        /// <summary>
        /// Downloads the results of a list of jobs.
        /// </summary>
        /// <param name="jobsToDownload">List of IDs of the jobs.</param>
        /// <param name="saveToCsv">If true, results will be combined into a csv file.</param>
        /// <param name="includeDebugFiles">If true, debug files will be downloaded.</param>
        /// <param name="keepOutputFiles">If true, the raw .db output files will be saved.</param>
        public void DownloadResults(List<string> jobsToDownload, bool downloadResults, bool saveToCsv, bool includeDebugFiles, bool keepOutputFiles, bool unzipResults, bool async = false)
        {
            Presenter.ShowMessage("", Simulation.MessageType.Information);

            view.DownloadStatus = "";            
            if (currentlyDownloading.Count > 0)
            {
                ShowErrorMessage("Unable to start a new batch of downloads - one or more downloads are already ongoing.");
                return;
            }

            if (jobsToDownload.Count < 1)
            {
                ShowMessage("Unable to download jobs - no jobs are selected.", Simulation.MessageType.Information);
                return;
            }
            
            view.ShowDownloadProgressBar();
            ShowMessage("", Simulation.MessageType.Information);
            string path = (string)AzureSettings.Default["OutputDir"];
            AzureResultsDownloader dl;

            // If a results directory (outputPath\jobName) already exists, the user will receive a warning asking them if they want to continue.
            // This message should only be displayed once. Once it's been displayed this boolean is set to true so they won't be asked again.
            bool ignoreWarning = false;
            Guid jobId;
            foreach (string id in jobsToDownload)
            {                
                // if the job id is invalid, skip downloading this job                
                if (!Guid.TryParse(id, out jobId)) continue;
                currentlyDownloading.Add(jobId);
                string jobName = GetJob(id).DisplayName;

                view.DownloadProgress = 0;

                // if output directory already exists and warning has not already been given, display a warning
                if (Directory.Exists(Path.Combine((string)AzureSettings.Default["OutputDir"], jobName)) && !ignoreWarning && saveToCsv)
                {
                    if (!view.AskQuestion("Files detected in output directory (" + Path.Combine((string)AzureSettings.Default["OutputDir"], jobName) + "). Results will be collated from ALL files in this directory. Are you certain you wish to continue?"))
                    {
                        // if user has chosen to cancel the download
                        view.HideDownloadProgressBar();
                        currentlyDownloading.Remove(jobId);
                        return;
                    }
                    else ignoreWarning = true;
                }

                // if job has not finished, skip to the next job in the list
                if (GetJob(id).State.ToString().ToLower() != "completed")
                {
                    ShowErrorMessage("Unable to download " + GetJob(id).DisplayName.ToString() + ": Job has not finished running");
                    continue;
                }

                dl = new AzureResultsDownloader(jobId, GetJob(id).DisplayName, path, this, downloadResults, saveToCsv, includeDebugFiles, keepOutputFiles, unzipResults);                
                dl.DownloadResults(async);
            }
        }        

        /// <summary>
        /// Removes a job from the list of currently downloading jobs.
        /// </summary>
        /// <param name="jobId">ID of the job.</param>
        public void DownloadComplete(Guid jobId)
        {
            currentlyDownloading.Remove(jobId);
            view.HideDownloadProgressBar();
            view.DownloadProgress = 0;
        }

        /// <summary>
        /// Displays an error message.
        /// </summary>
        /// <param name="msg">Message to be displayed.</param>
        public void ShowErrorMessage(string msg)
        {
            Presenter.ShowError(msg);
        }

        /// <summary>
        /// Displays an error in the status bar.
        /// </summary>
        /// <param name="err">Error to be displayed.</param>
        public void ShowError(Exception err)
        {
            Presenter.ShowError(err);
        }

        /// <summary>
        /// Displays a message to the user.
        /// </summary>
        /// <param name="msg"></param>
        public void ShowMessage(string msg, Simulation.MessageType errorLevel)
        {
            Presenter.ShowMessage(msg, errorLevel);
        }

        /// <summary>
        /// Sets the default downlaod directory.
        /// </summary>
        /// <param name="dir">Path to the directory.</param>
        public void SetDownloadDirectory(string dir)
        {
            if (dir == null || dir == "") return;

            if (Directory.Exists(dir))
            {
                AzureSettings.Default["OutputDir"] = dir;
                AzureSettings.Default.Save();
            }
            else
            {
                ShowErrorMessage("Directory " + dir + " does not exist.");
            }
        }

        /// <summary>
        /// Parses and compares two DateTime objects stored as strings.
        /// </summary>
        /// <param name="str1">First DateTime.</param>
        /// <param name="str2">Second DateTime.</param>
        /// <returns></returns>
        public int CompareDateTimeStrings(string str1, string str2)
        {
            // if either of these strings is empty, the job is still running
            if (str1 == "" || str1 == null)
            {
                if (str2 == "" || str2 == null) // neither job has finished
                {
                    return 0;
                }
                else // first job is still running, second is finished
                {
                    return 1;
                }
            }
            else if (str2 == "" || str2 == null) // first job is finished, second job still running
            {                
                return -1;
            }
            // otherwise, both jobs are still running
            DateTime t1 = GetDateTimeFromString(str1);
            DateTime t2 = GetDateTimeFromString(str2);
            
            return DateTime.Compare(t1, t2);
        }

        /// <summary>
        /// Generates a DateTime object from a string.
        /// </summary>
        /// <param name="st">Date time string. MUST be in the format dd/mm/yyyy hh:mm:ss (A|P)M</param>
        /// <returns>A DateTime object representing this string.</returns>
        public DateTime GetDateTimeFromString(string st)
        {
            try
            {
                string[] separated = st.Split(' ');
                string[] date = separated[0].Split('/');
                string[] time = separated[1].Split(':');
                int year, month, day, hour, minute, second;
                day = Int32.Parse(date[0]);
                month = Int32.Parse(date[1]);
                year = Int32.Parse(date[2]);

                hour = Int32.Parse(time[0]);
                if (separated[separated.Length - 1].ToLower() == "pm" && hour < 12) hour += 12;
                minute = Int32.Parse(time[1]);
                second = Int32.Parse(time[2]);

                return new DateTime(year, month, day, hour, minute, second);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
            return new DateTime();
        }

        /// <summary>
        /// Writes to a log file and asks the view to display an error message if download was unsuccessful.
        /// </summary>
        /// <param name="code"></param>
        public void DisplayFinishedDownloadStatus(string name, int code, string path)
        {            
            view.HideDownloadProgressBar();
            if (code == 0)
            {
                ShowMessage("Download successful.", Simulation.MessageType.Information);
                return;
            }
            string msg = DateTime.Now.ToLongTimeString().Split(' ')[0] + ": " +  name + ": ";
            switch (code)
            {
                case 1:
                    msg += "Unable to generate a .csv file: no result files were found.";
                    break;
                case 2:
                    msg += "Unable to generate a .csv file: one or more result files may be empty";
                    break;
                case 3:
                    msg += "Unable to generate a temporary directory.";
                    break;
                case 4:
                    msg += "Error getting report.";
                    break;
                default:
                    msg += "Download unsuccessful.";
                    break;
            }
            string logFile = Path.Combine(path, "download.log");
            view.DownloadStatus = "One or more downloads encountered an error. See " + logFile + " for more details.";
            lock (logFileMutex)
            {
                try
                {
                    if (!File.Exists(logFile)) File.Create(logFile);
                    using (StreamWriter sw = File.AppendText(logFile))
                    {
                        sw.WriteLine(msg);
                        sw.Close();
                    }
                }
                catch
                {

                }
            }
        }

        /// <summary>
        /// Asks the user for confirmation and then halts execution of a list of jobs.
        /// </summary>
        /// <param name="id">ID of the job.</param>
        public void StopJobs(List<string> jobIds)
        {
            // ask user once for confirmation
            if (jobIds.Count < 1)
            {
                Presenter.ShowMessage("Unable to stop jobs: no jobs are selected", Simulation.MessageType.Information);
                return;
            }
            // get the grammar right when asking for confirmation
            bool stopMultiple = jobIds.Count > 1;
            string msg = "Are you sure you want to stop " + (stopMultiple ? "these " + jobIds.Count + " jobs?" : "this job?") + " There is no way to resume " + (stopMultiple ? "their" : "its" ) + " execution!";
            if (!view.AskQuestion(msg)) return;
            
            foreach (string id in jobIds)
            {
                // no need to stop a job that is already finished
                if (GetJob(id).State.ToLower() != "completed")
                {
                    StopJob(id);
                }
            }            
        }

        /// <summary>
        /// Halts the execution of a job.
        /// </summary>
        /// <param name="id"></param>
        public void StopJob(string id)
        {
            try
            {
                cloudInterface.StopJob(Guid.Parse(id));
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Asks the user for confirmation and then deletes a list of jobs.
        /// </summary>
        /// <param name="jobIds">ID of the job.</param>
        public void DeleteJobs(List<string> jobIds)
        {
            if (jobIds.Count < 1)
            {
                Presenter.ShowMessage("Unable to delete jobs: no jobs are selected.", Simulation.MessageType.Information);
                return;
            }

            bool restart = fetchJobs.IsBusy;
            // cancel the fetch jobs worker
            if (restart)
                fetchJobs.CancelAsync();

            view.HideLoadingProgressBar();


            // get the grammar right when asking for confirmation
            bool deletingMultiple = jobIds.Count > 1;
            string msg = "Are you sure you want to delete " + (deletingMultiple ? "these " + jobIds.Count + " jobs?" : "this job?");
            string label = deletingMultiple ? "Delete these jobs?" : "Delete this job?";

            // if user says no to the popup, no further action required
            if (!AskQuestion(msg)) return;
            Guid parsedId;
            foreach (string id in jobIds)
            {
                try
                {
                    if (!Guid.TryParse(id, out parsedId))
                        continue;

                    // Delete the job.
                    cloudInterface.DeleteJob(parsedId);
                    
                    // Remove the job from the locally stored list of jobs.
                    jobList.RemoveAt(jobList.IndexOf(GetJob(id)));
                }
                catch (Exception err)
                {
                    ShowError(err);
                }                
            }
            // Refresh the tree view.
            view.UpdateJobTable(jobList);

            // Restart the fetch jobs worker.
            // fixme - this has more holes than swiss cheese
            try
            {
                if (restart)
                {
                    if (fetchJobs.IsBusy)
                    {
                        fetchJobs.CancelAsync();
                    }

                    fetchJobs.RunWorkerAsync();
                }
            }
            catch
            {
                Presenter.ShowMessage("Unable to restart job fetcher. Current job list will not be updated.", Simulation.MessageType.Warning);
            }
        }

        /// <summary>
        /// Asks the user a question, allowing them to choose yes or no. Returns true if they clicked yes, returns false otherwise.
        /// </summary>
        /// <param name="msg">Message/question to be displayed.</param>
        /// <returns></returns>
        private bool AskQuestion(string msg)
        {
            return Presenter.AskQuestion(msg) == QuestionResponseEnum.Yes;
        }

        private void FetchJobs_DoWork(object sender, DoWorkEventArgs args)
        {
            while (!fetchJobs.CancellationPending) // this check is performed regularly inside the ListJobs() function as well.
            {
                // update the list of jobs. this will take a bit of time                
                view.ShowLoadingProgressBar();
                CancellationToken ct = new CancellationToken(); // fixme!!
                var newJobs = cloudInterface.ListJobs(ct, p => view.JobLoadProgress = p);
                view.HideLoadingProgressBar();

                if (fetchJobs.CancellationPending)
                    return;

                if (newJobs == null)
                    return;

                if (newJobs.Count > 0)
                {
                    // if the new job list is different, update the tree view
                    if (newJobs.Count() != jobList.Count())
                    {
                        jobList = newJobs;
                        if (UpdateDisplay() == 1)
                            return;
                    }
                    else
                    {
                        for (int i = 0; i < newJobs.Count(); i++)
                        {
                            if (!IsEqual(newJobs[i], jobList[i]))
                            {
                                jobList = newJobs;
                                if (UpdateDisplay() == 1)
                                    return;

                                break;
                            }
                        }
                        jobList = newJobs;
                    }
                }
                // Refresh job list every 10 seconds
                System.Threading.Thread.Sleep(10000);
            }
        }

        /// <summary>
        /// Asks the view to update the tree view.
        /// </summary>
        /// <returns>0 if the operation is successful, 1 if a NullRefEx. occurs, 2 if another exception is generated.</returns>
        private int UpdateDisplay()
        {
            try
            {
                view.UpdateJobTable(jobList);
            }
            catch (NullReferenceException)
            {
                return 1;
            }
            catch (Exception err)
            {
                ShowError(err);
                return 2;
            }
            return 0;
        }

        /// <summary>
        /// Tests if two jobs are equal.
        /// </summary>
        /// <param name="a">The first job.</param>
        /// <param name="b">The second job.</param>
        /// <returns>True if the jobs have the same ID and they are in the same state.</returns>
        private bool IsEqual(JobDetails a, JobDetails b)
        {
            return (a.Id == b.Id && a.State == b.State && a.Progress == b.Progress);
        }

        public void GetCredentials()
        {
            throw new NotImplementedException();
        }
    }
}
