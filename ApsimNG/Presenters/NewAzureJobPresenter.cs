using System;
using System.Text;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using UserInterface.Views;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Storage;
using System.Security.Cryptography;
using ApsimNG.Cloud;
using Microsoft.Azure.Batch.Common;
using Models.Core;
using System.Linq;
using System.Reflection;
using Models;
using ApsimNG.Cloud.Azure;
using ApsimNG.Interfaces;

namespace UserInterface.Presenters
{
    public class NewAzureJobPresenter : IPresenter
    {
        /// <summary>The new azure job view</summary>
        private INewCloudJobView view;

        /// <summary>The explorer presenter</summary>
        private ExplorerPresenter presenter;

        /// <summary>
        /// The node which we want to run on Azure.
        /// </summary>
        private IModel model;
        
        /// <summary>
        /// The worker which will submit the job.
        /// </summary>
        private BackgroundWorker submissionWorker;

        /// <summary>
        /// The settings file name. This is uploaded to Azure, and stores some information
        /// used by the Azure APSIM job manager (azure-apsim.exe).
        /// </summary>
        private const string settingsFileName = "settings.txt";

        /// <summary>
        /// Default constructor.
        /// </summary>
        public NewAzureJobPresenter()
        {
        }

        /// <summary>
        /// Attaches this presenter to a view.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="view"></param>
        /// <param name="parentPresenter"></param>
        public void Attach(object model, object view, ExplorerPresenter parentPresenter)
        {
            this.presenter = parentPresenter;
            this.view = (INewCloudJobView)view;

            this.model = (IModel)model;
            this.view.JobName = this.model.Name;

            this.view.OKClicked += OnOKClicked;
            this.view.CancelClicked += OnCancelClicked;

            submissionWorker = new BackgroundWorker();
            submissionWorker.DoWork += SubmitJob_DoWork;
            submissionWorker.WorkerSupportsCancellation = true;
        }

        /// <summary>
        /// Detach the view from the presenter.
        /// </summary>
        public void Detach()
        {
            view.CancelClicked -= OnCancelClicked;
            view.OKClicked -= OnOKClicked;
        }

        /// <summary>
        /// Validates user input, saves their choices and starts the job submission in a separate thread.
        /// </summary>
        /// <param name="jp">Job Parameters.</param>
        private void SubmitJob(JobParameters jp)
        {
            if (string.IsNullOrWhiteSpace(jp.JobDisplayName))
                throw new Exception("Display name not provided");

            if (string.IsNullOrWhiteSpace(jp.ApsimPath))
                throw new Exception("APSIM directory/zip file not provided.");

            if (! (Directory.Exists(jp.ApsimPath) || File.Exists(jp.ApsimPath)) )
                throw new Exception($"File or Directory not found: '{jp.ApsimPath}'");

            if (jp.CoresPerProcess.ToString().Length < 1)
                throw new Exception("Number of cores per CPU not provided");

            if (jp.SaveModelFiles && string.IsNullOrWhiteSpace(jp.ModelPath))
                throw new Exception("Model file output directory not provided.");

            if (string.IsNullOrWhiteSpace(jp.OutputDir))
                throw new Exception("Output directory not provided.");
            
            // save user's choices to ApsimNG.Properties.Settings
            AzureSettings.Default["OutputDir"] = jp.OutputDir;
            AzureSettings.Default.Save();
            submissionWorker.RunWorkerAsync(jp);
        }

        /// <summary>
        /// Invoked when the user clicks the cancel button. Cancels
        /// submission of a job.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnCancelClicked(object sender, EventArgs args)
        {
            if (submissionWorker != null)
                submissionWorker.CancelAsync();
        }

        /// <summary>
        /// Invoked when the user clicks the OK button to submit the job.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnOKClicked(object sender, EventArgs args)
        {
            JobParameters parameters = new JobParameters
            {
                JobId = Guid.NewGuid(),
                JobDisplayName = view.JobName,
                PoolVMCount = view.NumCores / 16,
                SaveModelFiles = view.SaveModelFiles,
                ModelPath = view.ModelFilePath,
                ApsimFromDir = view.ApsimFromDirectory,
                OutputDir = view.OutputPath,
                AutoDownload = view.AutoDownload,
                ApsimVersion = null, // TBI
                ApsimPath = view.ApsimPath,
                Model = model,
                SendEmail = view.SendEmail,
                EmailAddress = view.EmailAddress,
            };

            submissionWorker.RunWorkerAsync(parameters);
        }

        /// <summary>
        /// Handles the bulk of the work for submitting the job to the cloud. 
        /// Zips up ApsimX (if necessary), uploads tools and ApsimX, 
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments containing the job parameters.</param>
        private void SubmitJob_DoWork(object sender, DoWorkEventArgs args)
        {
            try
            {
                JobParameters job = (JobParameters)args.Argument;

                // TBI : a method of selecting a cloud platform.
                AzureInterface azure = new AzureInterface();
                azure.SubmitJob(job, status => view.Status = status);

                if (job.AutoDownload)
                {
                    // Start a results downloader in another thread.
                    AzureResultsDownloader dl = new AzureResultsDownloader(job.JobId, job.JobDisplayName, job.OutputDir, null, true, false, true, true, true);
                    dl.DownloadResults(true);
                }
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
            }
        }
    }
}
