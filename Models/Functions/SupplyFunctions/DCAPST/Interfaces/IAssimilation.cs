﻿namespace Models.Functions.SupplyFunctions.DCAPST
{
    /// <summary>
    /// Describes an assimilation
    /// </summary>
    public interface IAssimilation
    {
        /// <summary>
        /// Partial pressure of O2 in air (microbar)
        /// </summary>
        double AirO2 { get; set; }

        /// <summary>
        /// Partial pressure of CO2 in air (microbar)
        /// </summary>
        double AirCO2 { get; set; }

        /// <summary>
        /// Ratio of intercellular CO2 to air CO2
        /// </summary>
        double IntercellularToAirCO2Ratio { get; set; }

        /// <summary>
        /// The ratio of diffusivity to solubility
        /// </summary>
        double DiffusivitySolubilityRatio { get; set; }

        /// <summary>
        /// PEP regeneration rate per leaf area
        /// </summary>
        double PEPRegeneration { get; set; }

        /// <summary>
        /// Bundle sheath CO2 conductance per leaf
        /// </summary>
        double BundleSheathConductance { get; set; }

        /// <summary>
        /// 
        /// </summary>
        AssimilationFunction GetFunction(AssimilationPathway pathway, TemperatureResponse leaf);

        /// <summary>
        /// Attempts to calculate possible changes to the assimilation value under current conditions.
        /// </summary>
        void UpdatePartialPressures(AssimilationPathway pathway, TemperatureResponse leaf, AssimilationFunction function);

        /// <summary>
        /// 
        /// </summary>
        void UpdateIntercellularCO2(AssimilationPathway pathway, double gt, double waterUseMolsSecond);
    }
}