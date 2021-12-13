using APSIM.Shared.Documentation;
using APSIM.Shared.JobRunning;
using Models.Core.Run;
using Models.Factorial;
using Models.Soils.Standardiser;
using Models.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Models.Core
{
    /// <summary>
    /// A simulation model
    /// </summary>
    [ValidParent(ParentType = typeof(Simulations))]
    [ValidParent(ParentType = typeof(Experiment))]
    [ValidParent(ParentType = typeof(Morris))]
    [ValidParent(ParentType = typeof(Sobol))]
    [Serializable]
    [ScopedModel]
    public class Simulation : Model, APSIM.Shared.JobRunning.IRunnable, IReportsStatus
    {
        [NonSerialized]
        private ScopingRules scope = null;

        /// <summary>Invoked when simulation is about to commence.</summary>
        public event EventHandler Commencing;

        /// <summary>Invoked to signal start of simulation.</summary>
        public event EventHandler<CommenceArgs> DoCommence;

        /// <summary>Invoked when the simulation is completed.</summary>
        public event EventHandler Completed;

        /// <summary>Return total area.</summary>
        public double Area
        {
            get
            {
                return this.FindAllChildren<Zone>().Sum(z => (z as Zone).Area);
            }
        }


        /// <summary>
        /// An enum that is used to indicate message severity when writing messages to the .db
        /// </summary>
        public enum ErrorLevel
        {
            /// <summary>Information</summary>
            Information,

            /// <summary>Warning</summary>
            Warning,

            /// <summary>Error</summary>
            Error
        };

        /// <summary>
        /// An enum that is used to indicate message severity when writing messages to the status window.
        /// </summary>
        public enum MessageType
        {
            /// <summary>Information</summary>
            Information,

            /// <summary>Warning</summary>
            Warning
        };

        /// <summary>Returns the object responsible for scoping rules.</summary>
        public ScopingRules Scope
        {
            get
            {
                if (scope == null)
                {
                    scope = new ScopingRules();
                }
                return scope;
            }
        }

        /// <summary>A locater object for finding models and variables.</summary>
        [NonSerialized]
        private Locater locater;

        /// <summary>Cache to speed up scope lookups.</summary>
        /// <value>The locater.</value>
        public Locater Locater
        {
            get
            {
                if (locater == null)
                {
                    locater = new Locater();
                }

                return locater;
            }
        }

        /// <summary>
        /// Returns the job's progress as a real number in range [0, 1].
        /// </summary>
        public double Progress
        {
            get
            {
                Clock c = this.FindChild<Clock>();
                if (c == null)
                    return 0;
                else
                    return c.FractionComplete;
            }
        }

        /// <summary>Is the simulation running?</summary>
        public bool IsRunning { get; private set; } = false;

        /// <summary>A list of keyword/value meta data descriptors for this simulation.</summary>
        public IEnumerable<SimulationDescription.Descriptor> Descriptors { get; set; }

        /// <summary>Gets the value of a variable or model.</summary>
        /// <param name="namePath">The name of the object to return</param>
        /// <returns>The found object or null if not found</returns>
        public object Get(string namePath)
        {
            return Locater.Get(namePath, this);
        }

        /// <summary>Get the underlying variable object for the given path.</summary>
        /// <param name="namePath">The name of the variable to return</param>
        /// <returns>The found object or null if not found</returns>
        public IVariable GetVariableObject(string namePath)
        {
            return Locater.GetInternal(namePath, this);
        }

        /// <summary>Sets the value of a variable. Will throw if variable doesn't exist.</summary>
        /// <param name="namePath">The name of the object to set</param>
        /// <param name="value">The value to set the property to</param>
        public void Set(string namePath, object value)
        {
            Locater.Set(namePath, this, value);
        }

        /// <summary>Return the filename that this simulation sits in.</summary>
        /// <value>The name of the file.</value>
        [JsonIgnore]
        public string FileName { get; set; }

        /// <summary>Collection of models that will be used in resolving links. Can be null.</summary>
        [JsonIgnore]
        public IEnumerable<object> Services { get; set; }

        /// <summary>Status message.</summary>
        public string Status => FindAllDescendants<IReportsStatus>().FirstOrDefault(s => !string.IsNullOrEmpty(s.Status))?.Status;

        /// <summary>
        /// Simulation has completed. Clear scope and locator
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            ClearCaches();
        }

        /// <summary>
        /// Clears the existing Scoping Rules
        /// </summary>
        public void ClearCaches()
        {
            Scope.Clear();
            Locater.Clear();
        }

        /// <summary>Gets a list of simulation descriptors.</summary>
        public List<SimulationDescription.Descriptor> GenerateSimulationDescriptors()
        {
            var descriptors = new List<SimulationDescription.Descriptor>();

            // Add a folderName descriptor.
            var folderNode = FindAncestor<Folder>();
            if (folderNode != null)
                descriptors.Add(new SimulationDescription.Descriptor("FolderName", folderNode.Name));

            descriptors.Add(new SimulationDescription.Descriptor("SimulationName", Name));

            foreach (var zone in this.FindAllDescendants<Zone>())
                descriptors.Add(new SimulationDescription.Descriptor("Zone", zone.Name));

            return descriptors;
        }

        /// <summary>
        /// Prepare the simulation for running.
        /// </summary>
        public void Prepare()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Runs the simulation on the current thread and waits for the simulation
        /// to complete before returning to caller. Simulation is NOT cloned before
        /// running. Use instance of Runner to get more options for running a 
        /// simulation or groups of simulations. 
        /// </summary>
        /// <param name="cancelToken">Is cancellation pending?</param>
        public void Run(CancellationTokenSource cancelToken = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Start the simulation running.
        /// </summary>
        /// <param name="cancelToken">The cancellation token.</param>
        internal void Start(CancellationTokenSource cancelToken)
        {
            IsRunning = true;

            // Invoke our commencing event to let all models know we're about to start.
            Commencing?.Invoke(this, new EventArgs());

            // Begin running the simulation.
            DoCommence?.Invoke(this, new CommenceArgs() { CancelToken = cancelToken });
        }

        /// <summary>
        /// The simulation has stopped. Perform cleanup.
        /// </summary>
        internal void End()
        {
            IsRunning = false; 
            Completed?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Gets the locater model.
        /// </summary>
        protected override Locater Locator()
        {
            return Locater;
        }

        /// <summary>
        /// Document the model, and any child models which should be documented.
        /// </summary>
        /// <remarks>
        /// It is a mistake to call this method without first resolving links.
        /// </remarks>
        public override IEnumerable<ITag> Document()
        {
            yield return new Section(Name, DocumentChildren());
        }

        private IEnumerable<ITag> DocumentChildren()
        {
            foreach (ITag tag in DocumentChildren<Memo>())
                yield return tag;
            foreach (ITag tag in DocumentChildren<Graph>())
                yield return tag;
            foreach (ITag tag in DocumentChildren<Map>())
                yield return tag;
            foreach (ITag tag in FindAllDescendants<Manager>().SelectMany(m => m.Document()))
                yield return tag;
        }
    }
}