using System;
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
using System.Threading.Tasks;

namespace UserInterface.Presenters
{
    public class AzureJobDisplayPresenter : IPresenter
    {
        /// <summary>The view displaying the information.</summary>
        private CloudJobDisplayView view;

        /// <summary>The parent presenter.</summary>
        private MainPresenter Presenter { get; set; }

        /// <summary>Class which handles comms with the cloud platform.</summary>
        private ICloudInterface cloudInterface;

        /// <summary>List of all Azure jobs.</summary>
        private List<JobDetails> jobList;

        /// <summary>Cancellation token for job loading.</summary>
        private CancellationTokenSource cancelRefresh;

        /// <summary>Cancellation token for results downloads.</summary>
        private CancellationTokenSource cancelDownload;

        /// <summary>Timer responsible for refreshing the job list every 10 seconds.</summary>
        private Timer refresh;

        /// <summary>Constructor.</summary>
        /// <param name="primaryPresenter">Main Presenter.</param>
        public AzureJobDisplayPresenter(MainPresenter primaryPresenter)
        {
            cloudInterface = new AzureInterface();
            Presenter = primaryPresenter;
            jobList = new List<JobDetails>();
            cancelRefresh = new CancellationTokenSource();
            cancelDownload = new CancellationTokenSource();
        }

        /// <summary>Attach the view to this presenter.</summary>
        /// <param name="model">The model (unused).</param>
        /// <param name="view">The view.</param>
        /// <param name="explorerPresenter">The explorer presenter (unused).</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.view = (CloudJobDisplayView)view;

            // Attach event handlers.
            this.view.DeleteJob += DeleteJobs;
            this.view.StopJob += StopJobs;
            this.view.DownloadJob += OnDownloadClicked;
            this.view.SetCredentials += SetCredentials;

            // Refresh job list every 10 seconds.
            refresh = new Timer(OnRefresh, null, 0, 10 * 1000);
        }

        /// <summary>
        /// Detach the view from this presenter.
        /// </summary>
        public void Detach()
        {
            view.DeleteJob -= DeleteJobs;
            view.StopJob -= StopJobs;
            view.DownloadJob -= OnDownloadClicked;
            view.SetCredentials -= SetCredentials;

            refresh.Dispose();
            cancelRefresh.Cancel();
            view.Detach();
        }

        /// <summary>Called by the timer every 10 seconds. Refreshes the job list.</summary>
        /// <param name="state">State variable passed in by timer. Unused.</param>
        private async void OnRefresh(object state)
        {
            await RefreshAsync(cancelRefresh.Token);
        }

        /// <summary>Refresh the list of jobs.</summary>
        /// <param name="ct">Cancellation token.</param>
        private async Task RefreshAsync(CancellationToken ct)
        {
            refresh.Change(Timeout.Infinite, Timeout.Infinite);
            // Update the list of jobs. this will take a bit of time.
            view.ShowLoadingProgressBar();
            List<JobDetails> newJobs = await cloudInterface.ListJobsAsync(ct, p => view.JobLoadProgress = p);
            view.HideLoadingProgressBar();

            if (ct.IsCancellationRequested || newJobs == null)
                return;

            // Only update the view if the new job list is different.
            if (newJobs.Count != jobList.Count)
                view.UpdateJobTable(newJobs);
            else
            {
                for (int i = 0; i < newJobs.Count; i++)
                {
                    if (i >= jobList.Count || !IsEqual(newJobs[i], jobList[i]))
                    {
                        view.UpdateJobTable(newJobs);
                        break;
                    }
                }
            }

            jobList = newJobs;
            refresh.Change(10 * 1000, 10 * 1000);
        }

        /// <summary>Gets a job with a given ID from the local job list.</summary>
        /// <param name="id">ID of the job.</param>
        private JobDetails GetJob(string id)
        {
            return jobList.FirstOrDefault(x => x.ID == id);
        }

        /// <summary>
        /// Called when the user clicks the download button. Opens a
        /// popup window which provides the user with download options.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnDownloadClicked(object sender, EventArgs args)
        {
            List<string> jobsToDownload = view.GetSelectedJobIds();
            if (jobsToDownload == null || jobsToDownload.Count < 1)
                throw new Exception("Unable to download jobs - no jobs are selected.");

            DownloadWindow getPrefs = new DownloadWindow(view);
            getPrefs.Download += DownloadResults;
        }

