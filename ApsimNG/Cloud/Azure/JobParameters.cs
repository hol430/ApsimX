using Models.Core;
using System;

namespace ApsimNG.Cloud
{
    public class JobParameters
    {
        /// <summary>
        /// Display name of the job.
        /// </summary>
        public string JobDisplayName { get; set; }

        /// <summary>
        /// Unique ID of the job.
        /// </summary>
        public Guid JobId { get; set; }

        /// <summary>
        /// Model to be run.
        /// </summary>
        public IModel Model { get; set; }

        /// <summary>
        /// Directory to save results to.
        /// </summary>
        public string OutputDir { get; set; }

        /// <summary>
        /// State of the job (active, running, complete...).
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Directory that model files should be saved to.
        /// </summary>
        public string ModelPath { get; set; }

        /// <summary>
        /// If true, ApplicationPackagePath points to a directory. If false, it points to a .zip file.
        /// </summary>
        public bool ApsimFromDir { get; set; }

        /// <summary>
        /// Directory or zip file containing ApsimX to be uploaded.
        /// </summary>
        public string ApsimPath { get; set; }

        /// <summary>
        /// ApsimX Version.
        /// </summary>
        public string ApsimVersion { get; set; }

        /// <summary>
        /// An email will be sent to this address when the job finishes.
        /// </summary>
        public string EmailAddress { get; set; }

        /// <summary>
        /// Number of cores per process.
        /// </summary>
        public int CoresPerProcess { get { return 1; } }

        /// <summary>
        /// Number of VMs per pool.
        /// </summary>
        public int PoolVMCount { get; set; }

        /// <summary>
        /// Maximum number of tasks allowed on a single VM.
        /// </summary>
        public int PoolMaxTasksPerVM { get { return 16; } }

        /// <summary>
        /// If true, results will automatically be downloaded once the job is finished.
        /// </summary>
        public bool AutoDownload { get; set; }

        /// <summary>
        /// If true, model files will be saved after they are generated.
        /// </summary>
        public bool SaveModelFiles { get; set; }

        /// <summary>
        /// If true, the job manager will submit the tasks.
        /// </summary>
        public bool JobManagerShouldSubmitTasks { get { return true; } }

        /// <summary>
        /// Unsure what this is...pretty sure it's somehow used by the
        /// job manager (azure-apsim.exe).
        /// </summary>
        public bool AutoScale { get { return true; } }

        /// <summary>
        /// If true, an email will be sent to a specified address when the job finishes.
        /// </summary>
        public bool SendEmail { get; set; }
    }
}