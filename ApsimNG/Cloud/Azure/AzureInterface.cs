using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Storage;
using Models;
using Models.Core;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Batch.Auth;
using Models.Core.Run;
using Microsoft.Azure.Storage.Blob;
using System.Security.Cryptography;
using Microsoft.Azure.Batch.Common;
using System.Threading;
using APSIM.Shared.Utilities;
using System.Text.RegularExpressions;

namespace ApsimNG.Cloud.Azure
{
    public class AzureInterface : ICloudInterface
    {
        /// <summary>An Azure blob client used to interact with a storage account.</summary>
        private CloudBlobClient storageClient;

        /// <summary>An Azure batch client used to interact with a batch account.</summary>
        private BatchClient batchClient;

        /// <summary>
        /// The .apsimx files are uploaded in an archive with this name.
        /// </summary>
        public const string modelZipFileName = "model.zip";

        public AzureInterface()
        {
            LoadCredentials();
        }

        /// <summary>
        /// Submit a job to Azure batch.
        /// </summary>
        /// <param name="job">Job settings/options specified by the user.</param>
        /// <param name="UpdateStatus">Function which takes a status message and displays it to the user.</param>
        public void SubmitJob(JobParameters job, Action<string> UpdateStatus)
        {
            string workingDirectory = Path.Combine(Path.GetTempPath(), job.JobId.ToString());
            if (!Directory.Exists(workingDirectory))
                Directory.CreateDirectory(workingDirectory);

            // Create an azure storage container for this job.
            SetContainerMetaData("job-" + job.JobId, "Owner", Environment.UserName.ToLower());

            // Check Apsim version.
            job.ApsimVersion = GetApsimVersion(job.ApsimPath);

            // Upload tools required by the job manager like 7zip, AzCopy, CMail, etc.
            UpdateStatus("Checking tools");
            UploadTools(job, workingDirectory);

            // Create/upload email config file if necessary.
            if (job.SendEmail)
                UploadEmailFile(job, workingDirectory);

            // If using apsim from a directory, it will need to be compressed.
            if (job.ApsimFromDir)
            {
                UpdateStatus("Compressing apsim binaries");
                string archive = Path.Combine(workingDirectory, "apsimx.zip");
                CompressApsimBinaries(job.ApsimPath, archive);
                job.ApsimPath = archive;
            }

            // Upload apsim binaries.
            UpdateStatus("Uploading apsim binaries");
            UploadFileIfNeeded("apsim", job.ApsimPath);

            // Generate .apsimx files.
            UpdateStatus("Generating .apsimx files");
            GenerateModelFiles(job, workingDirectory);
            
            // Compress .apsimx files.
            UpdateStatus("Compressing .apsimx files");
            string modelZip = Path.Combine(workingDirectory, modelZipFileName);
            ZipFile.CreateFromDirectory(job.ModelPath, modelZip, CompressionLevel.Fastest, false);
            if (!job.SaveModelFiles)
                Directory.Delete(job.ModelPath, true);
            job.ModelPath = modelZip;

            // Upload .apsimx files.
            UpdateStatus("Uploading .apsimx files");
            string modelZipFileSas = UploadFileIfNeeded(job.JobId.ToString(), job.ModelPath);

            // Remove temporary .apsimx files.
            File.Delete(job.ModelPath);

            // Submit job.
            UpdateStatus("Submitting Job");

            CloudJob cloudJob = batchClient.JobOperations.CreateJob(job.JobId.ToString(), GetPoolInfo(PoolSettings.FromConfiguration()));
            cloudJob.DisplayName = job.JobDisplayName;
            cloudJob.JobPreparationTask = CreateJobPreparationTask(job, modelZipFileSas);
            cloudJob.JobReleaseTask = CreateJobReleaseTask(job, modelZipFileSas);
            cloudJob.JobManagerTask = CreateJobManagerTask(job);

            cloudJob.Commit();
            UpdateStatus("Job Successfully submitted");
        }

