using APSIM.Server;
using APSIM.Server.Cli;
using APSIM.Server.Commands;
using APSIM.Shared.Extensions.Collections;
using APSIM.Shared.JobRunning;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Core.Run;
using Models.Factorial;
using Models.Storage;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UnitTests.Server
{
    /// <summary>
    /// Unit tests for managed comms protocol.
    /// </summary>
    /// <remarks>
    /// This is currently quick n dirty to verify that things basically work.
    /// todo:
    /// - mock out socket layer
    /// - 
    /// </remarks>
    [TestFixture]
    public class FastWheatTests
    {

        [Test]
        public void RunAcrossGrid()
        {
            var simulations = FileFormat.ReadFromFile<Simulations>(@"C:\Users\hol353\Repos\ApsimX\Tests\Validation\Wheat\WheatNPI.apsimx",
                                                                   (err) => { throw err; },
                                                                   false);

            var dbReader = new ReadCommand("Report", new string[] { "Wheat.Grain.Wt" });

            //var commands = GetReplacementsForEachSimulation(simulations);

            // Find the first simulation
            var simulation = simulations.FindDescendant<Simulation>();
            var dataStore = new DataStore()
            {
                UseInMemoryDB = true
            };

            var stopWatch = Stopwatch.StartNew();

            var jobManager = new JobManager();

            foreach (var experiment in simulations.FindAllDescendants<Experiment>())
                jobManager.Add(new SimulationRunner(experiment.BaseSimulation, experiment.GetSimulationDescription()));

            var jobRunner = new JobRunner();
            jobRunner.Add(jobManager);
            jobRunner.Run(wait: true);

            File.WriteAllText(@"C:\Users\hol353\Temp\Timing.txt", stopWatch.Elapsed.TotalSeconds.ToString());
        }

        class FastRun : IJobManager
        {
            private const int chunkSize = 500;
            private Queue<IEnumerable<IReplacement>> commands;
            private Simulation simulationToRun;
            private DataStore dataStore;

            public FastRun(Simulation simulationToRun, DataStore dataStore, IEnumerable<IEnumerable<IReplacement>> commands)
            {
                this.simulationToRun = simulationToRun;
                this.dataStore = dataStore;
                this.commands = new Queue<IEnumerable<IReplacement>>(commands);
            }

            public int NumJobs => Convert.ToInt32(Math.Ceiling((double) commands.Count() / chunkSize));

            public IEnumerable<IRunnable> GetJobs()
            {
                var chunk = commands.DequeueChunk(500).ToList();
                while (chunk.Any())
                {
                    yield return new FastApsim(simulationToRun, dataStore, chunk);
                    chunk = commands.DequeueChunk(500).ToList();
                }
            }

            public void JobHasCompleted(JobCompleteArguments args)
            {
                //throw new NotImplementedException();
            }
        }

        class FastApsim : IRunnable
        {
            private IEnumerable<IEnumerable<IReplacement>> commands;
            private SimulationDescription simulationDescription;
            private DataStore dataStore;

            public FastApsim(Simulation simulationToRun, DataStore dataStore, IEnumerable<IEnumerable<IReplacement>> commands)
            {
                this.commands = commands;
                this.dataStore = dataStore;

                var harness = new Simulations()
                {
                    Children = new List<IModel>()
                    {
                        simulationToRun,
                        dataStore
                    }
                };
                harness.ParentAllDescendants();

                simulationDescription = new SimulationDescription(simulationToRun, clone: true);
            }

            public double Progress => throw new NotImplementedException();

            public string Name => throw new NotImplementedException();

            public void Prepare()
            {
                simulationDescription.Prepare();
            }

            public void Run(CancellationTokenSource cancelToken)
            {
                foreach (var commandReplacements in commands)
                    simulationDescription.Run(cancelToken, commandReplacements);
            }
        }

/*
        private IEnumerable<IEnumerable<IReplacement>> GetReplacementsForEachSimulation(Simulations simulations)
        {
            //var replacementsNode = simulations.FindChild<Replacements>();

            var experiments = simulations.FindAllDescendants<Experiment>();

            List<IEnumerable<IReplacement>> commands = new List<IEnumerable<IReplacement>>();
            foreach (var experiment in experiments)
            {
                var simulationDescriptions = experiment.GenerateSimulationNames();
                foreach (var simulationDescription in simulationDescriptions)
                {
                    simulationDescription.AddReplacements();
                    commands.Add(simulationDescription.Replacements);
                }
            }

            return commands;
        }
*/
        [Test]
        public void RunAcrossGrid2()
        {
            var simulations = FileFormat.ReadFromString<Simulations>(ReflectionUtilities.GetResourceAsString("APSIM.Tests.MultipleWeathers.test.apsimx"),
                                                                    (err) => { throw err; },
                                                                    false);
            var dataStore = simulations.FindChild<Models.Storage.DataStore>();
            dataStore.UseInMemoryDB = true;
            var cancelToken = new CancellationTokenSource();
            var simulation = simulations.FindChild<Simulation>();
            var simulationDescription = new SimulationDescription(simulation, clone: false);

            var baseDirectory = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "APSIM.Tests", "MultipleWeathers"));
            var commands = new List<IReplacement[]>()
            {
                new IReplacement[] { new PropertyReplacement("Test.Weather.FileName", Path.Combine(baseDirectory, "-37.9500_141.0000.met")) },
                new IReplacement[] { new PropertyReplacement("Test.Weather.FileName", Path.Combine(baseDirectory, "-37.9500_141.0500.met")) },
                new IReplacement[] { new PropertyReplacement("Test.Weather.FileName", Path.Combine(baseDirectory, "-37.9500_141.1000.met")) },
                new IReplacement[] { new PropertyReplacement("Test.Weather.FileName", Path.Combine(baseDirectory, "-38.0000_141.0000.met")) },
                new IReplacement[] { new PropertyReplacement("Test.Weather.FileName", Path.Combine(baseDirectory, "-38.0500_140.9500.met")) },
                new IReplacement[] { new PropertyReplacement("Test.Weather.FileName", Path.Combine(baseDirectory, "-38.0500_141.0000.met")) },
                new IReplacement[] { new PropertyReplacement("Test.Weather.FileName", Path.Combine(baseDirectory, "-38.0500_141.0500.met")) },
                new IReplacement[] { new PropertyReplacement("Test.Weather.FileName", Path.Combine(baseDirectory, "-38.0500_141.1000.met")) },
                new IReplacement[] { new PropertyReplacement("Test.Weather.FileName", Path.Combine(baseDirectory, "-38.1000_141.1000.met")) },
            };

            simulationDescription.Prepare();
            foreach (var command in commands)
                simulationDescription.Run(cancelToken, command);

            var data = dataStore.Reader.GetData("Report", fieldNames: new string[] { "Wheat.Grain.Wt" });
            Assert.AreEqual(279, data.Rows.Count);
        }
    }
}
