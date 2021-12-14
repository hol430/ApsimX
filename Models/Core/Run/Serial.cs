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
    [ValidParent(typeof(Parallel))]
    [ValidParent(typeof(Serial))]
    public class Serial : Model, IRunnable
    {
        /// <summary>
        /// The task list.
        /// </summary>
        private IReadOnlyList<IRunnable> tasks;

        /// <summary>
        /// Aggregated progress of all tasks.
        /// </summary>
        public double Progress
        {
            get
            {
                    IReadOnlyList<double> progresses = tasks.Select(t => t.Progress)
                                                            .Where(p => !double.IsNaN(p))
                                                            .ToList();
                return progresses.Sum() / progresses.Count;
            }
        }

        /// <summary>
        /// Default constructor - runs all child models serially.
        /// </summary>
        public Serial()
        {
            tasks = FindAllChildren<IRunnable>().ToList();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tasks">A collection of tasks to run.</param>
        public Serial(IEnumerable<IRunnable> tasks)
        {
            this.tasks = tasks.ToList();
        }

        /// <summary>
        /// A convienence static method for running a collection of tasks.
        /// </summary>
        /// <param name="tasks">A collection of tasks to run.</param>
        /// <param name="statusCallback">A callback to handle a status message update.</param>
        /// <param name="errorCallback">A callback to handle an error.</param>
        /// <param name="cancel">A cancellation token.</param>
        public static void Run(IEnumerable<IRunnable> tasks,
                               Action<string> statusCallback,
                               Action<Exception> errorCallback,
                               CancellationTokenSource cancel = null)
        {
            var serial = new Serial(tasks);
            serial.Run(statusCallback, errorCallback, cancel);
        }

        /// <summary>The run method.</summary>
        /// <param name="statusCallback">A status callback.</param>
        /// <param name="errorCallback">A status callback.</param>
        /// <param name="cancelToken">An optional cancellation token.</param>
        public void Run(Action<string> statusCallback, Action<Exception> errorCallback,
            CancellationTokenSource cancelToken = null)
        {
            int numCompleted = 0;
            foreach (IRunnable task in tasks)
            {
                try
                {
                    task.Run(statusCallback, errorCallback, cancelToken);
                }
                catch (Exception ex)
                {
                    errorCallback(ex);
                }
                finally
                {
                    numCompleted++;
                }
            }
        }
    }
}