        /// <summary>
        /// Gets the list of jobs submitted to Azure.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <param name="ShowProgress">Function to report progress as percentage in range [0, 100].</param>
        public List<JobDetails> ListJobs(CancellationToken ct, Action<double> ShowProgress)
        {
            ShowProgress(0);

            IEnumerable<CloudPool> pools = batchClient.PoolOperations.ListPools();
            ODATADetailLevel jobDetailLevel = new ODATADetailLevel { SelectClause = "id,displayName,state,executionInfo,stats", ExpandClause = "stats" };

            // Download raw job list via the Azure API.
            List<CloudJob> cloudJobs = batchClient.JobOperations.ListJobs(jobDetailLevel).ToList();

            // Parse jobs into a list of JobDetails objects.
            List<JobDetails> jobs = new List<JobDetails>();
            for (int i = 0; i < cloudJobs.Count; i++)
            {
                if (ct.IsCancellationRequested)
                    return null;

                ShowProgress(100.0 * i / cloudJobs.Count);
                jobs.Add(GetJobDetails(cloudJobs[i]));
            }

            ShowProgress(100);
            return jobs;
        }

        /// <summary>
        /// Abort the execution of a running job.
        /// </summary>
        /// <param name="jobId">Job ID.</param>
        public void StopJob(Guid jobId)
        {
            batchClient.JobOperations.TerminateJob(jobId.ToString());
        }

        /// <summary>
        /// Aborts a job if still running and deletes all data associated with the job.
        /// </summary>
        /// <param name="jobId">Job ID. Unsure if we want to stick with GUID here in the long run but it'll do for now.
        public void DeleteJob(Guid jobId)
        {
            CloudBlobContainer container = storageClient.GetContainerReference(GetJobOutputContainer(jobId));
            if (container.Exists())
                container.Delete();

            container = storageClient.GetContainerReference(GetJobContainer(jobId));
            if (container.Exists())
                container.Delete();

            container = storageClient.GetContainerReference(jobId.ToString());
            if (container.Exists())
                container.Delete();

            batchClient.JobOperations.DeleteJob(jobId.ToString());
        }

        /// <summary>
        /// Download the results of a job.
        /// </summary>
        /// <param name="jobId">Job ID.</param>
        /// <param name="path">Path to which the results will be downloaded.</param>
        public void DownloadResults(Guid jobId, string path)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Loads Azure storage/batch credentials from config file.
        /// Prompts user for credentials if none have been provided.
        /// </summary>
        private void LoadCredentials()
        {
            AzureCredentialsSetup.GetCredentialsFromUser();

            string storageAccountName = AzureSettings.Default["StorageAccount"]?.ToString();
            string storageKey = AzureSettings.Default["StorageKey"]?.ToString();
            StorageCredentials storageAuth = new StorageCredentials(storageAccountName, storageKey);
            CloudStorageAccount storageAccount = new CloudStorageAccount(storageAuth, true);
            storageClient = storageAccount.CreateCloudBlobClient();

            string batchAccount = AzureSettings.Default["BatchAccount"]?.ToString();
            string batchKey = AzureSettings.Default["BatchKey"]?.ToString();
            string batchUrl = AzureSettings.Default["BatchURL"]?.ToString();
            BatchSharedKeyCredentials batchAuth = new BatchSharedKeyCredentials(batchUrl, batchAccount, batchKey);
            batchClient = BatchClient.Open(batchAuth);
        }

