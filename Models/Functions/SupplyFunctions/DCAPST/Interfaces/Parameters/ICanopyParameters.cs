﻿namespace Models.Functions.SupplyFunctions.DCAPST
{
    /// <summary>
    /// Describes parameters used by a crop canopy to calculate photosynthesis
    /// </summary>
    public interface ICanopyParameters
    {
        /// <summary>
        /// Partial pressure of CO2 in air (microbar)
        /// </summary>
        double AirCO2 { get; set; }

        /// <summary>
        /// Partial pressure of O2 in air (microbar)
        /// </summary>
        double AirO2 { get; set; }

        /// <summary>
        /// Empirical curvature factor
        /// </summary>
        double CurvatureFactor { get; set; }

        /// <summary>
        /// The ratio of diffusivity to solubility
        /// </summary>
        double DiffusivitySolubilityRatio { get; set; }
    }
}
