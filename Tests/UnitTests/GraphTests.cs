using System;
using NUnit.Framework;
using Models;
using Models.Core;
using Models.Graph;
using Models.Report;
using Models.Storage;
using System.IO;
using APSIM.Shared.Utilities;
using Models.Core.Runners;
using Models.PostSimulationTools;

namespace UnitTests
{
    class GraphTests
    {
        /// <summary>
        /// PredictedObserved graphs should be able to filter results based on a
        /// column which exists in only one table (either the predicted or the
        /// observed).
        /// </summary>
        [Test]
        public void PredictedObservedTest()
        {
            Simulation sim = new Simulation()
            {
                Name = "Simulation",
                FileName = Path.ChangeExtension(Path.GetTempFileName(), ".apsimx"),
            };

            Clock clock = new Clock();
            clock.StartDate = new DateTime(2018, 1, 1);
            clock.EndDate = new DateTime(2018, 1, 10);
            sim.Children.Add(clock);

            Summary summary = new Summary();
            sim.Children.Add(summary);

            Report predicted = new Report();
            predicted.Name = "Predicted";
            predicted.VariableNames = new string[] {"[Clock].Today.DayOfWeek as W",
                                                    "[Clock].Today.DayOfYear as X",
                                                    "2^([Clock].Today.DayOfYear) as Y" };
            predicted.EventNames = new string[] { "[Clock].DoReport" };
            sim.Children.Add(predicted);

            Report observed = new Report();
            observed.Name = "Observed";
            observed.VariableNames = new string[2];
            observed.VariableNames[0] = "[Clock].Today.DayOfYear as X";
            observed.VariableNames[1] = "2^([Clock].Today.DayOfYear + 1) as Z";
            observed.EventNames = new string[] { "[Clock].DoReport" };
            sim.Children.Add(observed);

            Graph graph = new Graph();
            graph.Name = "Graph";
            Axis xAxis = new Axis()
            {
                Type = Axis.AxisType.Bottom,
                Inverted = false
            };
            Axis yAxis = new Axis()
            {
                Type = Axis.AxisType.Left,
                Inverted = false
            };
            graph.Axes = new System.Collections.Generic.List<Axis>() { xAxis, yAxis };

            Series predictedData = new Series()
            {
                Name = "PredictedData",
                TableName = "Predicted",
                Checkpoint = "Current",
                XFieldName = "X",
                YFieldName = "Y",
                Filter = "[W] = 'Monday'",
                XAxis = Axis.AxisType.Bottom,
                YAxis = Axis.AxisType.Left,
                Type = SeriesType.Scatter
            };

            Series observedData = new Series()
            {
                Name = "ObservedData",
                TableName = "Observed",
                Checkpoint = "Current",
                XFieldName = "X",
                YFieldName = "Z",
                Filter = "[W] = 'Monday'", // There is no field called [W] in the observed table
                XAxis = Axis.AxisType.Bottom,
                YAxis = Axis.AxisType.Left,
                Type = SeriesType.Scatter
            };
            graph.Children.Add(predictedData);
            graph.Children.Add(observedData);
            sim.Children.Add(graph);

            DataStore storage = new DataStore();

            Simulations sims = Simulations.Create(new Model[] { sim, storage });
            sims.FileName = sim.FileName;
            storage.Open(false);
            IJobManager jobManager = new RunOrganiser(sims, sim, false);
            IJobRunner jobRunner = new JobRunnerAsync();
            jobRunner.Run(jobManager, true);

            // This should throw, because the observed series is trying to filter on
            // a field which doesn't exist in the observed table, and there is no
            // PredictedObserved model.
            Assert.Throws<SQLiteException>(() => graph.GetDefinitionsToGraph(storage));

            // If we add in a PredictedObserved model, we should no longer get an exception.
            PredictedObserved poModel = new PredictedObserved()
            {
                Name = "PredictedObserved",
                FieldNameUsedForMatch = "X",
                PredictedTableName = "Predicted",
                ObservedTableName = "Observed"
            };
            storage.Children.Add(poModel);
            poModel.Parent = storage;

            Assert.DoesNotThrow(() => graph.GetDefinitionsToGraph(storage));
        }
    }
}
