using System;
using System.Threading;
using Models.Storage;

namespace Models.Core.Run
{
    /// <summary>
    /// A class to encapsulate the running of a post simulation tool.
    /// </summary>
    internal class PostSimulationToolRunner : IRunnable
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
            status("Waiting for datastore...");
            storage?.Writer.WaitForIdle();
            storage?.Reader.Refresh();

            // If we run into problems, we will want to include the name of the test in the 
            // exception's message. However, tests may be manager scripts, which always have
            // a name of 'Script'. Therefore, if the test's parent is a Manager, we use the
            // manager's name instead.
            string toolName = tool.Parent is Manager ? tool.Parent.Name : tool.Name;

            status($"Running post-simulation tool {toolName}");
            simulations?.Links.Resolve(tool as IModel);
            tool.Run();
            Progress = 1;
        }
    }
}
