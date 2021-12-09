using Models.Core.Run;
using Models.Storage;
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
    public class Serial : Model, IApsimRunnable
    {
        private IEnumerable<IApsimRunnable> tasks;

        /// <summary>
        /// Default constructor - runs all child models serially.
        /// </summary>
        public Serial()
        {
            tasks = FindAllChildren<IApsimRunnable>();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tasks">A collection of tasks to run.</param>
        public Serial(IEnumerable<IApsimRunnable> tasks)
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
            var serial = new Serial(tasks);
            serial.Run(cancel);
        }

        /// <summary>
        /// Run the post-simulation tool.
        /// </summary>
        public void Run(CancellationTokenSource cancel = null)
        {
            foreach (var task in tasks)
                task.Run(cancel);
        }
    }
}
