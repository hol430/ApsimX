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
using System.Threading;

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

            var stopWatch = Stopwatch.StartNew();

            var thingsToRun = new List<IApsimRunnable>();
            foreach (var experiment in simulations.FindAllDescendants<Experiment>())
                thingsToRun.Add(new SimulationRunnable(experiment.BaseSimulation, experiment.GetSimulationDescription()));

            Parallel.Run(thingsToRun);

            File.WriteAllText(@"C:\Users\hol353\Temp\Timing.txt", stopWatch.Elapsed.TotalSeconds.ToString());
        }
    }
}
