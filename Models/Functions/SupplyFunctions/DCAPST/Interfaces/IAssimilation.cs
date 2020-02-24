namespace Models.Functions.SupplyFunctions.DCAPST
{
    /// <summary>
    /// Describes an assimilation
    /// </summary>
    public interface IAssimilation
    {
        /// <summary>
        /// Initialises the assimilation pathways
        /// </summary>
        void Initialise(double temperature);

        /// <summary>
        /// Attempts to calculate possible changes to the assimilation value under current conditions.
        /// </summary>
        void UpdateAssimilation(WaterParameters Params);        

        /// <summary>
        /// Gets the rate of CO2 assimilation
        /// </summary>
        double GetCO2Rate();

        /// <summary>
        /// Gets the water used by the CO2 assimilation
        /// </summary>
        double GetWaterUse();
    }
}
