using APSIM.Shared.JobRunning;
using System.Collections.Generic;

namespace Models.Core.Run
{
    /// <summary>An interface for a model that can be run.</summary>
    public interface ISimulationsRunnable
    {
        /// <summary>Gets the base simulation to run.</summary>
        Simulation BaseSimulation { get; }

        /// <summary>Gets simulation description.</summary>
        IEnumerable<FactorLevel> GetSimulationDescription();
    }
}
