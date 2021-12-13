using System;
using System.Threading;

namespace Models.Core.Run
{
    /// <summary>
    /// Simple interface that denoates that something can be run.
    /// </summary>
    public interface IRunnable
    {
        /// <summary>The run method.</summary>
        /// <param name="statusCallback">A status callback.</param>
        /// <param name="progressCallback">A status callback.</param>
        /// <param name="errorCallback">A status callback.</param>
        /// <param name="cancel">An optional cancellation token.</param>
        void Run(Action<string> statusCallback, Action<double> progressCallback,
                 Action<Exception> errorCallback, CancellationTokenSource cancel = null);
    }
}
