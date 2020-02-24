﻿namespace Models.Functions.SupplyFunctions.DCAPST
{
    /// <summary>
    /// Describes the pathway parameters
    /// </summary>
    public interface IPathwayParameters
    {       
        /// <summary>
        /// Ratio of intercellular CO2 to air CO2
        /// </summary>
        double IntercellularToAirCO2Ratio { get; set; }        
        
        /// <summary>
        /// Mesophyll electron transport fraction
        /// </summary>
        double MesophyllElectronTransportFraction { get; set; }
        
        /// <summary>
        /// ATP production electron transport factor
        /// </summary>
        double ATPProductionElectronTransportFactor { get; set; }
 
        /// <summary>
        /// Extra ATP cost
        /// </summary>
        double ExtraATPCost { get; set; }

        /// <summary>
        /// Describes how Rubisco carboxylation changes with temperature
        /// </summary>
        TemperatureResponseValues RubiscoCarboxylation { get; set; }

        /// <summary>
        /// Describes how Rubisco oxygenation changes with temperature
        /// </summary>
        TemperatureResponseValues RubiscoOxygenation { get; set; }

        /// <summary>
        /// Describes how Rubisco carboxylation to oxygenation changes with temperature
        /// </summary>
        TemperatureResponseValues RubiscoCarboxylationToOxygenation { get; set; }

        /// <summary>
        /// Describes how Rubisco activity changes with temperature
        /// </summary>
        TemperatureResponseValues RubiscoActivity { get; set; }

        /// <summary>
        /// Describes how PEPc changes with temperature
        /// </summary>
        TemperatureResponseValues PEPc { get; set; }

        /// <summary>
        /// Describes how PEPc activity changes with temperature
        /// </summary>
        TemperatureResponseValues PEPcActivity { get; set; }

        /// <summary>
        /// Describes how Respiration changes with temperature
        /// </summary>
        TemperatureResponseValues Respiration { get; set; }

        /// <summary>
        /// Describes how electron transport rate changes with temperature
        /// </summary>
        LeafTemperatureParameters ElectronTransportRateParams { get; set; }

        /// <summary>
        /// Describes how Mesophyll conductance changes with temperature
        /// </summary>
        LeafTemperatureParameters MesophyllCO2ConductanceParams { get; set; }

        /// <summary>
        /// Spectral correction factor
        /// </summary>
        double SpectralCorrectionFactor { get; set; }

        /// <summary>
        /// Fraction of photosystem II activity in the bundle sheath
        /// </summary>
        double PS2ActivityFraction { get; set; }
        
        /// <summary>
        /// PEP regeneration rate per leaf area
        /// </summary>
        double PEPRegeneration { get; set; }
        
        /// <summary>
        /// Bundle sheath CO2 conductance per leaf
        /// </summary>
        double BundleSheathConductance { get; set; }
    }
}
