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
    public class Parallel : Model, IApsimRunnable
    {
        private IEnumerable<IApsimRunnable> tasks;

        /// <summary>
        /// Default constructor - runs all child models asynchronously.
        /// </summary>
        public Parallel()
        {
            tasks = FindAllChildren<IApsimRunnable>();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tasks">A collection of tasks to run.</param>
        public Parallel(IEnumerable<IApsimRunnable> tasks)
        {
            this.tasks = tasks;
        }

        /// <summary>
        /// A convienence static method for running a collection of tasks.
        /// </summary>
        /// <param name="tasks">A collection of tasks to run.</param>
        /// <param name="cancel">A cancellation token.</param>
        public static void Run(IEnumerable<IApsimRunnable> tasks, CancellationTokenSource cancel = null)
        {
            var parallel = new Parallel(tasks);
            parallel.Run(cancel);
        }

        /// <summary>
        /// Run the post-simulation tool.
        /// </summary>
        /// <param name="cancelToken">The cancelation token.</param>
        /// <param name="status">A callback for reporting status messages.</param>
        public void Run(CancellationTokenSource cancelToken = null,
                        Action<string, MessageType> status = null)
        {
            var lockInstance = new object();
            System.Threading.Tasks.Parallel.ForEach(tasks, task =>
            {
                try
                { 
                    task.Run(cancelToken);
                }
                catch (Exception ex)
                {
                    lock (lockInstance)
                        status(ex.ToString(), MessageType.Error);
                }
            });
        }
    }
}
