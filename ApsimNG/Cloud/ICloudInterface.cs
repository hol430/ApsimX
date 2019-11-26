using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ApsimNG.Cloud
{
    /// <summary>
    /// A class which interfaces with a cloud platform such as AWS or Azure.
    /// </summary>
    public interface ICloudInterface
    {
        /// <summary>Submit a job (apsim simulations) to run on the cloud platform.</summary>
        /// <param name="parameters">Job parameters.</param>
        /// <param name="UpdateStatus">Function to call which can show a status update.</param>
        Task SubmitJobAsync(JobParameters parameters, CancellationToken ct, Action<string> UpdateStatus);

        /// <summary>Gets the list of jobs submitted to this cloud platform.</summary>
        /// <param name="ct">Cancellation token.</param>
        /// <param name="ShowProgress">Function to report progress as percentage in range [0, 100].</param>
        Task<List<JobDetails>> ListJobsAsync(CancellationToken ct, Action<double> ShowProgress);

        /// <summary>Abort the execution of a running job.</summary>
        /// <param name="jobId">Job ID.</param>
        /// <param name="ct">Cancellation token.</param>
        Task StopJobAsync(Guid jobId, CancellationToken ct);

        /// <summary>Aborts a job if still running and deletes all data associated with the job.</summary>
        /// <param name="jobId">Job ID.</param>
        /// <param name="ct">Cancellation token.</param>
        Task DeleteJobAsync(Guid jobId, CancellationToken ct);

        /// <summary>Download the results of a job.</summary>
        /// <param name="options">Download options.</param>
        /// <param name="ct">Cancellation token.</param>
        Task DownloadResultsAsync(DownloadOptions options, CancellationToken ct);
    }
}
