using System.Collections.Generic;
namespace Models.Core.Run
{
    /// <summary>
    /// Describes a single simulation in terms of its descriptors and replacements against a base simulation.
    /// </summary>
    public class SimulationDesc
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="simulationName">Name of simulation.</param>
        /// <param name="descriptors">Simulation descriptors.</param>
        /// <param name="replacements">Simulation replacements to apply to a base simulation.</param>
        public SimulationDesc(string simulationName, IEnumerable<SimulationDescription.Descriptor> descriptors, IEnumerable<IReplacement> replacements)
        {
            Name = simulationName;
            Replacements = replacements;
            Descriptors = descriptors;
        }

        /// <summary>Name of simulation.</summary>
        public string Name { get; }

        /// <summary>Simulation descriptors.</summary>
        public IEnumerable<SimulationDescription.Descriptor> Descriptors { get; }

        /// <summary>Simulation replacements to apply to a base simulation.</summary>
        public IEnumerable<IReplacement> Replacements { get; }

    }
}
