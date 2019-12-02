using APSIM.Shared.Utilities;
using System;

namespace ApsimNG.Cloud
{
    /// <summary>Details about a cloud job which has already been submitted.</summary>
    public class JobDetails
    {
        /// <summary>Job ID.</summary>
        public string ID { get; set; }

        /// <summary>Job display name.</summary>
        public string Name { get; set; }

        /// <summary>Status of the job (uploading, finished, etc.).</summary>
        public string State { get; set; }

        /// <summary>Owner of the job (user who submitted the job).</summary>
        public string Owner { get; set; }

        /// <summary>Job progress as a percentage.</summary>
        public double Progress { get; set; }

        /// <summary>Total number of simulations in the job.</summary>
        public long NumSims { get; set; }

        /// <summary>Start time of the job.</summary>
        /// <remarks>fixme: why is this nullable???</remarks>
        public DateTime? StartTime { get; set; }

        /// <summary>End time of the job.</summary>
        public DateTime? EndTime { get; set; }

        /// <summary>Duration of the job.</summary>
        public TimeSpan Duration
        {
            get
            {
                if (StartTime == null || EndTime == null)
                    return TimeSpan.Zero;

                return EndTime.Value - StartTime.Value;
            }
        }
        
        /// <summary>Total CPU time of the job.</summary>
        public TimeSpan CpuTime { get; set; }

        /// <summary>Check if two <see cref="Jobdetails"/> instances are equal. Checks ID, state and progress.</summary>
        /// <param name="job">A <see cref="JobDetails"/> instance.</param>
        public bool Equals(JobDetails job)
        {
            return ID == job.ID && State == job.State && MathUtilities.FloatsAreEqual(Progress, job.Progress);
        }
    }
}
