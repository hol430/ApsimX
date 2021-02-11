using APSIM.Shared.JobRunning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Models.Core.Run
{
    /// <summary>
    /// A simpler job runner implementation which starts running
    /// all simulations in their own threads immediately, rather
    /// than running them N at a time.
    /// </summary>
    public class SimpleJobRunner : IJobRunner
    {
        private object jobsLock = new object();

        private int numJobs;

        private int numJobsComplete;

        /// <summary>
        /// The time at which job execution commenced.
        /// </summary>
        private DateTime startTime;

        /// <summary>
        /// Queue of all job managers with jobs to be run.
        /// </summary>
        private Queue<IJobManager> jobManagers = new Queue<IJobManager>();

        /// <summary>
        /// Cancellation token used to cancel jobs.
        /// </summary>
        private CancellationTokenSource cancel = new CancellationTokenSource();

        /// <summary>
        /// All running jobs.
        /// </summary>
        private Dictionary<IRunnable, Task> jobs = new Dictionary<IRunnable, Task>();

        /// <summary>
        /// Exceptions thrown during execution.
        /// </summary>
        public Exception ExceptionThrownByRunner { get; private set; }

        /// <summary>
        /// Time taken to run the job.
        /// </summary>
        public TimeSpan ElapsedTime { get; private set; }

        /// <summary>
        /// Job progress in range [0, 1].
        /// </summary>
        public double Progress
        {
            get
            {
                if (jobs.Count == 0)
                    return 1;

                return (numJobsComplete + jobs.Keys.Sum(j => j.Progress)) / numJobs;
            }
        }

        /// <summary>
        /// Status of the jobs.
        /// </summary>
        public string Status => $"{numJobsComplete} of {numJobs} completed";

        /// <summary>
        /// Called after every job is completed.
        /// </summary>
        public event EventHandler<JobCompleteArguments> JobCompleted;

        /// <summary>
        /// Called after all jobs are completed.
        /// </summary>
        public event EventHandler<AllCompleteArguments> AllCompleted;

        /// <summary>
        /// Adds a job manager to be run.
        /// </summary>
        /// <param name="jobManager">The job manager.</param>
        public void Add(IJobManager jobManager)
        {
            jobManagers.Enqueue(jobManager);
        }

        /// <summary>
        /// Run all jobs.
        /// </summary>
        /// <param name="wait">Block current thread until jobs are finished?</param>
        public void Run(bool wait = false)
        {
            startTime = DateTime.Now;
            while (jobManagers.Count > 0)
            {
                IJobManager jobManager = jobManagers.Dequeue();
                foreach (IRunnable job in jobManager.GetJobs())
                {
                    numJobs++;
                    lock (jobsLock)
                        jobs.Add(job, Task.Run(() => job.Run(cancel)).ContinueWith(t => TaskCompleted(job, jobManager)));
                }
            }

            if (wait)
                foreach (Task task in jobs.Values)
                    task.Wait();
        }

        /// <summary>
        /// Abort execution of all jobs.
        /// </summary>
        public void Stop()
        {
            cancel.Cancel();
            // todo - wait until all jobs have stopped.
        }

        /// <summary>
        /// Called when a task is completed.
        /// </summary>
        /// <param name="job">Job which has finished running.</param>
        /// <param name="jobManager">Job manager.</param>
        private void TaskCompleted(IRunnable job, IJobManager jobManager)
        {
            Task task;
            lock (jobsLock)
            {
                task = jobs[job];
                numJobsComplete++;
            }

            var args = new JobCompleteArguments()
            {
                ElapsedTime = DateTime.Now - startTime,
                Job = job,
                ExceptionThrowByJob = task.Exception,
            };
            JobCompleted?.Invoke(this, args);
            jobManager.JobHasCompleted(args);

            bool noJobsRemaining;
            lock (jobsLock)
                noJobsRemaining = jobs.Count == numJobsComplete;
            if (noJobsRemaining)
                AllTasksCompleted();
        }

        private void AllTasksCompleted()
        {
            ElapsedTime = DateTime.Now - startTime;
            AllCompleted?.Invoke(this, new AllCompleteArguments()
            {
                ElapsedTime = ElapsedTime,
            });
        }
    }
}
