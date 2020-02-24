using System;
using Models.Core;

namespace Models.Functions.SupplyFunctions.DCAPST
{
    /// <summary>
    /// Implements the canopy parameters
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IPhotosynthesisModel))]
    public class CanopyParameters : Model, ICanopyParameters
    {
        /// <inheritdoc/>
        [Description("Canopy type")]
        [Units("")]
        public CanopyType Type { get; set; }

        /// <inheritdoc/>
        [Description("Partial pressure of O2 in air")]
        [Units("μbar")]
        public double AirO2 { get; set; }

        /// <inheritdoc/>
        [Description("Partial pressure of CO2 in air")]
        [Units("")]
        public double AirCO2 { get; set; }

        /// <inheritdoc/>
        [Description("Empirical curvature factor")]
        [Units("")]
        public double CurvatureFactor { get; set; }

        /// <inheritdoc/>
        [Description("Diffusivity solubility ratio")]
        [Units("")]
        public double DiffusivitySolubilityRatio { get; set; }
        
    }
}
