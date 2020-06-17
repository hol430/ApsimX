using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSIM.Shared.JobRunning
{
    /// <summary>
    /// An interface for classes which run <see cref="IRunnable"/> jobs.
    /// </summary>
    public interface IJobRunner
    {
        /// <summary>Event is invoked when a job is complete.</summary>
        event EventHandler<JobCompleteArguments> JobCompleted;

        /// <summary>Event is invoked when all jobs are complete.</summary>
        event EventHandler<AllCompleteArguments> AllCompleted;

        /// <summary>The exception (if any) thrown by the runner.</summary>
        Exception ExceptionThrownByRunner { get; }

        /// <summary>The total time taken by the runner to run all jobs.</summary>
        TimeSpan ElapsedTime { get; }

        /// <summary>Gets the aggregate progress of all jobs as a real number in range [0, 1].</summary>
        double Progress { get; }

        /// <summary>Current status of the running jobs.</summary>
        string Status { get; }

        /// <summary>Add a job manager to the collection of job managers to be run.</summary>
        /// <param name="jobManager">The job manager to add.</param>
        void Add(IJobManager jobManager);

        /// <summary>Stop all jobs currently running. Wait until all stopped.</summary>
        void Stop();

        /// <summary>Run all jobs.</summary>
        /// <param name="wait">Wait until all jobs finished before returning?</param>
        void Run(bool wait = false);
    }
}