        private PoolInformation GetPoolInfo(PoolSettings settings)
        {
            if (string.IsNullOrEmpty(settings.PoolName))
            {
                return new PoolInformation
                {
                    AutoPoolSpecification = new AutoPoolSpecification
                    {
                        PoolLifetimeOption = PoolLifetimeOption.Job,
                        PoolSpecification = new PoolSpecification
                        {
                            MaxTasksPerComputeNode = settings.MaxTasksPerVM,

                            // This specifies the OS that our VM will be running.
                            // OS Family 5 means .NET 4.6 will be installed. For more info see:
                            // https://docs.microsoft.com/en-us/azure/cloud-services/cloud-services-guestos-update-matrix#releases
                            CloudServiceConfiguration = new CloudServiceConfiguration("5"),
                            ResizeTimeout = TimeSpan.FromMinutes(15),
                            TargetDedicatedComputeNodes = settings.VMCount,
                            VirtualMachineSize = settings.VMSize,
                            TaskSchedulingPolicy = new TaskSchedulingPolicy(ComputeNodeFillType.Spread)
                        }
                    }
                };
            }
            return new PoolInformation
            {
                PoolId = settings.PoolName
            };
        }

        /// <summary>
        /// Sets metadata value for a container in Azure cloud storage.
        /// </summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="key">Metadata key.</param>
        /// <param name="val">Metadata value.</param>
        private void SetContainerMetaData(string containerName, string key, string val)
        {
            CloudBlobContainer containerRef = storageClient.GetContainerReference(containerName);
            containerRef.CreateIfNotExists();
            containerRef.Metadata.Add(key, val);
            containerRef.SetMetadata();
        }

        /// <summary>
        /// Gets metadata value for a container in Azure cloud storage.
        /// </summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="key">Metadata key.</param>
        private string GetContainerMetaData(string containerName, string key)
        {
            CloudBlobContainer containerRef = storageClient.GetContainerReference(containerName);
            if (!containerRef.Exists())
                throw new Exception($"Failed to fetch metadata '{key}' for container '{containerName}' - container does not exist");

            containerRef.FetchAttributes();
            if (containerRef.Metadata.ContainsKey(key))
                return containerRef.Metadata[key];

            throw new Exception($"Failed to fetch metadata '{key}' for container '{containerName}' - key does not exist");
        }

        /// <summary>
        /// Upload a file to Azure's cloud storage if it does not already exist.
        /// Returns the SAS of the uploaded file.
        /// </summary>
        /// <param name="containerName">Name of the container to upload the file to</param>
        /// <param name="filePath">Path to the file on disk</param>
        private string UploadFileIfNeeded(string containerName, string filePath)
        {
            CloudBlobContainer containerRef = storageClient.GetContainerReference(containerName);
            containerRef.CreateIfNotExists();
            CloudBlockBlob blobRef = containerRef.GetBlockBlobReference(Path.GetFileName(filePath));

            string md5 = GetMd5(filePath);

            // If blob exists and md5 matches, no need to upload the file.
            if (!blobRef.Exists() || !string.Equals(md5, blobRef.Properties.ContentMD5, StringComparison.InvariantCultureIgnoreCase))
            {
                blobRef.Properties.ContentMD5 = md5;
                blobRef.UploadFromFile(filePath);
            }

            var policy = new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-15),
                SharedAccessExpiryTime = DateTime.UtcNow.AddMonths(12)
            };