        /// <summary>Downloads the results of a list of jobs.</summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private async Task DownloadResults(object sender, EventArgs args)
        {
            List<string> jobsToDownload = view.GetSelectedJobIds();
            if (jobsToDownload == null || jobsToDownload.Count < 1)
                throw new Exception("Unable to download jobs - no jobs are selected.");

            DownloadWindow window = sender as DownloadWindow;

            DownloadOptions opts = new DownloadOptions();
            opts.ExportToCsv = window.ExportToCsv;
            opts.DownloadDebugFiles = window.IncludeDebugFiles;
            opts.ExtractResults = window.ExtractResults;

            foreach (string id in jobsToDownload)
            {
                if (!Guid.TryParse(id, out Guid jobId))
                    throw new Exception($"Unable to parse job ID '{id}'");

                JobDetails job = GetJob(id);
                if (job == null)
                    throw new Exception($"Unable to find job with ID '{id}'");

                opts.Path = Path.Combine((string)AzureSettings.Default["OutputDir"], job.Name);
                opts.JobID = jobId;

                // If output directory already exists, display a warning.
                string msg = $"Output directory '{opts.Path}' already exists. Are you sure you wish to continue?";
                if (Directory.Exists(opts.Path) && opts.ExportToCsv && Presenter.AskQuestion(msg) != QuestionResponseEnum.Yes)
                    return;

                view.ShowDownloadProgressBar();
                view.DownloadProgress = 0;

                // If job has not finished, skip to the next job in the list.
                if (job.State.ToString().ToLower() != "completed")
                    throw new Exception($"Unable to download '{job.Name}': Job has not finished running");

                await cloudInterface.DownloadResultsAsync(opts, cancelDownload.Token);
            }
        }        

        /// <summary>
        /// Asks the user for confirmation and then halts execution of a list of jobs.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private async Task StopJobs(object sender, EventArgs args)
        {
            List<string> jobIds = view.GetSelectedJobIds();
            if (jobIds == null || jobIds.Count < 1)
                throw new Exception("Unable to stop jobs: no jobs are selected");

            // Ask user for confirmation.
            bool stopMultiple = jobIds.Count > 1;
            string msg = "Are you sure you want to stop " + (stopMultiple ? "these " + jobIds.Count + " jobs?" : "this job?") + " There is no way to resume " + (stopMultiple ? "their" : "its" ) + " execution!";
            if (Presenter.AskQuestion(msg) != QuestionResponseEnum.Yes)
                return;
            
            foreach (string id in jobIds)
                if (GetJob(id).State.ToLower() != "completed")
                    await cloudInterface.StopJobAsync(Guid.Parse(id), new CancellationToken());
        }

        /// <summary>
        /// Asks the user for confirmation and then deletes a list of jobs.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private async Task DeleteJobs(object sender, EventArgs args)
        {
            List<string> jobIds = view.GetSelectedJobIds();
            if (jobIds == null || jobIds.Count < 1)
                Presenter.ShowMessage("Unable to delete jobs: no jobs are selected.", Simulation.MessageType.Information);

            // Stop the fetch jobs worker.
            StopRefresh();

            // "Are you sure you want to delete these jobs?"
            bool deletingMultiple = jobIds.Count > 1;
            string msg = "Are you sure you want to delete " + (deletingMultiple ? "these " + jobIds.Count + " jobs?" : "this job?");
            string label = deletingMultiple ? "Delete these jobs?" : "Delete this job?";

            // if user says no to the popup, no further action required
            if (Presenter.AskQuestion(msg) != QuestionResponseEnum.Yes)
                return;

            foreach (string id in jobIds)
            {
                if (!Guid.TryParse(id, out Guid parsedId))
                    throw new Exception($"Unable to parse Job ID '{id}'");

                // Delete the job.
                await cloudInterface.DeleteJobAsync(parsedId, new CancellationToken());
                    
                // Remove the job from the locally stored list of jobs.
                jobList.Remove(GetJob(id));    
            }

            // Refresh the tree view.
            view.UpdateJobTable(jobList);

            // Restart the fetch jobs worker.
            StartRefresh();
        }

        /// <summary>Prompt the user for credentials for the selected cloud platform.</summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private Task SetCredentials(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        /// <summary>Stop the timer responsible for refreshing the job list.</summary>
        private void StopRefresh()
        {
            refresh.Change(Timeout.Infinite, Timeout.Infinite);
            cancelRefresh.Cancel();
            view.HideLoadingProgressBar();
        }

        /// <summary>Start the timer responsible for refreshing the job list.</summary>
        private void StartRefresh()
        {
            cancelRefresh = new CancellationTokenSource();
            refresh.Change(0, 10 * 1000);
        }

        /// <summary>Tests if two jobs are equal (same ID, state and progress).</summary>
        /// <param name="a">The first job.</param>
        /// <param name="b">The second job.</param>
        private bool IsEqual(JobDetails a, JobDetails b)
        {
            return (a.ID == b.ID && a.State == b.State && a.Progress == b.Progress);
        }
    }
}
