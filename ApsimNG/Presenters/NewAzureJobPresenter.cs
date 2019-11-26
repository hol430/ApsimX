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
using System.Threading;

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
        /// Cancellation token for job submission.
        /// </summary>
        private CancellationTokenSource ct;

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

            ct = new CancellationTokenSource();

            // Pre-populate some of the view's fields.
            this.view.JobName = this.model.Name;
            this.view.EmailAddress = AzureSettings.Default.EmailRecipient;

            // Attach event handlers.
            this.view.OKClicked += OnOKClicked;
            this.view.CancelClicked += OnCancelClicked;

            submissionWorker = new BackgroundWorker();
            submissionWorker.DoWork += OnSubmitJob;
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
            if (string.IsNullOrWhiteSpace(jp.Name))
                throw new Exception("Display name not provided");

            if (string.IsNullOrWhiteSpace(jp.ApsimPath))
                throw new Exception("APSIM directory/zip file not provided.");

            if (! (Directory.Exists(jp.ApsimPath) || File.Exists(jp.ApsimPath)) )
                throw new Exception($"File or Directory not found: '{jp.ApsimPath}'");

            if (jp.CoresPerProcess.ToString().Length < 1)
                throw new Exception("Number of cores per CPU not provided");

            if (jp.SaveModelFiles && string.IsNullOrWhiteSpace(jp.ModelPath))
                throw new Exception("Model file output directory not provided.");

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
            {
                ct.Cancel();
                submissionWorker.CancelAsync();
            }
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
                ID = Guid.NewGuid(),
                Name = view.JobName,
                PoolVMCount = view.NumCores / 16,
                SaveModelFiles = view.SaveModelFiles,
                ModelPath = view.ModelFilePath,
                ApsimFromDir = view.ApsimFromDirectory,
                //ApsimVersion = null, // TBI
                ApsimPath = view.ApsimPath,
                Model = model,
                SendEmail = view.SendEmail,
                EmailAddress = view.EmailAddress,
            };

            // Save some settings for next time.
            AzureSettings.Default.EmailRecipient = view.EmailAddress;
            AzureSettings.Default.Save();

            submissionWorker.RunWorkerAsync(parameters);
        }

        /// <summary>Creates and submits a job to be run on the cloud.</summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments containing the job parameters.</param>
        private async void OnSubmitJob(object sender, DoWorkEventArgs args)
        {
            try
            {
                JobParameters job = (JobParameters)args.Argument;

                // TBI : a method of selecting a cloud platform.
                AzureInterface azure = new AzureInterface();
                await azure.SubmitJobAsync(job, ct.Token, status => view.Status = status);
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
            }
        }
    }
}
