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
using ApsimNG.Cloud.Azure;

namespace ApsimNG.Cloud
{
    public class AzureInterface : ICloudInterface
    {
        /// <summary>An Azure blob client used to interact with a storage account.</summary>
        private CloudBlobClient storageClient;

        /// <summary>An Azure batch client used to interact with a batch account.</summary>
        private BatchClient batchClient;

        /// <summary>The .apsimx files are uploaded in an archive with this name.</summary>
        private const string modelZipFileName = "model.zip";

        /// <summary>The results are compressed into a file with this name.</summary>
        private const string resultsFileName = "Results.zip";

        /// <summary>Path to model directory on the Azure compute nodes.</summary>
        private const string computeNodeModelPath = "%AZ_BATCH_NODE_SHARED_DIR%\\{0}";

        /// <summary>ID of the job manager task.</summary>
        private const string jobManagerTaskName = "JobManager";

        /// <summary>Array of all valid debug file formats.</summary>
        private static readonly string[] debugFileFormats = { ".stdout", ".sum" };

        public AzureInterface()
        {
            LoadCredentials();
        }

        /// <summary>
        /// Submit a job to Azure batch.
        /// </summary>
        /// <param name="job">Job settings/options specified by the user.</param>
        /// <param name="UpdateStatus">Function which takes a status message and displays it to the user.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task SubmitJobAsync(JobParameters job, CancellationToken ct, Action<string> UpdateStatus)
        {
            string workingDirectory = Path.Combine(Path.GetTempPath(), job.ID.ToString());
            if (!Directory.Exists(workingDirectory))
                Directory.CreateDirectory(workingDirectory);

            // Create an azure storage container for this job.
            SetContainerMetaDataAsync("job-" + job.ID, "Owner", Environment.UserName.ToLower());

            if (ct.IsCancellationRequested)
            {
                UpdateStatus("Job submission cancelled");
                return;
            }

            // Upload tools required by the job manager like 7zip, AzCopy, CMail, etc.
            UpdateStatus("Checking tools");
            await UploadToolsAsync(job, workingDirectory);

            // Create/upload email config file if necessary.
            if (job.SendEmail)
                await UploadEmailFileAsync(job, workingDirectory);

            // If using apsim from a directory, it will need to be compressed.
            if (job.ApsimFromDir)
            {
                UpdateStatus("Compressing apsim binaries");
                string archive = Path.Combine(workingDirectory, "apsimx.zip");
                CompressApsimBinaries(job.ApsimPath, archive);
                job.ApsimPath = archive;
            }

            // Check Apsim version.
            string apsimVersion = GetApsimVersion(job.ApsimPath);

            // Upload apsim binaries.
            UpdateStatus("Uploading apsim binaries");
            await UploadFileAsync("apsim", job.ApsimPath);

            if (ct.IsCancellationRequested)
            {
                UpdateStatus("Job submission cancelled");
                return;
            }

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
            string modelZipFileSas = await UploadFileAsync(job.ID.ToString(), job.ModelPath);

            if (ct.IsCancellationRequested)
            {
                UpdateStatus("Job submission cancelled");
                return;
            }

            // Remove temporary .apsimx files.
            File.Delete(job.ModelPath);

            // Submit job.
            UpdateStatus("Submitting Job");

            CloudJob cloudJob = batchClient.JobOperations.CreateJob(job.ID.ToString(), GetPoolInfo());
            cloudJob.DisplayName = job.Name;
            cloudJob.JobPreparationTask = CreateJobPreparationTask(job, modelZipFileSas, apsimVersion);
            cloudJob.JobReleaseTask = CreateJobReleaseTask(job, modelZipFileSas, apsimVersion);
            cloudJob.JobManagerTask = CreateJobManagerTask(job);

            if (ct.IsCancellationRequested)
            {
                UpdateStatus("Job submission cancelled");
                return;
            }

            await cloudJob.CommitAsync();
            UpdateStatus("Job Successfully submitted");
        }

