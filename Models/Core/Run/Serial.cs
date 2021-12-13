using Models.Storage;
using System;
using System.Collections.Generic;
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
        private IEnumerable<IRunnable> tasks;

        /// <summary>
        /// Default constructor - runs all child models serially.
        /// </summary>
        public Serial()
        {
            tasks = FindAllChildren<IRunnable>();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tasks">A collection of tasks to run.</param>
        public Serial(IEnumerable<IRunnable> tasks)
        {
            this.tasks = tasks;
        }

        /// <summary>
        /// A convienence static method for running a collection of tasks.
        /// </summary>
        /// <param name="tasks">A collection of tasks to run.</param>
        /// <param name="cancel">A cancellation token.</param>
        public static void Run(IEnumerable<IRunnable> tasks, 
                               CancellationTokenSource cancel = null)
        {
            var serial = new Serial(tasks);
            serial.Run(cancel);
        }

        /// <summary>The run method.</summary>
        /// <param name="status">A status callback.</param>
        /// <param name="progressCallback">A status callback.</param>
        /// <param name="errorCallback">A status callback.</param>
        /// <param name="cancelToken">An optional cancellation token.</param>
        public void Run(Action<string> status, Action<double> progressCallback,
            Action<Exception> errorCallback, CancellationTokenSource cancelToken = null)
        {
            foreach (IRunnable task in tasks)
            {
                try
                {
                    task.Run(status, progressCallback, errorCallback, cancelToken);
                }
                catch (Exception ex)
                {
                    errorCallback(ex);
                }
            }
        }
    }
}
