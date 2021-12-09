using System.Threading;

namespace Models.Core.Run
{
    /// <summary>
    /// Simple interface that denoates that something can be run.
    /// </summary>
    public interface IApsimRunnable
    {
        /// <summary>The run method.</summary>
        /// <param name="cancel">An optional cancellation token.</param>
        void Run(CancellationTokenSource cancel = null);
    }
}
