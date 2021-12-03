using APSIM.Shared.JobRunning;
using Models.Soils.Standardiser;
using Models.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Models.Core.Run
{
    /// <summary>
    /// Encapsulates a collection of simulations (defined by a set of descriptions) and 
    /// provides methods for preparing and running the set of simulations. It prepares
    /// the simulation once and runs it once for each description.
    /// </summary>
    public class SimulationRunner : IRunnable
    {
        /// <summary>The base simulation to run.</summary>
        private IModel rootModel;

        /// <summary>The base simulation to run.</summary>
        private Simulation simulationToRun;

        /// <summary>Multiple replacements for multiple simulation runs.</summary>
        private IEnumerable<SimulationDesc> simulationDescriptions;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="simulation">The base simulation to run.</param>
        /// <param name="simulationDescriptions">A collection of descriptions of simulations to run.</param>
        public SimulationRunner(Simulation simulation, IEnumerable<SimulationDesc> simulationDescriptions)
        {
            simulationToRun = Apsim.Clone(simulation);
            this.simulationDescriptions = simulationDescriptions;

            // Find the root (top level) model.
            rootModel = simulation;
            while (rootModel.Parent != null)
                rootModel = rootModel.Parent;
        }

        //public IEnumerable<SimulationDescription> Descriptions;

        /// <summary>Get run progress.</summary>
        public double Progress => throw new NotImplementedException();

        //public void SetRunMask(Func<bool, string> mask)

        /// <summary>
        /// Prepare the simulation for running.
        /// </summary>
        public void Prepare()
        {
            try
            {
                // After a binary clone, we need to force all managers to
                // recompile their scripts. This is to work around an issue
                // where scripts will change during deserialization. See issue
                // #4463 and the TestMultipleChildren test inside ReportTests.
                foreach (Manager script in simulationToRun.FindAllDescendants<Manager>())
                    script.OnCreated();

                simulationToRun.Parent = null;
                simulationToRun.ParentAllDescendants();

                // Perform replacements from a top level Replacements node.
                foreach (var replacement in GetGlobalReplacements())
                    replacement.Replace(simulationToRun);

                var services = GetServices();
                simulationToRun.Services = services;

                simulationToRun.ClearCaches();

                // Remove disabled models.
                RemoveDisabledModels(simulationToRun);

                // Standardise the soil.
                var soils = simulationToRun.FindAllDescendants<Soils.Soil>();
                foreach (Soils.Soil soil in soils)
                    SoilStandardiser.Standardise(soil);

                // If this simulation was not created from deserialisation then we need
                // to parent all child models correctly and call OnCreated for each model.
                bool hasBeenDeserialised = simulationToRun.Children.Count > 0 && simulationToRun.Children[0].Parent == this;
                if (!hasBeenDeserialised)
                {
                    // Parent all models.
                    simulationToRun.ParentAllDescendants();

                    // Call OnCreated in all models.
                    foreach (IModel model in simulationToRun.FindAllDescendants().ToList())
                        model.OnCreated();
                }

                // Call OnPreLink in all models.
                // Note the ToList(). This is important because some models can
                // add/remove models from the simulations tree in their OnPreLink()
                // method, and FindAllDescendants() is lazy.
                simulationToRun.FindAllDescendants().ToList().ForEach(model => model.OnPreLink());

                var links = new Links(services);
                var events = new Events(simulationToRun);

                // Connect all events.
                events.ConnectEvents();

                // Resolve all links
                links.Resolve(simulationToRun, true);

                events.Publish("SubscribeToEvents", new object[] { this, EventArgs.Empty });
            }
            catch (Exception err)
            {
                throw new Exception($"Error preparing simulation {simulationToRun.Name} in file {simulationToRun.FileName}", err);
            }
        }

        /// <summary>
        /// Run the simulation once for each simulation description.
        /// </summary>
        /// <param name="cancelToken">The cancelation token.</param>
        public void Run(CancellationTokenSource cancelToken)
        {
            // Now run simulation for each simulation description.
            foreach (SimulationDesc description in simulationDescriptions)
                cancelToken = Run(description, cancelToken);
        }

        /// <summary>
        /// Run the simulation once for a description.
        /// </summary>
        /// <param name="cancelToken">The cancelation token.</param>
        /// <param name="description">The description of the simulation to run.</param>
        /// <returns></returns>
        private CancellationTokenSource Run(SimulationDesc description, CancellationTokenSource cancelToken)
        {
            // Give descriptors to the simulation.
            if (description.Descriptors.Any())
                simulationToRun.Descriptors = description.Descriptors;

            // Give the simulation the correct name.
            simulationToRun.Name = description.Name;

            // Apply all replacements.
            foreach (var replacement in description.Replacements)
                replacement.Replace(simulationToRun);

            // If the cancelToken is null then give it a default one. This can happen 
            // when called from the unit tests.
            if (cancelToken == null)
                cancelToken = new CancellationTokenSource();

            // Run the simulation.
            Exception simulationError = null;
            try
            {
                simulationToRun.Start(cancelToken);
            }
            catch (Exception err)
            {
                // Exception occurred. 
                simulationError = new SimulationException("", err, simulationToRun.Name, simulationToRun.FileName);

                // Try and write to summary.
                var summary = simulationToRun.FindChild<ISummary>();
                summary?.WriteMessage(simulationToRun, simulationError.ToString(), Models.Core.MessageType.Error);

                // Rethrow exception
                throw simulationError;
            }
            finally
            {
                try
                {
                    // Signal that the simulation is complete.
                    simulationToRun.End();
                }
                catch (Exception error)
                {
                    // Exception was thrown during simulation cleanup.
                    var cleanupError = new SimulationException($"Error while performing simulation cleanup", error, simulationToRun.Name, simulationToRun.FileName);

                    // Undo the replacements.
                    foreach (var replacement in description.Replacements)
                        replacement.Undo();

                    // Throw either the exception that was thrown during the simulation or the one that was thrown
                    // during simulation cleanup.
                    if (simulationError == null)
                        throw cleanupError;
                    else
                        throw new AggregateException(simulationError, cleanupError);
                }
            }

            return cancelToken;
        }

        /// <summary>
        /// Remove all disabled child models from the specified model.
        /// </summary>
        /// <param name="model"></param>
        private void RemoveDisabledModels(IModel model)
        {
            model.Children.RemoveAll(child => !child.Enabled);
            model.Children.ForEach(child => RemoveDisabledModels(child));
        }

        /// <summary>Get simulation services (e.g. link, events).</summary>
        /// <returns>Always returns a collection - never null.</returns>
        private IEnumerable<object> GetServices()
        {
            List<object> services = new List<object>();
            if (rootModel is Simulations sims)
            {
                // If the top-level model is a simulations object, it will have access
                // to services such as the checkpoints. This should be passed into the
                // simulation to be used in link resolution. If we don't provide these
                // services to the simulation, it will not be able to resolve links to
                // checkpoints.
                services = sims.GetServices();
            }
            else
            {
                // When is this possible?
                IModel storage = rootModel.FindInScope<DataStore>();
                services.Add(storage);
            }

            return services;
        }

        /// <summary>If a 'Replacements' node exists then return it's collection of IReplacements.</summary>
        /// <returns>A collection of IReplacements if 'Replacements' exists, otherwise and empty collection - never null.</returns>
        private IEnumerable<IReplacement> GetGlobalReplacements()
        {
            List<IReplacement> replacementsToApply = new List<IReplacement>();
            if (rootModel != null)
            {
                IModel replacements = rootModel.FindChild<Replacements>();
                if (replacements != null && replacements.Enabled)
                {
                    foreach (IModel replacement in replacements.Children)
                    {
                        var modelReplacement = new ModelReplacement(null, replacement);
                        replacementsToApply.Insert(0, modelReplacement);
                    }
                }
            }

            return replacementsToApply;
        }


    }
}
