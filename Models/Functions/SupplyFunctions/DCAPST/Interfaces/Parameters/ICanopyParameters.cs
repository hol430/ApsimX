namespace Models.Functions.SupplyFunctions.DCAPST
{
    /// <summary>
    /// Possible canopy types
    /// </summary>
    public enum CanopyType { 

        /// <summary>
        /// C3 canopy type
        /// </summary>
        C3, 

        /// <summary>
        /// C4 canopy type
        /// </summary>
        C4, 

        /// <summary>
        /// CCM canopy type
        /// </summary>
        CCM
    }

    /// <summary>
    /// Describes parameters used by a crop canopy to calculate photosynthesis
    /// </summary>
    public interface ICanopyParameters
    {
        /// <summary>
        /// The type of canopy
        /// </summary>
        CanopyType Type { get; set; }

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
