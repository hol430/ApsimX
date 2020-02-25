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
    [ValidParent(ParentType = typeof(ITotalCanopy))]
    public class PathwayParameters : Model, IPathwayParameters
    {       
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
        [Description("Empirical curvature factor")]
        [Units("")]
        public double CurvatureFactor { get; set; }

        /// <inheritdoc/>
        [Description("Spectral correction factor")]
        [Units("")]
        public double SpectralCorrectionFactor { get; set; }        

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
