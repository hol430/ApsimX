// -----------------------------------------------------------------------
// <copyright file="SummaryPresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
using EventArguments;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Data;
using Models;
using Models.Core;
using Models.Factorial;
using UserInterface.Views;

namespace UserInterface.Presenters
{


    /// <summary>Presenter class for working with HtmlView</summary>
    public class SummaryPresenter : IPresenter
    {
        /// <summary>The summary model to work with.</summary>
        private Summary summaryModel;

        /// <summary>The view model to work with.</summary>
        private ISummaryView view;

        /// <summary>The explorer presenter which manages this presenter.</summary>
        private ExplorerPresenter presenter;

        /// <summary>Our data store</summary>
        [Link]
        private IStorageReader dataStore = null;

        /// <summary>Attach the model to the view.</summary>
        /// <param name="model">The model to work with</param>
        /// <param name="view">The view to attach to</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.summaryModel = model as Summary;
            this.presenter = explorerPresenter;
            this.view = view as ISummaryView;
            // populate the simulation names in the view.
            Simulation parentSimulation = Apsim.Parent(summaryModel, typeof(Simulation)) as Simulation;
            Experiment parentExperiment = Apsim.Parent(summaryModel, typeof(Experiment)) as Experiment;
            if (parentExperiment != null)
                this.view.SimulationNames = parentExperiment.GetSimulationNames().ToList();
            else if (parentSimulation != null)
            {
                this.view.SimulationNames = new string[] { parentSimulation.Name };
                this.view.SimulationName = parentSimulation.Name;
            }
            else
            {
                List<string> simulationNames = new List<string>();
                foreach (Experiment childExperiment in Apsim.ChildrenRecursively(summaryModel.Parent, typeof(Experiment)))
                    foreach (string simulationName in childExperiment.GetSimulationNames())
                        simulationNames.Add(simulationName);

                foreach (Simulation childSimulation in Apsim.ChildrenRecursively(summaryModel.Parent, typeof(Simulation)).Where(s => !(s.Parent is Experiment)))
                    simulationNames.Add(childSimulation.Name);
                this.view.SimulationNames = simulationNames;
            }

            if (this.view.SimulationNames.Count() > 0)
                this.view.SimulationName = this.view.SimulationNames.First();

            // Populate the view.
            this.SetHtmlInView();

            // Subscribe to the simulation name changed event.
            this.view.SimulationNameChanged += this.OnSimulationNameChanged;

            // Subscribe to the view's copy event.
            this.view.Copy += OnCopy;

            this.view.ModelFilterChanged += OnModelFilterChanged;
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            this.view.SimulationNameChanged -= this.OnSimulationNameChanged;
            this.view.Copy -= OnCopy;
        }

        /// <summary>Populate the summary view.</summary>
        private void SetHtmlInView()
        {
            StringWriter writer = new StringWriter();
            Summary.WriteReport(this.dataStore, this.view.SimulationName, writer, Utility.Configuration.Settings.SummaryPngFileName, outtype: Summary.OutputType.html);
            this.view.SetSummaryContent(writer.ToString());
            writer.Close();
            //DataTable modelNames = dataStore.GetData("_Messages", filter: "ComponentName='Clock'");
            view.ModelNames = dataStore.GetData("_Messages", fieldNames: new string[] { "ComponentName" }).AsEnumerable().Select(x => x[0].ToString()).Distinct().ToList();
        }

        /// <summary>Handles the SimulationNameChanged event of the view control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnSimulationNameChanged(object sender, EventArgs e)
        {
            this.SetHtmlInView();
        }

        /// <summary>
        /// Event handler for the view's copy event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnCopy(object sender, CopyEventArgs e)
        {
            this.presenter.SetClipboardText(e.Text, "CLIPBOARD");
        }

        /// <summary>
        /// Event handelr for the view's model filter changed event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arugments.</param>
        private void OnModelFilterChanged(object sender, EventArgs e)
        {

        }
    }
}