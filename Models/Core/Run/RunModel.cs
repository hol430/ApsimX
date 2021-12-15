using APSIM.Shared.Extensions.Collections;
using Models.PostSimulationTools;
using Models.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Models.Core.Run
{
    /// <summary>
    /// This class encapsulates a model run. It extends the <see cref="Serial"/>
    /// to support better progress reporting. This is achieved by maintaining separate
    /// IRunnable collections for simulations, post-simulation tools, and tests.
    /// </summary>
    public class RunModel : Serial
    {
        private IReadOnlyList<IRunnable> simulations;
        private IReadOnlyList<IRunnable> postSimulationTools;
        private IReadOnlyList<IRunnable> tests;
        private IModel model;

        /// <inheritdoc />
        /// <remarks>
        /// Overriding in order to ignore post-simulation tools and tests in
        /// progress calculations.
        /// </remarks>
        public override double Progress
        {
            get
            {
                return simulations.Sum(s => s.Progress) / simulations.Count;
            }
        }

        /// <summary>
        /// Create a new <see cref="RunModel"/> instance.
        /// </summary>
        /// <param name="model"></param>
        public RunModel(IModel model) : this(FindSimulations(model), FindPostSimulationTools<IPostSimulationTool>(model), FindPostSimulationTools<ITest>(model))
        {
            this.model = model;
        }

        /// <inheritdoc />
        public override void Run(Action<string> statusCallback, Action<Exception> errorCallback, CancellationTokenSource cancelToken = null)
        {
            base.Run(statusCallback, errorCallback, cancelToken);
            IDataStore storage = model.FindInScope<IDataStore>();
            if (storage != null && storage.Writer != null)
                storage.Writer.Stop();
        }

        /// <summary>
        /// Internal constructor.
        /// </summary>
        /// <param name="simulations">Simulations to be run.</param>
        /// <param name="postSimulationTools">Post-simulation tools to be run.</param>
        /// <param name="tests">Tests to be run.</param>
        private RunModel(IReadOnlyList<IRunnable> simulations, IReadOnlyList<IRunnable> postSimulationTools, IReadOnlyList<IRunnable> tests) : base(Combine(simulations, postSimulationTools, tests))
        {
            this.simulations = simulations;
            this.postSimulationTools = postSimulationTools;
            this.tests = tests;
        }

        /// <summary>
        /// Combine the simulations, post-simulation tools, and tests into a
        /// single collection of IRunnable instances which can be passed into
        /// the base class' constructor.
        /// </summary>
        /// <param name="simulations">Simulations to be run.</param>
        /// <param name="postSimulationTools">Post-simulation tools to be run.</param>
        /// <param name="tests">Tests to be run.</param>
        private static IReadOnlyList<IRunnable> Combine(IReadOnlyList<IRunnable> simulations, IReadOnlyList<IRunnable> postSimulationTools, IReadOnlyList<IRunnable> tests)
        {
            // Remember that the base class is Serial. So each element
            // of the return value will be run result.
            return new IRunnable[]
            {
                // Simulations will all be run in parallel...
                new Parallel(simulations),
                // ...then the post-simulation tools will be run...
                new Serial(postSimulationTools),
                // ...finally, the tests will be run.
                new Serial(tests)
            };
        }

        /// <summary>
        /// Find and return all simulations to run.
        /// </summary>
        /// <param name="parent">The parent model to look under for simulations.</param>
        /// <returns>A collection of runnable simulations.</returns>
        private static IReadOnlyList<IRunnable> FindSimulations(IModel parent)
        {
            // Find all descendants of the model
            return FindAllRunnables(parent)
            // Where the descendant is not an IModel OR the descendant is an IModel
            // and none of its ancestors are an ISimulationsRunnable.
                         .Where(r => !(r is IModel) || (r as IModel).FindAncestor<ISimulationsRunnable>() == null)
                         .Select(r => new Factorial(r.BaseSimulation, r.GetSimulationDescription()))
                         .ToList();
        }

        /// <summary>
        /// Find all runnable instances below the given model. The model will be
        /// included in the return result if it is itself a runnable instance.
        /// </summary>
        /// <param name="model"></param>
        private static IEnumerable<ISimulationsRunnable> FindAllRunnables(IModel model)
        {
            IEnumerable<ISimulationsRunnable> runnables = model.FindAllDescendants<ISimulationsRunnable>();
            if (model is ISimulationsRunnable runnable)
                runnables = runnables.Prepend(runnable);
            return runnables;
        }

        /// <summary>
        /// Find and return all tools to run post simulation.
        /// </summary>
        /// <typeparam name="T">The type of tool to find.</typeparam>
        /// <param name="parent">The parent model to look under for simulations.</param>
        /// <returns></returns>
        public static IReadOnlyList<IRunnable> FindPostSimulationTools<T>(IModel parent) where T : IModel
        {
            var storage = parent.FindInScope<DataStore>();
            var simulations = parent.FindInScope<Simulations>();
            return parent.FindAllInScope<T>()
                              .Where(t => t.FindAllAncestors()
                              .All(a => !(a is ParallelPostSimulationTool || a is SerialPostSimulationTool)))
                              .Cast<IPostSimulationTool>() // fixme - this will fail with any T other than IPostSimulationTool.
                              .Select(t => new PostSimulationToolRunner(t, simulations, storage))
                              .ToList();
        }
    }
}