            return blobRef.Uri.AbsoluteUri + blobRef.GetSharedAccessSignature(policy);
        }

        /// <summary>
        /// Generates the settings.txt file used to store data for
        /// email which gets sent upon job completion.
        /// </summary>
        /// <param name="job">Job parameters.</param>
        /// <param name="workingDirectory">Working directory.</param>
        private void UploadEmailFile(JobParameters job, string workingDirectory)
        {
            // Create a config file to store email settings.
            string configFile = Path.Combine(workingDirectory, "settings.txt");
            File.WriteAllLines(configFile, new string[]
            {
                        $"EmailRecipient={job.EmailAddress}",
                        $"EmailSender={AzureSettings.Default["EmailSender"]}",
                        $"EmailPW={AzureSettings.Default["EmailPW"]}"
            });

            UploadFileIfNeeded("job-" + job.JobId, configFile);
            File.Delete(configFile);
        }

        /// <summary>
        /// Upload all tools required to run a job.
        /// </summary>
        /// <param name="job"></param>
        /// <param name="workingDirectory"></param>
        private void UploadTools(JobParameters job, string workingDirectory)
        {
            string tools = Path.Combine(job.ApsimPath, "Bin", "tools");
            if (!job.ApsimFromDir)
            {
                tools = Path.Combine(workingDirectory, "tools");
                Directory.CreateDirectory(tools);
                ExtractTools(job.ApsimPath, tools);
            }

            foreach (string filePath in Directory.EnumerateFiles(tools))
                UploadFileIfNeeded("tools", filePath);

            Directory.Delete(tools, true);
        }

        /// <summary>
        /// Extract all tools files from a zip archive of the bin
        /// directory.
        /// </summary>
        /// <param name="archiveName">Archive of the apsim bin directory.</param>
        /// <param name="tools">Output directory.</param>
        private void ExtractTools(string archiveName, string tools)
        {
            using (FileStream stream = new FileStream(archiveName, FileMode.Open))
            using (ZipArchive archive = new ZipArchive(stream))
                foreach (ZipArchiveEntry file in archive.Entries)
                    if (file.FullName.StartsWith("tools"))
                        file.ExtractToFile(Path.Combine(tools, file.Name));
        }

        /// <summary>
        /// Runs Models.exe and parses the output to figure out the
        /// apsim version number.
        /// </summary>
        /// <param name="apsimPath">Path to .zip file containing Models.exe.</param>
        /// <returns></returns>
        private string GetApsimVersion(string apsimPath)
        {
            string models = Path.Combine(Path.GetTempPath(), "Models.exe");
            using (FileStream stream = new FileStream(apsimPath, FileMode.Open))
            using (ZipArchive archive = new ZipArchive(stream))
                archive.GetEntry("Models.exe").ExtractToFile(models, true);

            ProcessUtilities.ProcessWithRedirectedOutput proc = new ProcessUtilities.ProcessWithRedirectedOutput();
            proc.Start(models, "/Version", Path.GetTempPath(), true);
            proc.WaitForExit();

            Match match = Regex.Match(proc.StdOut, @"((\d\.){3}\d)");
            return match.Groups[0].Value;
        }

        /// <summary>
        /// Get the md5 hash of a file.
        /// </summary>
        /// <param name="filePath">Path to the file.</param>
        private string GetMd5(string filePath)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
                return Convert.ToBase64String(md5.ComputeHash(stream));
        }

        /// <summary>
        /// Zips up the apsimx bin directory. This assumes that all .dll and .exe
        /// files in this directory are required to run apsim.
        /// </summary>
        /// <param name="srcPath">Path of the ApsimX directory.</param>
        /// <param name="zipPath">Path to which the zip file will be saved.</param>
        private void CompressApsimBinaries(string srcPath, string zipPath)
        {
            try
            {
                using (FileStream zipToOpen = new FileStream(zipPath, FileMode.Create))
                {
                    using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                    {
                        string bin = Path.Combine(srcPath, "Bin");
                        string[] extensions = { "*.dll", "*.exe" };
                        foreach (string extension in extensions)
                            foreach (string fileName in Directory.EnumerateFiles(bin, extension))
                                archive.CreateEntryFromFile(fileName, Path.GetFileName(fileName));
                    }
                }
            }
            catch (Exception err)
            {
                throw new Exception("Error zipping up APSIM", err);
            }
        }

        /// <summary>
        /// Generate one .apsimx file per simulation. Save them all to
        /// a central location along with the weather files.
        /// </summary>
        /// <param name="job">Job parameters.</param>
        /// <param name="workingDirectory">Working directory.</param>
        private void GenerateModelFiles(JobParameters job, string workingDirectory)
        {
            if (!job.SaveModelFiles)
                job.ModelPath = Path.Combine(workingDirectory, "model");

            if (!Directory.Exists(job.ModelPath))
                Directory.CreateDirectory(job.ModelPath);

            Simulations sims = Apsim.Parent(job.Model, typeof(Simulations)) as Simulations;

            // Copy weather files to models directory to be zipped up.
            // todo : other classes can use external files as well - how do we want to handle these?
            foreach (Weather met in Apsim.ChildrenRecursively(job.Model, typeof(Weather)))
            {
                if (Path.GetDirectoryName(met.FullFileName) != Path.GetDirectoryName(sims.FileName))
                    throw new Exception($"Weather file {met.FullFileName} must be in the same directory as .apsimx file");

                string sourceFile = met.FullFileName;
                string destFile = Path.Combine(job.ModelPath, met.FileName);
                File.Copy(sourceFile, destFile, true);
            }

            // todo : show progress bar?
            var runner = new Runner(job.Model);
            var errors = GenerateApsimXFiles.Generate(runner, job.ModelPath, p => { });
        }

        /// <summary>
        /// Create the job preparation task. This task is run once on
        /// each compute node before any tasks are run on that node.
        /// </summary>
        /// <param name="job">Job parameters.</param>
        /// <param name="sas">SAS of the compressed .apsimx files.</param>
        private JobPreparationTask CreateJobPreparationTask(JobParameters job, string sas)
        {
            return new JobPreparationTask
            {
                CommandLine = "cmd.exe /c jobprep.cmd",
                ResourceFiles = GetJobResourceFiles(sas, job.ApsimVersion).ToList(),
                WaitForSuccess = true,
            };
        }

        /// <summary>
        /// Create the job release task. This task is run once all
        /// other tasks are finished.
        /// </summary>
        /// <param name="job">Job parameters.</param>
        /// <param name="sas">SAS of the compressed .apsimx files.</param>
        private JobReleaseTask CreateJobReleaseTask(JobParameters job, string sas)
        {
            return new JobReleaseTask
            {
                CommandLine = "cmd.exe /c jobrelease.cmd",
                ResourceFiles = GetJobResourceFiles(sas, job.ApsimVersion).ToList(),
                EnvironmentSettings = new[]
                {
                    new EnvironmentSetting("APSIM_STORAGE_ACCOUNT", AzureSettings.Default["StorageAccount"].ToString()/*job.StorageAuth.Account*/),
                    new EnvironmentSetting("APSIM_STORAGE_KEY", AzureSettings.Default["StorageKey"].ToString()/*job.StorageAuth.Key*/),
                    new EnvironmentSetting("JOBNAME", job.JobDisplayName),
                    new EnvironmentSetting("RECIPIENT", job.EmailAddress),
                }
            };
        }

        /// <summary>
        /// Creates the job manager task.
        /// </summary>
        /// <param name="job">Job parameters.</param>
        public JobManagerTask CreateJobManagerTask(JobParameters job)
        {
            // todo : find a better way to handle credentials
            var cmd = string.Format("cmd.exe /c {0} job-manager {1} {2} {3} {4} {5} {6} {7} {8} {9}",
                BatchConstants.GetJobManagerPath(job.JobId),
                AzureSettings.Default["BatchURL"],//job.BatchAuth.Url,
                AzureSettings.Default["BatchAccount"],//job.BatchAuth.Account,
                AzureSettings.Default["BatchKey"],//job.BatchAuth.Key,
                AzureSettings.Default["StorageAccount"],//job.StorageAuth.Account,
                AzureSettings.Default["StorageKey"],//job.StorageAuth.Key,
                job.JobId,
                BatchConstants.GetModelPath(job.JobId),
                job.JobManagerShouldSubmitTasks,
                job.AutoScale
            );

            return new JobManagerTask
            {
                CommandLine = cmd,
                DisplayName = "Job manager task",
                KillJobOnCompletion = true,
                Id = BatchConstants.JobManagerName,
                RunExclusive = false,
                ResourceFiles = GetResourceFiles("jobmanager").ToList(),
            };
        }

        /// <summary>
        /// Gets the resource files required by the job prep/release
        /// tasks.
        /// </summary>
        /// <param name="sas">SAS of the compressed .apimx files.</param>
        /// <param name="version">Version of apsim to be run.</param>
        private IEnumerable<ResourceFile> GetJobResourceFiles(string sas, string version)
        {
            List<ResourceFile> files = new List<ResourceFile>();

            // .apsimx files
            files.Add(ResourceFile.FromUrl(sas, modelZipFileName));

            // Tools (7zip, AZCopy, etc.)
            files.AddRange(GetResourceFiles("tools"));

            // Apsim binaries.
            files.AddRange(GetResourceFiles("apsim").Where(f => f.FilePath.ToLower().Contains(version.ToLower())));

            return files;
        }

        /// <summary>
        /// Enumerates all files in an Azure container.
        /// </summary>
        /// <param name="containerName">Name of the container.</param>
        private IEnumerable<ResourceFile> GetResourceFiles(string containerName)
        {
            CloudBlobContainer container = storageClient.GetContainerReference(containerName);
            foreach (CloudBlockBlob listBlobItem in container.ListBlobs())
            {
                var signature = listBlobItem.GetSharedAccessSignature(new SharedAccessBlobPolicy
                {
                    SharedAccessStartTime = DateTime.UtcNow.AddHours(-1),
                    SharedAccessExpiryTime = DateTime.UtcNow.AddMonths(2),
                    Permissions = SharedAccessBlobPermissions.Read,
                });
                yield return ResourceFile.FromUrl(listBlobItem.Uri.AbsoluteUri + signature, listBlobItem.Name);
            }
        }

        /// <summary>
        /// Translates an Azure-specific CloudDetails object into a
        /// generic JobDetails object which can be passed back to the
        /// presenter.
        /// </summary>
        /// <param name="cloudJob"></param>
        private JobDetails GetJobDetails(CloudJob cloudJob)
        {
            string owner = GetContainerMetaData($"job-{cloudJob.Id}", "Owner");

            TaskCounts tasks = batchClient.JobOperations.GetJobTaskCounts(cloudJob.Id);
            int numTasks = tasks.Active + tasks.Running + tasks.Completed;

            // If there are no tasks, set progress to 100%.
            double jobProgress = numTasks == 0 ? 100 : 100.0 * tasks.Completed / numTasks;

            // If cpu time is unavailable, set this field to 0.
            TimeSpan cpu = cloudJob.Statistics == null ? TimeSpan.Zero : cloudJob.Statistics.KernelCpuTime + cloudJob.Statistics.UserCpuTime;
            JobDetails job = new JobDetails
            {
                Id = cloudJob.Id,
                DisplayName = cloudJob.DisplayName,
                State = cloudJob.State.ToString(),
                Owner = owner,
                NumSims = numTasks - 1, // subtract one because one of these is the job manager
                Progress = jobProgress,
                CpuTime = cpu
            };

            if (cloudJob.ExecutionInformation != null)
            {
                job.StartTime = cloudJob.ExecutionInformation.StartTime;
                job.EndTime = cloudJob.ExecutionInformation.EndTime;
            }

            return job;
        }

        /// <summary>
        /// Gets the name of a job's output container.
        /// </summary>
        /// <param name="jobId">Job ID.</param>
        private static string GetJobOutputContainer(Guid jobId)
        {
            return string.Format("job-{0}-outputs", jobId);
        }

        /// <summary>
        /// Gets the name of a job's top-level container.
        /// </summary>
        /// <param name="jobId">Job ID.</param>
        private static string GetJobContainer(Guid jobId)
        {
            return string.Format("job-{0}", jobId);
        }
    }
}
