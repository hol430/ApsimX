using DCAPST.Interfaces;

namespace DCAPST
{
    /// <summary>
    /// Implements the canopy parameters
    /// </summary>
    public class CanopyParameters : ICanopyParameters
    {
        /// <inheritdoc/>
        public CanopyType Type { get; set; }

        /// <inheritdoc/>
        public double AirO2 { get; set; }

        /// <inheritdoc/>
        public double AirCO2 { get; set; }

        /// <inheritdoc/>
        public double LeafAngle { get; set; }

        /// <inheritdoc/>
        public double LeafWidth { get; set; }

        /// <inheritdoc/>
        public double LeafScatteringCoeff { get; set; }

        /// <inheritdoc/>
        public double LeafScatteringCoeffNIR { get; set; }

        /// <inheritdoc/>
        public double DiffuseExtCoeff { get; set; }

        /// <inheritdoc/>
        public double DiffuseExtCoeffNIR { get; set; }

        /// <inheritdoc/>
        public double DiffuseReflectionCoeff { get; set; }

        /// <inheritdoc/>
        public double DiffuseReflectionCoeffNIR { get; set; }

        /// <inheritdoc/>
        public double Windspeed { get; set; }

        /// <inheritdoc/>
        public double WindSpeedExtinction { get; set; }

        /// <inheritdoc/>
        public double CurvatureFactor { get; set; }

        /// <inheritdoc/>
        public double DiffusivitySolubilityRatio { get; set; }

        /// <inheritdoc/>
        public double MinimumN { get; set; }

        /// <inheritdoc/>
        public double SLNRatioTop { get; set; }
    }
}
