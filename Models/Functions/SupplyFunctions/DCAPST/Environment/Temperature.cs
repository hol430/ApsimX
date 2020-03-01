using System;
using Models.Core;
using Models.Interfaces;

namespace Models.Functions.SupplyFunctions.DCAPST
{
    /// <summary>
    /// Models the environmental temperature
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IDCAPSTModel))]
    public class Temperature : Model, ITemperature
    {
        /// <summary>
        /// The weather
        /// </summary>
        [Link]
        IWeather Weather = null;

        /// <summary>
        /// The solar geometry
        /// </summary>
        [Link]
        ISolarGeometry Solar = null;

        /// <summary>
        /// Accounts for the delay in temperature response to the sun
        /// </summary>
        [Description("X lag")]
        [Units("")]
        public double XLag { get; set; } = 1.8;

        /// <summary>
        /// Accounts for the delay in temperature response to the sun
        /// </summary>
        [Description("Y lag")]
        [Units("")]
        public double YLag { get; set; } = 2.2;

        /// <summary>
        /// Accounts for the delay in temperature response to the sun
        /// </summary>
        [Description("Z lag")]
        [Units("")]
        public double ZLag { get; set; } = 1;       

        /// <summary>
        /// The current air temperature
        /// </summary>
        public double AirTemperature { get; set; }

        /// <summary>
        /// Air density in mols
        /// </summary>
        public double AirMolarDensity => ((Weather.AirPressure * 100000) / (287 * (AirTemperature + 273))) * (1000 / 28.966);

        /// <summary>
        /// Calculates the air temperature based on the current time
        /// </summary>
        public void UpdateAirTemperature(double time)
        {
            if (time < 0 || 24 < time) throw new Exception("The time must be between 0 and 24");

            double timeOfMinT = 12.0 - Solar.DayLength / 2.0 + ZLag;
            double deltaT = Weather.MaxT - Weather.MinT;

            if /*DAY*/ (timeOfMinT < time && time < Solar.Sunset)
            {
                double m = time - timeOfMinT;
                AirTemperature = deltaT * Math.Sin((Math.PI * m) / (Solar.DayLength + 2 * XLag)) + Weather.MinT;
            }
            else /*NIGHT*/
            {
                double n = time - Solar.Sunset;
                if (n < 0) n += 24;

                double tempChange = deltaT * Math.Sin(Math.PI * (Solar.DayLength - ZLag) / (Solar.DayLength + 2 * XLag));
                AirTemperature = Weather.MinT + tempChange * Math.Exp(-YLag * n / (24.0 - Solar.DayLength));
            }
        }
    }
}
