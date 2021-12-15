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
    public class Serial : CompositeTask, IRunnable
    {
        /// <summary>
        /// Aggregated progress of all tasks.
        /// </summary>
        public override double Progress
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
        /// Constructor
        /// </summary>
        /// <param name="tasks">A collection of tasks to run.</param>
        public Serial(IReadOnlyList<IRunnable> tasks) : base(tasks)
        {
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
            var serial = new Serial(tasks.ToList());
            serial.Run(statusCallback, errorCallback, cancel);
        }

        /// <summary>The run method.</summary>
        /// <param name="statusCallback">A status callback.</param>
        /// <param name="errorCallback">A status callback.</param>
        /// <param name="cancelToken">An optional cancellation token.</param>
        public override void Run(Action<string> statusCallback, Action<Exception> errorCallback,
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
