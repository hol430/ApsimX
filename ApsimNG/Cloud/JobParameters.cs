using Models.Core;
using System;

namespace ApsimNG.Cloud
{
    /// <summary>Options exposed to the user for cloud job submission.</summary>
    public class JobParameters
    {
        /// <summary>Display name.</summary>
        public string Name { get; set; }

        /// <summary>Job ID.</summary>
        /// <remarks>Hopefully a Guid is not Azure-specific.</remarks>
        public Guid ID { get; set; }

        /// <summary>Model to be run.</summary>
        public IModel Model { get; set; }

        /// <summary>If true, model files will be saved after they are generated.</summary>
        public bool SaveModelFiles { get; set; }

        /// <summary>Directory to which model files should be saved.</summary>
        public string ModelPath { get; set; }

        /// <summary>If true, upload apsim from a directory. If false, upload apsim from a .zip file.</summary>
        public bool ApsimFromDir { get; set; }

        /// <summary>Directory or zip file containing ApsimX to be uploaded.</summary>
        public string ApsimPath { get; set; }

        /// <summary>Number of cores per process.</summary>
        /// <remarks>todo: refactor this out</remarks>
        public int CoresPerProcess { get { return 1; } }

        /// <summary>Number of VMs per pool.</summary>
        /// <remarks>fixme: this is probably specific to Azure.</remarks>
        public int PoolVMCount { get; set; }

        /// <summary>Maximum number of tasks allowed on a single VM.</summary>
        /// <remarks>fixme</remarks>
        public int PoolMaxTasksPerVM { get { return 16; } }

        /// <summary>Unsure what this does...it's somehow used by the job manager (azure-apsim.exe).</summary>
        public bool AutoScale { get { return true; } }

        /// <summary>If true, an email will be sent to a specified address when the job finishes.</summary>
        public bool SendEmail { get; set; }

        /// <summary>An email will be sent to this address when the job finishes.</summary>
        public string EmailAddress { get; set; }
    }
}
