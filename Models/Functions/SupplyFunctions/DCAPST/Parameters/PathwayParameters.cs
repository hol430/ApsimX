using DCAPST.Interfaces;

namespace DCAPST
{   
    /// <summary>
    /// Implements the pathwayparameters interface
    /// </summary>
    public class PathwayParameters : IPathwayParameters
    {
        /// <inheritdoc/>
        public double IntercellularToAirCO2Ratio { get; set; }

        /// <inheritdoc/>
        public double FractionOfCyclicElectronFlow { get; set; }

        /// <inheritdoc/>
        public double RespirationSLNRatio { get; set; }

        /// <inheritdoc/>
        public double MaxRubiscoActivitySLNRatio { get; set; }

        /// <inheritdoc/>
        public double MaxElectronTransportSLNRatio { get; set; }

        /// <inheritdoc/>
        public double MaxPEPcActivitySLNRatio { get; set; }

        /// <inheritdoc/>
        public double MesophyllCO2ConductanceSLNRatio { get; set; }

        /// <inheritdoc/>
        public double MesophyllElectronTransportFraction { get; set; }

        /// <inheritdoc/>
        public double ATPProductionElectronTransportFactor { get; set; }

        /// <inheritdoc/>
        public double ExtraATPCost { get; set; }

        /// <inheritdoc/>
        public TemperatureResponseValues RubiscoCarboxylation { get; set; }

        /// <inheritdoc/>
        public TemperatureResponseValues RubiscoOxygenation { get; set; }

        /// <inheritdoc/>
        public TemperatureResponseValues RubiscoCarboxylationToOxygenation { get; set; }

        /// <inheritdoc/>
        public TemperatureResponseValues RubiscoActivity { get; set; }

        /// <inheritdoc/>
        public TemperatureResponseValues PEPc { get; set; }

        /// <inheritdoc/>
        public TemperatureResponseValues PEPcActivity { get; set; }

        /// <inheritdoc/>
        public TemperatureResponseValues Respiration { get; set; }

        /// <inheritdoc/>
        public LeafTemperatureParameters ElectronTransportRateParams { get; set; }

        /// <inheritdoc/>
        public LeafTemperatureParameters MesophyllCO2ConductanceParams { get; set; }

        /// <inheritdoc/>

        public double SpectralCorrectionFactor { get; set; }

        /// <inheritdoc/>
        public double PS2ActivityFraction { get; set; }

        /// <inheritdoc/>
        public double PEPRegeneration { get; set; }

        /// <inheritdoc/>
        public double BundleSheathConductance { get; set; }       
    }

    /// <summary>Groups the temperature response terms of a parameter</summary>
    public struct TemperatureResponseValues
    {
        /// <summary>
        /// A scaling factor which changes for each temperature response parameter
        /// </summary>
        public double Factor;

        /// <summary>
        /// The value of the temperature response factor at 25 degrees
        /// </summary>
        public double At25;
    }
}
