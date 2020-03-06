using System;
using Models.Core;
using Models.Interfaces;

namespace Models.Functions.SupplyFunctions.DCAPST
{
    /// <summary>
    /// Models the position of the sun
    /// </summary>
    [Serializable]
    [Description("Models the position of the sun relative to the crop")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IDCAPSTModel))]
    public class SolarGeometry : Model, ISolarGeometry
    {
        [Link]
        IWeather Weather = null;

        [Link]
        IClock Clock = null;

        /// <summary>
        /// The angle between the solar disk and the equatorial plane
        /// </summary>
        private double solarDeclination;

        /// <summary>
        /// The rate at which the suns energy reaches the earth
        /// </summary>
        [Description("Solar constant")]
        [Units("")]
        public double SolarConstant { get; set; } = 1360;
        
        /// <summary>
        /// Time the sun is in the sky (hours)
        /// </summary>
        public double DayLength { get; private set; }
        
        /// <summary>
        /// Time of sunrise (hours)
        /// </summary>
        public double Sunrise { get; private set; }
        
        /// <summary>
        /// Time of sunset (hours)
        /// </summary>
        public double Sunset { get; private set; }

        /// <summary>
        /// Initialise the solar geometry model
        /// </summary>
        public void InitialiseDay()
        {
            solarDeclination = CalcSolarDeclination();
            DayLength = 2 * CalcSunsetAngle().ToDegrees() / 15;
            Sunrise = 12.0 - DayLength / 2.0;
            Sunset = 12.0 + DayLength / 2.0;
        }     

        /// <summary>
        /// Calculates the solar declination angle (radians)
        /// </summary>
        private double CalcSolarDeclination() => 23.45.ToRadians() * Math.Sin(2 * Math.PI * (284 + Clock.Today.DayOfYear) / 365);

        /// <summary>
        /// Calculates the angle of the sun at sunset
        /// </summary>
        private double CalcSunsetAngle() => Math.Acos(-1 * Math.Tan(Weather.Latitude.ToRadians()) * Math.Tan(solarDeclination));
        
        /// <summary>
        /// Calculates the angle of the sun in the sky (radians)
        /// </summary>
        /// <param name="hour">The time in hours</param>        
        public double SunAngle(double hour)
        {
            var angle = Math.Asin(Math.Sin(Weather.Latitude.ToRadians()) * Math.Sin(solarDeclination)
                + Math.Cos(Weather.Latitude.ToRadians())
                * Math.Cos(solarDeclination)
                * Math.Cos(Math.PI / 12.0 * DayLength * (((hour - Sunrise) / DayLength) - 0.5)));
            return angle;
        }
       
    }
}
