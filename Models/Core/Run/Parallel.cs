using Models.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Models.Core.Run
{
    /// <summary>
    /// This is a post-simulation tool which will run all child post-simulation
    /// tools in parallel.
    /// </summary>
    [ValidParent(typeof(IDataStore))]
    public class Parallel : CompositeTask, IRunnable
    {
        /// <inheritdoc />
        public override double Progress => tasks.Sum(t => t.Progress) / tasks.Count;

        // /// <summary>
        // /// Default constructor - runs all child models asynchronously.
        // /// </summary>
        // public Parallel()
        // {
        //     tasks = FindAllChildren<IRunnable>().ToList();
        // }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tasks">A collection of tasks to run.</param>
        public Parallel(IReadOnlyList<IRunnable> tasks) : base(tasks)
        {
        }

        /// <summary>
        /// A convienence static method for running a collection of tasks.
        /// </summary>
        /// <param name="tasks">A collection of tasks to run.</param>
        /// <param name="statusHandler">Callback for status reporting.</param>
        /// <param name="errorHandler">Callback for errors.</param>
        /// <param name="cancel">A cancellation token.</param>
        public static void Run(IEnumerable<IRunnable> tasks, Action<string> statusHandler,
                               Action<Exception> errorHandler, CancellationTokenSource cancel = null)
        {
            var parallel = new Parallel(tasks.ToList());
            parallel.Run(statusHandler, errorHandler, cancel);
        }

        /// <summary>The run method.</summary>
        /// <param name="statusCallback">A status callback.</param>
        /// <param name="errorCallback">A status callback.</param>
        /// <param name="cancelToken">An optional cancellation token.</param>
        public override void Run(Action<string> statusCallback, Action<Exception> errorCallback,
            CancellationTokenSource cancelToken = null)
        {
            if (tasks.Count == 0)
                return;

            var lockInstance = new object();
            statusCallback($"{GetNumTasksCompleted()} of {GetNumTasks()} complete");
            System.Threading.Tasks.Parallel.ForEach(tasks, task =>
            {
                try
                {
                    // fixme: for now don't allow the subtasks to update the status
                    // message. This would require the callback be threadsafe, or
                    // that we wrap the callback in a lock.
                    task.Run(_ => { }, errorCallback, cancelToken);
                }
                catch (Exception ex)
                {
                    lock (lockInstance)
                        errorCallback(ex);
                }
                finally
                {
                    lock (lockInstance)
                    {
                        statusCallback($"{GetNumTasksCompleted()} of {GetNumTasks()} complete");
                    }
                }
            });
        }
    }
}
