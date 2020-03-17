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
        [Description("Maximum temperature lag coefficient")]
        [Units("hours")]
        public double XLag { get; set; } = 1.8;

        /// <summary>
        /// Accounts for the delay in temperature response to the sun
        /// </summary>
        [Description("Night time temperature lag coefficient")]
        [Units("hours")]
        public double YLag { get; set; } = 2.2;

        /// <summary>
        /// Accounts for the delay in temperature response to the sun
        /// </summary>
        [Description("Minimum temperature lag coefficient")]
        [Units("hours")]
        public double ZLag { get; set; } = 1;

        /// <summary>
        /// The current air temperature
        /// </summary>
        public double AirTemperature { get; set; }

        /// <summary>
        /// Air density in mols / m^3
        /// </summary>
        public double AirMolarDensity
        {
            get
            {
                var atm_to_Pa = 100000;
                var pressure = Weather.AirPressure * atm_to_Pa;

                var molarMassAir = 28.966;

                var kg_to_g = 1000;

                var specificHeat = 287;
                var absolute0C = 273;

                var numerator = pressure * kg_to_g / molarMassAir;
                var denominator = specificHeat * (AirTemperature + absolute0C);

                return numerator / denominator;
            }
        }


        /// <summary>
        /// Calculates the air temperature based on the current time
        /// </summary>
        public void UpdateAirTemperature(double time)
        {
            if (time < 0 || 24 < time) throw new Exception("The time must be between 0 and 24");

            double timeOfMinT = ZLag + 12.0 - Solar.DayLength / 2.0;
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
