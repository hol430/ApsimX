using APSIM.Shared.JobRunning;
using System;
using System.Collections.Generic;
using Models.Core.Run;
using System.Linq;
using IRunnable = APSIM.Shared.JobRunning.IRunnable;

namespace APSIM.Server
{
    /// <summary>
    /// This class extends the standard job manager. It's designed to be hold the
    /// jobs in memory, in a state that is ready to be run and can be re-run
    /// multiple times without re-preparing the jobs each time.
    /// </summary>
    public class ServerJobRunner : JobRunner
    {
        public IEnumerable<IReplacement> Replacements { get; set; }
        private List<(IRunnable, IJobManager)> jobs = new List<(IRunnable, IJobManager)>();

        /// <summary>
        /// Get a list of jobs to be run.
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<(IRunnable, IJobManager)> GetJobs() => jobs;

        /// <summary>
        /// Add the job manager and prepare all jobs to be run.
        /// </summary>
        /// <param name="jobManager"></param>
        public override void Add(IJobManager jobManager)
        {
            base.Add(jobManager);
            foreach (Shared.JobRunning.IRunnable job in jobManager.GetJobs())
            {
                job.Prepare();
                jobs.Add(((Shared.JobRunning.IRunnable, IJobManager))(job:(Shared.JobRunning.IRunnable)job, jobManager:(IJobManager)jobManager));
            }
        }

        protected override void Prepare(Shared.JobRunning.IRunnable job)
        {
            // Do nothing - jobs are already prepared at this point.
            // todo: should we call base.Prepare if job is not a simulation?
        }

        protected override void Run(Shared.JobRunning.IRunnable job)
        {
            if (job is SimulationDescription sim)
            {
                sim.Storage.Writer.Clean(new[] { sim.SimulationToRun.Name }, false);
                sim.Run(cancelToken, Replacements);
            }
            else
            {
                base.Prepare(job);
                base.Run(job);
            }
        }
    }
}