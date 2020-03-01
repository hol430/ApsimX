namespace Models.Functions.SupplyFunctions.DCAPST
{
    /// <summary>
    /// Describes a temperature model
    /// </summary>
    public interface ITemperature
    {
        /// <summary>
        /// Air density on location in terms of mols
        /// </summary>
        double AirMolarDensity { get; }

        /// <summary>
        /// Current air temperature
        /// </summary>
        double AirTemperature { get; }

        /// <summary>
        /// Sets the AirTemperature value based on the provided time
        /// </summary>
        void UpdateAirTemperature(double time);
    }
}
