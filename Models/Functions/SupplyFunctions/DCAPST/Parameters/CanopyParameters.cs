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
        [Description("Average leaf angle (relative to horizontal)")]
        [Units("Degrees")]
        public double LeafAngle { get; set; }

        /// <inheritdoc/>
        [Description("Average leaf width")]
        [Units("")]
        public double LeafWidth { get; set; }

        /// <inheritdoc/>
        [Description("Leaf-level coefficient of scattering radiation")]
        [Units("")]
        public double LeafScatteringCoeff { get; set; }

        /// <inheritdoc/>
        [Description("Leaf-level coefficient of scattering NIR")]
        [Units("")]
        public double LeafScatteringCoeffNIR { get; set; }

        /// <inheritdoc/>
        [Description("Diffuse radiation extinction coefficient")]
        [Units("")]
        public double DiffuseExtCoeff { get; set; }

        /// <inheritdoc/>
        [Description("Diffuse NIR extinction coefficient")]
        [Units("")]
        public double DiffuseExtCoeffNIR { get; set; }

        /// <inheritdoc/>
        [Description("Diffuse radiation reflection coefficient")]
        [Units("")]
        public double DiffuseReflectionCoeff { get; set; }

        /// <inheritdoc/>
        [Description("Diffuse NIR reflection coefficient")]
        [Units("")]
        public double DiffuseReflectionCoeffNIR { get; set; }

        /// <inheritdoc/>
        [Description("Local wind speed")]
        [Units("")]
        public double Windspeed { get; set; }

        /// <inheritdoc/>
        [Description("Wind speed extinction coefficient")]
        [Units("")]
        public double WindSpeedExtinction { get; set; }

        /// <inheritdoc/>
        [Description("Empirical curvature factor")]
        [Units("")]
        public double CurvatureFactor { get; set; }

        /// <inheritdoc/>
        [Description("Diffusivity solubility ratio")]
        [Units("")]
        public double DiffusivitySolubilityRatio { get; set; }

        /// <inheritdoc/>
        [Description("Minimum nitrogen for assimilation")]
        [Units("")]
        public double MinimumN { get; set; }

        /// <inheritdoc/>
        [Description("Ratio of average SLN to canopy top SLN")]
        [Units("")]
        public double SLNRatioTop { get; set; }
    }
}