        /// <summary>
        /// Gets the list of jobs submitted to Azure.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <param name="ShowProgress">Function to report progress as percentage in range [0, 100].</param>
        public async Task<List<JobDetails>> ListJobsAsync(CancellationToken ct, Action<double> ShowProgress)
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
                    return jobs;

                ShowProgress(100.0 * i / cloudJobs.Count);
                jobs.Add(await GetJobDetails(cloudJobs[i]));
            }

            ShowProgress(100);
            return jobs;
        }

        /// <summary>Abort the execution of a running job.</summary>
        /// <param name="jobId">Job ID.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task StopJobAsync(Guid jobId, CancellationToken ct)
        {
            await batchClient.JobOperations.TerminateJobAsync(jobId.ToString(), cancellationToken: ct);
        }

        /// <summary>
        /// Aborts a job if still running and deletes all data associated with the job.
        /// </summary>
        /// <param name="jobId">Job ID. Unsure if we want to stick with GUID here in the long run but it'll do for now.
        /// <param name="ct">Cancellation token.</param>
        public async Task DeleteJobAsync(Guid jobId, CancellationToken ct)
        {
            CloudBlobContainer container = storageClient.GetContainerReference(OutputContainer(jobId));
            if (await container.ExistsAsync(ct))
                await container.DeleteAsync();

            container = storageClient.GetContainerReference(GetJobContainer(jobId));
            if (await container.ExistsAsync(ct))
                await container.DeleteAsync(ct);

            container = storageClient.GetContainerReference(jobId.ToString());
            if (await container.ExistsAsync(ct))
                await container.DeleteAsync(ct);

            await batchClient.JobOperations.DeleteJobAsync(jobId.ToString(), cancellationToken: ct);
        }

        /// <summary>Download the results of a job.</summary>
        /// <param name="options">Download options.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task DownloadResultsAsync(DownloadOptions options, CancellationToken ct)
        {
            if (!Directory.Exists(options.Path))
                Directory.CreateDirectory(options.Path);

            List<CloudBlockBlob> outputs = await GetJobOutputs(options.JobID);
            List<CloudBlockBlob> toDownload = new List<CloudBlockBlob>();

            // Build up a list of files to download.
            CloudBlockBlob results = outputs.Find(b => string.Equals(b.Name, resultsFileName, StringComparison.InvariantCultureIgnoreCase));
            if (results != null)
                toDownload.Add(results);
            else
                // Always download debug files if no results archive can be found.
                options.DownloadDebugFiles = true;

            if (options.DownloadDebugFiles)
                toDownload.AddRange(outputs.Where(blob => debugFileFormats.Contains(Path.GetExtension(blob.Name.ToLower()))));

            // Now download the necessary files.
            foreach (CloudBlockBlob blob in toDownload)
            {
                if (ct.IsCancellationRequested)
                    return;
                else
                    // todo: Download in parallel?
                    await blob.DownloadToFileAsync(Path.Combine(options.Path, blob.Name), FileMode.Create, ct);
            }

            if (options.ExtractResults)
            {
                string archive = Path.Combine(options.Path, resultsFileName);
                string resultsDir = Path.Combine(options.Path, "results");
                if (File.Exists(archive))
                {
                    // Extract the result files.
                    using (ZipArchive zip = ZipFile.Open(archive, ZipArchiveMode.Read, Encoding.UTF8))
                        zip.ExtractToDirectory(resultsDir);

                    // Merge results into a single .db file.
                    DBMerger.MergeFiles(Path.Combine(resultsDir, "*.db"), false, "combined.db");

                    // TBI: merge into csv file.
                    if (options.ExportToCsv)
                        throw new NotImplementedException();
                }
            }
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

        /// <summary>Gets default pool settings. This isn't really configurable by the user. Maybe one day...</summary>
        private PoolInformation GetPoolInfo()
        {
            string maxTasks = AzureSettings.Default.PoolMaxTasksPerVM;
            string vmCount = AzureSettings.Default.PoolVMCount;
            string vmSize = AzureSettings.Default.PoolVMSize;
            string poolName = AzureSettings.Default.PoolName;

            if (string.IsNullOrEmpty(poolName))
            {
                return new PoolInformation
                {
                    AutoPoolSpecification = new AutoPoolSpecification
                    {
                        PoolLifetimeOption = PoolLifetimeOption.Job,
                        PoolSpecification = new PoolSpecification
                        {
                            MaxTasksPerComputeNode = string.IsNullOrEmpty(maxTasks) ? (int?)null : int.Parse(maxTasks),
                            TargetDedicatedComputeNodes = string.IsNullOrEmpty(vmCount) ? (int?)null : int.Parse(vmCount),
                            VirtualMachineSize = string.IsNullOrEmpty(vmSize) ? "standard_d5_v2" : vmSize,

                            // This specifies the OS that our VM will be running.
                            // OS Family 5 means .NET 4.6 will be installed. For more info see:
                            // https://docs.microsoft.com/en-us/azure/cloud-services/cloud-services-guestos-update-matrix#releases
                            CloudServiceConfiguration = new CloudServiceConfiguration("5"),
                            ResizeTimeout = TimeSpan.FromMinutes(15),
                            TaskSchedulingPolicy = new TaskSchedulingPolicy(ComputeNodeFillType.Spread)
                        }
                    },
                    PoolId = poolName
                };
            }
            return new PoolInformation
            {
                PoolId = poolName
            };
        }

        /// <summary>Sets metadata value for a container in Azure cloud storage.</summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="key">Metadata key.</param>
        /// <param name="val">Metadata value.</param>
        private async void SetContainerMetaDataAsync(string containerName, string key, string val)
        {
            CloudBlobContainer containerRef = storageClient.GetContainerReference(containerName);
            await containerRef.CreateIfNotExistsAsync();
            containerRef.Metadata.Add(key, val);
            await containerRef.SetMetadataAsync();
        }

        /// <summary>Gets metadata value for a container in Azure cloud storage.</summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="key">Metadata key.</param>
        private async Task<string> GetContainerMetaDataAsync(string containerName, string key)
        {
            CloudBlobContainer containerRef = storageClient.GetContainerReference(containerName);
            if (!await containerRef.ExistsAsync())
                throw new Exception($"Failed to fetch metadata '{key}' for container '{containerName}' - container does not exist");

            await containerRef.FetchAttributesAsync();
            if (containerRef.Metadata.ContainsKey(key))
                return containerRef.Metadata[key];

            throw new Exception($"Failed to fetch metadata '{key}' for container '{containerName}' - key does not exist");
        }

        /// <summary>Lists all output files of a given job.</summary>
        /// <param name="jobID">Job ID.</param>
        private async Task<List<CloudBlockBlob>> GetJobOutputs(Guid jobID)
        {
            CloudBlobContainer containerRef = storageClient.GetContainerReference(OutputContainer(jobID));
            if (!await containerRef.ExistsAsync())
                return null;
            
            return await containerRef.ListBlobsAsync();
        }

        /// <summary>
        /// Upload a file to Azure's cloud storage if it does not already exist.
        /// Returns the SAS of the uploaded file.
        /// </summary>
        /// <param name="containerName">Name of the container to upload the file to</param>
        /// <param name="filePath">Path to the file on disk</param>
        private async Task<string> UploadFileAsync(string containerName, string filePath)
        {
            CloudBlobContainer containerRef = storageClient.GetContainerReference(containerName);
            await containerRef.CreateIfNotExistsAsync();
            CloudBlockBlob blobRef = containerRef.GetBlockBlobReference(Path.GetFileName(filePath));

            string md5 = GetMd5(filePath);

            // If blob exists and md5 matches, no need to upload the file.
            if (!await blobRef.ExistsAsync() || !string.Equals(md5, blobRef.Properties.ContentMD5, StringComparison.InvariantCultureIgnoreCase))
            {
                blobRef.Properties.ContentMD5 = md5;
                await blobRef.UploadFromFileAsync(filePath);
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
        private async Task UploadEmailFileAsync(JobParameters job, string workingDirectory)
        {
            // Create a config file to store email settings.
            string configFile = Path.Combine(workingDirectory, "settings.txt");
            File.WriteAllLines(configFile, new string[]
            {
                        $"EmailRecipient={job.EmailAddress}",
                        $"EmailSender={AzureSettings.Default["EmailSender"]}",
                        $"EmailPW={AzureSettings.Default["EmailPW"]}"
            });

            await UploadFileAsync("job-" + job.ID, configFile);
            File.Delete(configFile);
        }

        /// <summary>Upload all tools required to run a job.</summary>
        /// <param name="job">Job parameters.</param>
        /// <param name="workingDirectory">Working directory.</param>
        private async Task UploadToolsAsync(JobParameters job, string workingDirectory)
        {
            string tools = Path.Combine(job.ApsimPath, "Bin", "tools");
            if (!job.ApsimFromDir)
            {
                tools = Path.Combine(workingDirectory, "tools");
                Directory.CreateDirectory(tools);
                ExtractTools(job.ApsimPath, tools);
            }

            foreach (string filePath in Directory.EnumerateFiles(tools))
                await UploadFileAsync("tools", filePath);

            if (!job.ApsimFromDir)
                // If uploading apsim from a .zip archive, remember to
                // delete the tools after extracting them.
                Directory.Delete(tools, true);
        }

        /// <summary>Extract all tools files from a zip archive of the bin directory.</summary>
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

        /// <summary>Runs Models.exe and parses the output to figure out the apsim version number.</summary>
        /// <param name="apsimPath">Path to .zip file containing Models.exe.</param>
        private string GetApsimVersion(string apsimPath)
        {
            string tmp = Path.Combine(Path.GetTempPath(), $"get-apsim-version-{Guid.NewGuid()}");
            string models = Path.Combine(tmp, "Models.exe");
            Directory.CreateDirectory(tmp);

            string[] deps = new[] { "Models.exe", "APSIM.Shared.dll" };
            using (FileStream stream = new FileStream(apsimPath, FileMode.Open))
            {
                using (ZipArchive archive = new ZipArchive(stream))
                {
                    foreach (string dep in deps)
                    {
                        string path = Path.Combine(tmp, dep);
                        archive.GetEntry(dep).ExtractToFile(path, true);
                    }
                }
            }

            ProcessUtilities.ProcessWithRedirectedOutput proc = new ProcessUtilities.ProcessWithRedirectedOutput();
            proc.Start(models, "/Version", Path.GetTempPath(), true);
            proc.WaitForExit();

            Directory.Delete(tmp, true);
            
            Match match = Regex.Match(proc.StdOut, @"((\d\.){3}\d)");
            return match.Groups[0].Value;
        }

        /// <summary>Compute the md5 hash of a file.</summary>
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
        /// <param name="apsimVersion">Version of apsim to be run.</param>
        private JobPreparationTask CreateJobPreparationTask(JobParameters job, string sas, string apsimVersion)
        {
            return new JobPreparationTask
            {
                CommandLine = "cmd.exe /c jobprep.cmd",
                ResourceFiles = GetJobResourceFiles(sas, apsimVersion).ToList(),
                WaitForSuccess = true,
            };
        }

        /// <summary>
        /// Create the job release task. This task is run once all
        /// other tasks are finished.
        /// </summary>
        /// <param name="job">Job parameters.</param>
        /// <param name="sas">SAS of the compressed .apsimx files.</param>
        /// <param name="apsimVersion">Version of apsim to be run.</param>
        private JobReleaseTask CreateJobReleaseTask(JobParameters job, string sas, string apsimVersion)
        {
            return new JobReleaseTask
            {
                CommandLine = "cmd.exe /c jobrelease.cmd",
                ResourceFiles = GetJobResourceFiles(sas, apsimVersion).ToList(),
                EnvironmentSettings = new[]
                {
                    new EnvironmentSetting("APSIM_STORAGE_ACCOUNT", AzureSettings.Default["StorageAccount"].ToString()/*job.StorageAuth.Account*/),
                    new EnvironmentSetting("APSIM_STORAGE_KEY", AzureSettings.Default["StorageKey"].ToString()/*job.StorageAuth.Key*/),
                    new EnvironmentSetting("JOBNAME", job.Name),
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
                GetJobManagerPath(job.ID),
                AzureSettings.Default["BatchURL"],//job.BatchAuth.Url,
                AzureSettings.Default["BatchAccount"],//job.BatchAuth.Account,
                AzureSettings.Default["BatchKey"],//job.BatchAuth.Key,
                AzureSettings.Default["StorageAccount"],//job.StorageAuth.Account,
                AzureSettings.Default["StorageKey"],//job.StorageAuth.Key,
                job.ID,
                GetModelPath(job.ID),
                true, // should job manager submit tasks - always true
                job.AutoScale // No idea what this does but it needs to be true
            );

            return new JobManagerTask
            {
                CommandLine = cmd,
                DisplayName = "Job manager task",
                KillJobOnCompletion = true,
                Id = jobManagerTaskName,
                RunExclusive = false,
                ResourceFiles = GetResourceFiles("jobmanager").ToList(),
            };
        }

        /// <summary>Gets the resource files required by the job prep/release tasks.</summary>
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

        /// <summary>Enumerates all files in an Azure container.</summary>
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
        private async Task<JobDetails> GetJobDetails(CloudJob cloudJob)
        {
            string owner = await GetContainerMetaDataAsync($"job-{cloudJob.Id}", "Owner");

            TaskCounts tasks = await batchClient.JobOperations.GetJobTaskCountsAsync(cloudJob.Id);
            int numTasks = tasks.Active + tasks.Running + tasks.Completed;

            // If there are no tasks, set progress to 100%.
            double jobProgress = numTasks == 0 ? 100 : 100.0 * tasks.Completed / numTasks;

            // If cpu time is unavailable, set this field to 0.
            TimeSpan cpu = cloudJob.Statistics == null ? TimeSpan.Zero : cloudJob.Statistics.KernelCpuTime + cloudJob.Statistics.UserCpuTime;
            JobDetails job = new JobDetails
            {
                ID = cloudJob.Id,
                Name = cloudJob.DisplayName,
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

        /// <summary>Gets the name of a job's output container.</summary>
        /// <param name="jobId">Job ID.</param>
        private static string OutputContainer(Guid jobId)
        {
            return string.Format("job-{0}-outputs", jobId);
        }

        /// <summary>Gets the name of a job's top-level container.</summary>
        /// <param name="jobId">Job ID.</param>
        private static string GetJobContainer(Guid jobId)
        {
            return string.Format("job-{0}", jobId);
        }

        /// <summary>Gets the path to the input files for a given job.</summary>
        /// <param name="jobId">Job ID.</param>
        private static string GetJobInputPath(Guid jobId)
        {
            return string.Format(computeNodeModelPath, jobId);
        }

        /// <summary>Gets the path to the .apsimx files for a given job.</summary>
        /// <param name="jobId"></param>
        private static string GetModelPath(Guid jobId)
        {
            return Path.Combine(GetJobInputPath(jobId), "Model");
        }

        /// <summary>Gets the name of the job manager executable.</summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        private static string GetJobManagerPath(Guid jobId)
        {
            return string.Format("azure-apsim.exe");
        }
    }
}
