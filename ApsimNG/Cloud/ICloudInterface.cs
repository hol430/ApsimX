using System;
using System.Collections.Generic;
using System.Threading;

namespace ApsimNG.Cloud
{
    /// <summary>
    /// A class which interfaces with a cloud platform such as AWS or Azure.
    /// </summary>
    public interface ICloudInterface
    {
        /// <summary>
        /// Submit a job (apsim simulations) to run on the cloud platform.
        /// </summary>
        /// <param name="parameters">Job parameters.</param>
        /// <param name="UpdateStatus">Function to call which can show a status update.</param>
        void SubmitJob(JobParameters parameters, Action<string> UpdateStatus);

        /// <summary>
        /// Gets the list of jobs submitted to this cloud platform.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <param name="ShowProgress">Function to report progress as percentage in range [0, 100].</param>
        List<JobDetails> ListJobs(CancellationToken ct, Action<double> ShowProgress);

        /// <summary>
        /// Abort the execution of a running job.
        /// </summary>
        /// <param name="jobId">Job ID.</param>
        /// <remarks>
        /// DH - I'm not sure how jobs will be identified in future cloud
        /// platforms but let's cross that bridge when we come to it and
        /// just assume for now that they'll work with a Guid.
        /// </remarks>
        void StopJob(Guid jobId);

        /// <summary>
        /// Aborts a job if still running and deletes all data associated with the job.
        /// </summary>
        /// <param name="jobId">Job ID.</param>
        /// <remarks>
        /// DH - I'm not sure how jobs will be identified in future cloud
        /// platforms but let's cross that bridge when we come to it and
        /// just assume for now that they'll work with a Guid.
        /// </remarks>
        void DeleteJob(Guid jobId);

        /// <summary>
        /// Download the results of a job.
        /// </summary>
        /// <param name="jobId">Job ID.</param>
        /// <param name="path">Path to which the results will be downloaded.</param>
        /// <remarks>
        /// DH - I'm not sure how jobs will be identified in future cloud
        /// platforms but let's cross that bridge when we come to it and
        /// just assume for now that they'll work with a Guid.
        /// </remarks>
        void DownloadResults(Guid jobId, string path);
    }
}
