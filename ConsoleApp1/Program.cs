using APSIM.Shared.JobRunning;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Core.Run;
using Models.Factorial;
using Models.PostSimulationTools;
using Models.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            var simulations = FileFormat.ReadFromFile<Simulations>(@"C:\Users\hol353\Repos\ApsimX\Tests\Validation\Wheat\WheatNPI.apsimx",
                                                       (err) => { throw err; },
                                                       false);

            var stopWatch = Stopwatch.StartNew();

            var thingsToRun = new List<IApsimRunnable>();
            foreach (var experiment in simulations.FindAllDescendants<Experiment>())
                thingsToRun.Add(new SimulationRunnable(experiment.BaseSimulation, experiment.GetSimulationDescription()));

            Parallel.Run(thingsToRun);

            Console.WriteLine($"Elapsed time {stopWatch.Elapsed.TotalSeconds} seconds");
        }
    }
}
