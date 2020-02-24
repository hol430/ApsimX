using System;
using Models.Core;

namespace Models.Functions.SupplyFunctions.DCAPST
{
    /// <summary>
    /// Models the environmental temperature
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IPhotosynthesisModel))]
    public class TemperatureModel : Model, ITemperature
    {
        /// <summary>
        /// The solar geometry
        /// </summary>
        [Link]
        ISolarGeometry Solar = null;

        /// <summary>
        /// The atmospheric pressure
        /// </summary>
        [Description("Atmospheric pressure")]
        [Units("")]
        public double AtmosphericPressure { get; set; } = 1.01325;

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
        /// The daily maximum temperature
        /// </summary>
        public double MaxTemperature { get; set; }

        /// <summary>
        /// The daily minimum temperature
        /// </summary>
        public double MinTemperature { get; set; }        

        /// <summary>
        /// The current air temperature
        /// </summary>
        public double AirTemperature { get; set; }

        /// <summary>
        /// Air density in mols
        /// </summary>
        public double AirMolarDensity => ((AtmosphericPressure * 100000) / (287 * (AirTemperature + 273))) * (1000 / 28.966);

        /// <summary>
        /// Calculates the air temperature based on the current time
        /// </summary>
        public void UpdateAirTemperature(double time)
        {
            if (time < 0 || 24 < time) throw new Exception("The time must be between 0 and 24");

            double timeOfMinT = 12.0 - Solar.DayLength / 2.0 + ZLag;
            double deltaT = MaxTemperature - MinTemperature;

            if /*DAY*/ (timeOfMinT < time && time < Solar.Sunset)
            {
                double m = time - timeOfMinT;
                AirTemperature = deltaT * Math.Sin((Math.PI * m) / (Solar.DayLength + 2 * XLag)) + MinTemperature;
            }
            else /*NIGHT*/
            {
                double n = time - Solar.Sunset;
                if (n < 0) n += 24;

                double tempChange = deltaT * Math.Sin(Math.PI * (Solar.DayLength - ZLag) / (Solar.DayLength + 2 * XLag));
                AirTemperature = MinTemperature + tempChange * Math.Exp(-YLag * n / (24.0 - Solar.DayLength));
            }
        }
    }
}
