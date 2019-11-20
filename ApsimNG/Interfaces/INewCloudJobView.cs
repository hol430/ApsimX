using System;

namespace ApsimNG.Interfaces
{
    public interface INewCloudJobView
    {
        /// <summary>
        /// Invoked when the user clicks the OK button.
        /// </summary>
        event EventHandler OKClicked;

        /// <summary>
        /// Invoked when the user clicks the Cancel button.
        /// </summary>
        event EventHandler CancelClicked;

        /// <summary>
        /// Job submission status.
        /// </summary>
        string Status { get; set; }

        /// <summary>
        /// Name or description of the job.
        /// </summary>
        string JobName { get; set;  }

        /// <summary>
        /// Number of VM cores to use to run the job.
        /// </summary>
        int NumCores { get; }

        /// <summary>
        /// Keep temporary .apsimx files after job submission?
        /// </summary>
        bool SaveModelFiles { get; }

        /// <summary>
        /// Optional path to which .apsimx files will be saved.
        /// </summary>
        string ModelFilePath { get; }

        /// <summary>
        /// Used to specify apsim version - if true, use a directory,
        /// otherwise, use a .zip file.
        /// </summary>
        bool ApsimFromDirectory { get; }

        /// <summary>
        /// Specifies path to apsim on disk. Can be .zip or directory.
        /// </summary>
        string ApsimPath { get; }

        /// <summary>
        /// Send an email to user upon job completion?
        /// </summary>
        bool SendEmail { get; }

        /// <summary>
        /// Email address.
        /// </summary>
        string EmailAddress { get; }

        /// <summary>
        /// Iff true, results will be automatically downloaded when the
        /// job finishes.
        /// </summary>
        bool AutoDownload { get; }

        /// <summary>
        /// Path to which results will be downloaded.
        /// </summary>
        string OutputPath { get; }
    }
}
