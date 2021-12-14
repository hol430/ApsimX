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
            return new Serial(new IRunnable[]
            {
                new Parallel(FindSimulations(parent)),
                new Serial(FindPostSimulationTools<IPostSimulationTool>(parent)),
                new Serial(FindPostSimulationTools<ITest>(parent))
            });
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
            return new Serial(new IRunnable[]
            {
                new Serial(FindPostSimulationTools<IPostSimulationTool>(parent)),
            });
        }

        /// <summary>
        /// Find and return all simulations to run.
        /// </summary>
        /// <param name="parent">The parent model to look under for simulations.</param>
        /// <returns>A collection of runnable simulations.</returns>
        private static IEnumerable<IRunnable> FindSimulations(IModel parent)
        {
            // Find all descendants of the model
            return parent.FindAllDescendants<ISimulationsRunnable>()
            // Where the descendant is not an IModel OR the descendant is an IModel
            // and none of its ancestors are an ISimulationsRunnable.
                         .Where(r => !(r is IModel) || (r as IModel).FindAncestor<ISimulationsRunnable>() == null)
                         .Select(r => new Factorial(r.BaseSimulation, r.GetSimulationDescription()));
        }

        /// <summary>
        /// Find and return all tools to run post simulation.
        /// </summary>
        /// <typeparam name="T">The type of tool to find.</typeparam>
        /// <param name="parent">The parent model to look under for simulations.</param>
        /// <returns></returns>
        private static IEnumerable<IRunnable> FindPostSimulationTools<T>(IModel parent) where T : IModel
        {
            var storage = parent.FindInScope<DataStore>();
            var simulations = parent.FindInScope<Simulations>();
            var tools = parent.FindAllInScope<T>()
                              .Where(t => t.FindAllAncestors()
                              .All(a => !(a is Parallel || a is Serial)));

            // Return all post simulation tools.
            foreach (IPostSimulationTool tool in tools)
                yield return new PostSimulationToolRunner(tool, simulations, storage);
        }

        /// <summary>
        /// A class to encapsulate the running of a post simulation tool.
        /// </summary>
        private class PostSimulationToolRunner : IRunnable
        {
            private readonly IPostSimulationTool tool;
            private readonly Simulations simulations;
            private readonly DataStore storage;

            public double Progress { get; private set; }

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="tool">The tool to run.</param>
            /// <param name="simulations">The top level simulations instance.</param>
            /// <param name="storage">The storage instance.</param>
            public PostSimulationToolRunner(IPostSimulationTool tool, Simulations simulations, DataStore storage)
            {
                this.tool = tool;
                this.simulations = simulations;
                this.storage = storage;
            }

            /// <summary>The run method.</summary>
            /// <param name="status">A status callback.</param>
            /// <param name="errorCallback">A status callback.</param>
            /// <param name="cancelToken">An optional cancellation token.</param>
            public void Run(Action<string> status, Action<Exception> errorCallback,
                CancellationTokenSource cancelToken = null)
            {
                storage?.Writer.WaitForIdle();
                storage?.Reader.Refresh();

                // If we run into problems, we will want to include the name of the test in the 
                // exception's message. However, tests may be manager scripts, which always have
                // a name of 'Script'. Therefore, if the test's parent is a Manager, we use the
                // manager's name instead.
                string toolName = tool.Parent is Manager ? tool.Parent.Name : tool.Name;

                status($"Resolving links for {toolName}");
                simulations?.Links.Resolve(tool as IModel);
                status($"Running {toolName}");
                tool.Run(status, errorCallback, cancelToken);
                status($"{toolName} completed");
                Progress = 1;
            }
        }
    }
}