namespace Models.Functions.SupplyFunctions.DCAPST
{
    /// <summary>
    /// Models the structure of the canopy
    /// </summary>
    public interface ICanopyStructure
    {       
        /// <summary>
        /// Performs initial calculations for the canopy provided daily conditions 
        /// </summary>
        void InitialiseDay();

        /// <summary>
        /// Updates the total canopy on a new timestep
        /// </summary>
        void DoTimestepUpdate(double transpiration = -1, double sunFraction = 0, double shadeFraction = 0);

        /// <summary>
        /// Adjusts the properties of the canopy to account for the suns movement across the sky
        /// </summary>
        void DoSolarAdjustment(double sunAngleRadians);

        /// <summary>
        /// Gets the amount of radiation intercepted by the canopy
        /// </summary>
        /// <returns></returns>
        double GetInterceptedRadiation();

        /// <summary>
        /// Calculates the total boundary heat conductance of the canopy
        /// </summary>
        double CalcBoundaryHeatConductance();

        /// <summary>
        /// Calculates the boundary heat conductance of the sunlit area of the canopy
        /// </summary>
        double CalcSunlitBoundaryHeatConductance();
    }
}
