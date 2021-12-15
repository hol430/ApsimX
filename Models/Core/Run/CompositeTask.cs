using Models.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Models.Core.Run
{
    /// <summary>
    /// This is a task which is made up of multiple sub-tasks.
    /// </summary>
    public abstract class CompositeTask : IRunnable
    {
        /// <summary>
        /// Internal task list containing all tasks.
        /// </summary>
        protected IReadOnlyList<IRunnable> tasks;

        /// <summary>
        /// List of all subtasks which are not composite tasks.
        /// </summary>
        private readonly IReadOnlyList<IRunnable> regularTasks;

        /// <summary>
        /// List of all composite subtasks.
        /// </summary>
        private readonly IReadOnlyList<CompositeTask> compositeTasks;

        /// <inheritdoc />
        public abstract double Progress { get; }

        /// <inheritdoc />
        public abstract void Run(Action<string> statusCallback, Action<Exception> errorCallback, CancellationTokenSource cancel = null);

        /// <summary>
        /// Create a new <see cref="CompositeTask"/> instance.
        /// </summary>
        /// <param name="tasks">List of tasks.</param>
        protected CompositeTask(IReadOnlyList<IRunnable> tasks)
        {
            this.tasks = tasks;

            (compositeTasks, regularTasks) = GetTasks();
        }

        /// <summary>
        /// Get the total number of tasks.
        /// </summary>
        /// <returns></returns>
        protected int GetNumTasks()
        {
            return regularTasks.Count + compositeTasks.Sum(t => t.GetNumTasks());
        }

        /// <summary>
        /// Get the number of completed tasks.
        /// </summary>
        protected virtual int GetNumTasksCompleted()
        {
            return regularTasks.Count(t => t.IsCompleted()) + compositeTasks.Sum(t => t.GetNumTasksCompleted());
        }

        /// <summary>
        /// Split the tasks list into regular tasks and composite tasks.
        /// </summary>
        protected (IReadOnlyList<CompositeTask>, IReadOnlyList<IRunnable>) GetTasks()
        {
            List<CompositeTask> compositeTasks = new List<CompositeTask>();
            List<IRunnable> regularTasks = new List<IRunnable>();

            foreach (IRunnable task in tasks)
            {
                if (task is CompositeTask composite)
                    compositeTasks.Add(composite);
                else
                    regularTasks.Add(task);
            }

            return (compositeTasks, regularTasks);
        }
    }
}
