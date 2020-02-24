using System;
using Models.Core;

namespace Models.Functions.SupplyFunctions.DCAPST
{
    /// <summary>
    /// Implements the pathwayparameters interface
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IPhotosynthesisModel))]
    public class PathwayParameters : Model, IPathwayParameters
    {
        /// <inheritdoc/>
        [Description("Ratio of intercellular CO2 to air CO2")]
        [Units("")]
        public double IntercellularToAirCO2Ratio { get; set; }

        /// <inheritdoc/>
        [Description("Ratio of SLN to respiration")]
        [Units("")]
        public double RespirationSLNRatio { get; set; }

        /// <inheritdoc/>
        [Description("Ratio of SLN to max Rubisco activity")]
        [Units("")]
        public double MaxRubiscoActivitySLNRatio { get; set; }

        /// <inheritdoc/>
        [Description("Ratio of SLN to max electron transport")]
        [Units("")]
        public double MaxElectronTransportSLNRatio { get; set; }

        /// <inheritdoc/>
        [Description("Ratio of SLN to max PEPc activity")]
        [Units("")]
        public double MaxPEPcActivitySLNRatio { get; set; }

        /// <inheritdoc/>
        [Description("Ratio of SLN to Mesophyll CO2 conductance")]
        [Units("")]
        public double MesophyllCO2ConductanceSLNRatio { get; set; }

        /// <inheritdoc/>
        [Description("Mesophyll electron transport fraction")]
        [Units("")]
        public double MesophyllElectronTransportFraction { get; set; }

        /// <inheritdoc/>
        [Description("ATP production electron transport factor")]
        [Units("")]
        public double ATPProductionElectronTransportFactor { get; set; }

        /// <inheritdoc/>
        [Description("Extra ATP cost")]
        [Units("")]
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
        [Description("Spectral correction factor")]
        [Units("")]
        public double SpectralCorrectionFactor { get; set; }

        /// <inheritdoc/>
        [Description("Photosystem II activity fraction")]
        [Units("")]
        public double PS2ActivityFraction { get; set; }

        /// <inheritdoc/>
        [Description("PEP regeneration")]
        [Units("")]
        public double PEPRegeneration { get; set; }

        /// <inheritdoc/>
        [Description("Bundle sheath conductance")]
        [Units("")]
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
