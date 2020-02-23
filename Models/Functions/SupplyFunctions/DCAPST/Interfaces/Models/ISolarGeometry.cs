namespace Models.Functions.SupplyFunctions.DCAPST
{
    /// <summary>
    /// Describes a solar geometry model
    /// </summary>
    public interface ISolarGeometry
    {
        /// <summary>
        /// Time of sunrise
        /// </summary>
        double Sunrise { get; }

        /// <summary>
        /// Time of sunset
        /// </summary>
        double Sunset { get; }

        /// <summary>
        /// Total time the sun is up
        /// </summary>
        double DayLength { get; }

        /// <summary>
        /// Mean solar radiation per unit area
        /// </summary>
        double SolarConstant { get; }

        /// <summary>
        /// Initialises the solar geometry
        /// </summary>
        void InitialiseDay();

        /// <summary>
        /// Calculates the angle of the sun in the sky at the given time
        /// </summary>
        double SunAngle(double time);
    }
}
