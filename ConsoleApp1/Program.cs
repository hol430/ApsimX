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

            simulations.Run(Console.WriteLine, _ => { }, Console.Error.WriteLine);

            Console.WriteLine($"Elapsed time {stopWatch.Elapsed.TotalSeconds} seconds");
        }
    }
}