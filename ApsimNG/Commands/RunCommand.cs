namespace UserInterface.Commands
{
    using Models.Core;
    using Models.Core.Run;
    using Presenters;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Media;
    using System.Threading;
    using System.Threading.Tasks;
    using Utility;

    public sealed class RunCommand
    {
        /// <summary>The name of the job</summary>
        private string jobName;

        /// <summary>The job to be run.</summary>
        private IRunnable job;

        /// <summary>The explorer presenter.</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>List of all errors encountered</summary>
        private List<Exception> errors = new List<Exception>();

        /// <summary>
        /// Start time of the task.
        /// </summary>
        private DateTime startTime;

        /// <summary>
        /// The task used to run the model.
        /// </summary>
        private Task runTask;

        /// <summary>
        /// The cancellation token.
        /// </summary>
        private CancellationTokenSource cts;

        /// <summary>
        /// Task to be executed (by the caller) when the command finishes running.
        /// </summary>
        private Action onCompleted;

        /// <summary>Constructor</summary>
        /// <param name="model">The model to be run.</param>
        /// <param name="presenter">The explorer presenter.</param>
        /// <param name="onCompleted">Action to be invoked when the job is finished.</param>
        public RunCommand(IModel model, ExplorerPresenter presenter, Action onCompleted) : this(model.Name, model.CreateRunnable(), presenter, onCompleted)
        {
        }

        /// <summary>Constructor</summary>
        /// <param name="name">Name of the job.</param>
        /// <param name="runnable">The job to be run.</param>
        /// <param name="presenter">The explorer presenter.</param>
        /// <param name="onCompleted">Action to be invoked when the job is finished.</param>
        public RunCommand(string name, IRunnable runnable, ExplorerPresenter presenter, Action onCompleted)
        {
            this.job = runnable;
            jobName = name;
            explorerPresenter = presenter;
            this.onCompleted = onCompleted;
        }

        /// <summary>Perform the command</summary>
        public void Do()
        {
            explorerPresenter.MainPresenter.ClearStatusPanel();

            cts = new CancellationTokenSource();
            startTime = DateTime.Now;
            runTask = Task.Run(() => job.Run(OnUpdateStatus, OnUpdateProgress, OnException, cts))
                          .ContinueWith(OnAllJobsCompleted)
                          .ContinueWith(_ => onCompleted);
            explorerPresenter.MainPresenter.AddStopHandler(OnStopSimulation);
        }

        /// <summary>
        /// Called when the runner task wants to provide a status update.
        /// </summary>
        /// <param name="status">The status message.</param>
        private void OnUpdateStatus(string status)
        {
            explorerPresenter.MainPresenter.ShowProgressMessage($"{jobName} running ({status})");
        }

        /// <summary>
        /// Called by the runner task to provide a progress update.
        /// </summary>
        /// <param name="progress">Task progress in range [0, 1].</param>
        private void OnUpdateProgress(double progress)
        {
            explorerPresenter.MainPresenter.ShowProgress(progress);
        }

        /// <summary>
        /// Called by the runner task to signal an error.
        /// </summary>
        /// <param name="error">The error details.</param>
        private void OnException(Exception error)
        {
            errors.Add(error);
            explorerPresenter.MainPresenter.ShowError(error, false);
        }

        /// <summary>All jobs have completed</summary>
        private void OnAllJobsCompleted(Task completedTask)
        {
            if (errors.Count == 0)
            {
                TimeSpan duration = DateTime.Now - startTime;
                explorerPresenter.MainPresenter.ShowMessage(string.Format("{0} complete [{1} sec]", jobName, duration.TotalSeconds.ToString("#.00")), Simulation.MessageType.Information, false);
            }
            // We don't need to display error messages now - they are displayed as they occur.

            if (!Configuration.Settings.Muted)
            {
                // Play a completion sound.
                SoundPlayer player = new SoundPlayer();
                if (errors.Count > 0)
                {
                    if (File.Exists(Configuration.Settings.SimulationCompleteWithErrorWavFileName))
                        player.SoundLocation = Configuration.Settings.SimulationCompleteWithErrorWavFileName;
                    else
                        player.Stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("ApsimNG.Resources.Sounds.Fail.wav");
                }
                else
                {
                    if (File.Exists(Configuration.Settings.SimulationCompleteWavFileName))
                        player.SoundLocation = Configuration.Settings.SimulationCompleteWavFileName;
                    else
                        player.Stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("ApsimNG.Resources.Sounds.Success.wav");
                }

                player.Play();
            }
        }

        /// <summary>
        /// Handles a signal that we want to abort the set of simulations.
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">Event arguments. Shouldn't be anything of interest</param>
        private void OnStopSimulation(object sender, EventArgs e)
        {
            cts.Cancel();
            runTask.Wait();

            explorerPresenter.MainPresenter.HideProgressBar();
            this.explorerPresenter.MainPresenter.RemoveStopHandler(OnStopSimulation);

            // Any error messages will already be onscreen, as they are
            // rendered as they occur.
            explorerPresenter.MainPresenter.ShowMessage($"{jobName} aborted", Simulation.MessageType.Information, false);
        }
    }
}
