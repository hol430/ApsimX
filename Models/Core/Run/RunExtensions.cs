using APSIM.Shared.Utilities;
using Models.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Models.Core.Run
{
    /// <summary>
    /// Adds a run method to an IModel
    /// </summary>
    public static class RunExtensions
    {
        /// <summary>
        /// Run all simulations under a model.
        /// </summary>
        /// <remarks>
        /// Exceptions thrown by a simulation or a post simulation tool are reported
        /// back to the caller via the status argument. Other exceptions (i.e. thrown
        /// by this Run method) will be thrown as normal.
        /// </remarks>
        /// <param name="parent">The parent model instance.</param>
        /// <param name="statusHandler">A callback for reporting status messages.</param>
        /// <param name="errorHandler">A callback for reporting error.</param>
        /// <param name="cancel">An optional cancellation token.</param>
        public static void Run(this IModel parent,
                               Action<string> statusHandler,
                               Action<Exception> errorHandler,
                               CancellationTokenSource cancel = null)
        {
            parent.CreateRunnable().Run(statusHandler, errorHandler, cancel);
        }

        /// <summary>
        /// Run all simulations under a model.
        /// </summary>
        /// <remarks>
        /// Exceptions thrown by a simulation or a post simulation tool are reported
        /// back to the caller via the status argument. Other exceptions (i.e. thrown
        /// by this Run method) will be thrown as normal.
        /// </remarks>
        /// <param name="parent">The parent model instance.</param>
        public static IRunnable CreateRunnable(this IModel parent)
        {
            return new RunModel(parent);
        }

        /// <summary>
        /// Run all simulations under a model.
        /// </summary>
        /// <remarks>
        /// Exceptions thrown by a simulation or a post simulation tool are reported
        /// back to the caller via the status argument. Other exceptions (i.e. thrown
        /// by this Run method) will be thrown as normal.
        /// </remarks>
        /// <param name="parent">The parent model instance.</param>
        public static IRunnable CreatePostSimulationToolsTask(this IModel parent)
        {
            return new Serial(RunModel.FindPostSimulationTools<IPostSimulationTool>(parent));
        }

        /// <summary>
        /// Check if a job is completed.
        /// </summary>
        /// <param name="job">The job.</param>
        public static bool IsCompleted(this IRunnable job)
        {
            return MathUtilities.FloatsAreEqual(1, job.Progress);
        }
    }
}