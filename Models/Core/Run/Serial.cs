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
        /// <param name="statusCallback">A callback to handle a status message update.</param>
        /// <param name="progressCallback">A callback to handle a progress update.</param>
        /// <param name="errorCallback">A callback to handle an error.</param>
        /// <param name="cancel">A cancellation token.</param>
        public static void Run(IEnumerable<IRunnable> tasks,
                               Action<string> statusCallback,
                               Action<double> progressCallback,
                               Action<Exception> errorCallback,
                               CancellationTokenSource cancel = null)
        {
            var serial = new Serial(tasks);
            serial.Run(statusCallback, progressCallback, errorCallback, cancel);
        }

        /// <summary>The run method.</summary>
        /// <param name="statusCallback">A status callback.</param>
        /// <param name="progressCallback">A status callback.</param>
        /// <param name="errorCallback">A status callback.</param>
        /// <param name="cancelToken">An optional cancellation token.</param>
        public void Run(Action<string> statusCallback, Action<double> progressCallback,
            Action<Exception> errorCallback, CancellationTokenSource cancelToken = null)
        {
            int numComplete = 0;
            IReadOnlyList<IRunnable> taskList = tasks.ToList();

            Action<double> taskProgressCallback = p => progressCallback( (numComplete + p) / taskList.Count);
            foreach (IRunnable task in taskList)
            {
                try
                {
                    task.Run(statusCallback, taskProgressCallback, errorCallback, cancelToken);
                }
                catch (Exception ex)
                {
                    errorCallback(ex);
                }
                finally
                {
                    numComplete++;
                }
            }
        }
    }
}
