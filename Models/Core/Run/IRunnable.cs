using System;
using System.Threading;

namespace Models.Core.Run
{
    /// <summary>
    /// Simple interface that denoates that something can be run.
    /// </summary>
    public interface IRunnable
    {
        /// <summary>
        /// Progress of the job.
        /// </summary>
        double Progress { get; }

        /// <summary>The run method.</summary>
        /// <param name="statusCallback">A status callback.</param>
        /// <param name="errorCallback">A status callback.</param>
        /// <param name="cancel">An optional cancellation token.</param>
        void Run(Action<string> statusCallback, Action<Exception> errorCallback,
            CancellationTokenSource cancel = null);
    }
}
