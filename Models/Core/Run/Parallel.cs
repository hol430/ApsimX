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
    public class Parallel : Model, IRunnable
    {
        private IEnumerable<IRunnable> tasks;

        /// <summary>
        /// Default constructor - runs all child models asynchronously.
        /// </summary>
        public Parallel()
        {
            tasks = FindAllChildren<IRunnable>();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tasks">A collection of tasks to run.</param>
        public Parallel(IEnumerable<IRunnable> tasks)
        {
            this.tasks = tasks;
        }

        /// <summary>
        /// A convienence static method for running a collection of tasks.
        /// </summary>
        /// <param name="tasks">A collection of tasks to run.</param>
        /// <param name="statusHandler">Callback for status reporting.</param>
        /// <param name="progressHandler">Callback for progress reporting.</param>
        /// <param name="errorHandler">Callback for errors.</param>
        /// <param name="cancel">A cancellation token.</param>
        public static void Run(IEnumerable<IRunnable> tasks, Action<string> statusHandler,
                               Action<double> progressHandler,
                               Action<Exception> errorHandler,
                               CancellationTokenSource cancel = null)
        {
            var parallel = new Parallel(tasks);
            parallel.Run(statusHandler, progressHandler, errorHandler, cancel);
        }

        /// <summary>The run method.</summary>
        /// <param name="statusCallback">A status callback.</param>
        /// <param name="progressCallback">A status callback.</param>
        /// <param name="errorCallback">A status callback.</param>
        /// <param name="cancelToken">An optional cancellation token.</param>
        public void Run(Action<string> statusCallback, Action<double> progressCallback,
            Action<Exception> errorCallback, CancellationTokenSource cancelToken = null)
        {
            var lockInstance = new object();
            int numComplete = 0;
            IReadOnlyList<IRunnable> taskList = tasks.ToList();
            Action<double> taskProgressCallback = p =>
            {
                lock (lockInstance)
                    progressCallback( (numComplete + p) / taskList.Count );
            };
            System.Threading.Tasks.Parallel.ForEach(taskList, task =>
            {
                try
                { 
                    task.Run(statusCallback, taskProgressCallback, errorCallback, cancelToken);
                }
                catch (Exception ex)
                {
                    lock (lockInstance)
                        errorCallback(ex);
                }
                finally
                {
                    numComplete++;
                }
            });
        }
    }
}
