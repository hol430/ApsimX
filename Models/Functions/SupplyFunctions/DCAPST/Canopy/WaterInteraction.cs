﻿using System;
using Models.Core;
using Models.Interfaces;

namespace Models.Functions.SupplyFunctions.DCAPST
{
    /// <summary>
    /// Models how temperature impacts the water used by the leaf during photosynthesis
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ICanopyStructure))]
    public class WaterInteraction : Model, IWaterInteraction
    {
        /// <summary>
        /// The weather
        /// </summary>
        [Link]
        IWeather Weather = null;

        /// <summary> Environment temperature model </summary>
        [Link]
        ITemperature Temperature = null;

        #region Fields
        /// <summary>
        /// Boltzmann's constant
        /// </summary>
        private readonly double kb = 0.0000000567;

        /// <summary>
        /// Volumetric heat capacity of air
        /// </summary>
        private readonly double sAir = 1200;

        /// <summary>
        /// Psychrometric constant
        /// </summary>
        private readonly double g = 0.066;

        /// <summary>
        /// Latent heat of vapourisation
        /// </summary>
        private readonly double latentHeatOfVapourisation = 2447000;

        /// <summary>Heat to water vapour conductance factor</summary>
        private readonly double k = 0.92;

        /// <summary>Boundary water to CO2 diffusion factor</summary>
        private readonly double m = 1.37;

        /// <summary>Stomata water diffusion factor</summary> 
        private readonly double n = 1.6;

        /// <summary> 
        /// Current leaf temperature 
        /// </summary>
        private double leafTemp;

        /// <summary> 
        /// Canopy boundary heat conductance
        /// </summary>
        private double gbh;

        #endregion

        #region Parameters

        /// <summary> Boundary H20 conductance </summary>
        private double Gbw => gbh / k;

        /// <summary> Boundary heat resistance </summary>
        private double Rbh => 1 / gbh;

        /// <summary> Boundary CO2 conductance </summary>
        private double GbCO2 => Weather.AirPressure * Temperature.AirMolarDensity * Gbw / m;

        /// <summary> Outgoing thermal radiation</summary>
        private double ThermalRadiation => 8 * kb * Math.Pow(Temperature.AirTemperature + 273, 3) * (leafTemp - Temperature.AirTemperature);

        /// <summary> Vapour pressure at the leaf temperature </summary>
        private double VpLeaf => 0.61365 * Math.Exp(17.502 * leafTemp / (240.97 + leafTemp));

        /// <summary> Vapour pressure at the air temperature</summary>
        private double VpAir => 0.61365 * Math.Exp(17.502 * Temperature.AirTemperature / (240.97 + Temperature.AirTemperature));

        /// <summary> Vapour pressure at one degree above air temperature</summary>
        private double VpAir1 => 0.61365 * Math.Exp(17.502 * (Temperature.AirTemperature + 1) / (240.97 + (Temperature.AirTemperature + 1)));

        /// <summary> Vapour pressure at the daily minimum temperature</summary>
        private double VptMin => 0.61365 * Math.Exp(17.502 * Weather.MinT / (240.97 + Weather.MinT));

        /// <summary> Difference in air vapour pressures </summary>
        private double DeltaAirVP => VpAir1 - VpAir;

        /// <summary> Leaf to air vapour pressure deficit </summary>
        private double Vpd => VpLeaf - VptMin;

        #endregion

        #region Methods

        /// <summary>
        /// Sets conditions for the water interaction
        /// </summary>
        /// <param name="leafTemp">Leaf temperature</param>
        /// <param name="gbh">Boundary heat conductance</param>
        public void SetConditions(double leafTemp, double gbh)
        {
            this.leafTemp = leafTemp;
            this.gbh = (gbh != 0) ? gbh : throw new Exception("Gbh cannot be 0");
        }
        
        /// <summary>
        /// Calculates the leaf resistance to water when the supply is unlimited
        /// </summary>
        /// <param name="A">CO2 assimilation rate</param>
        /// <param name="Ca">Air CO2 partial pressure</param>
        /// <param name="Ci">Intercellular CO2 partial pressure</param>
        public double UnlimitedWaterResistance(double A, double Ca, double Ci)
        {
            // Unit conversion
            var atm_to_kPa = 100;

            // Leaf water mol fraction
            double Wl = VpLeaf / (Weather.AirPressure * atm_to_kPa);

            // Air water mol fraction
            double Wa = VptMin / (Weather.AirPressure * atm_to_kPa);

            // temporary variables
            double b = (Wl - Wa) * (Ca + Ci) / (2 - (Wl + Wa));
            double c = Ca - Ci;
            double d = A / GbCO2;
            double e = d * (m + n) + m * (b * n - c);
            double f = d * m * n * (d + b * m - c);

            // Stomatal CO2 conductance
            double gsCO2 = 2 * A * m / (Math.Sqrt(e * e - 4 * f) - e);

            // Resistances
            double rsCO2 = 1 / (n * gsCO2); // Stomatal
            double rbCO2 = 1 / (m * GbCO2); // Boundary
            double total = rsCO2 + rbCO2;

            // Total leaf water conductance
            double gtw = 1 / total;

            double rtw = Temperature.AirMolarDensity / gtw * Weather.AirPressure;
            return rtw;
        }

        /// <summary>
        /// Calculates the leaf resistance to water when supply is limited
        /// </summary>
        public double LimitedWaterResistance(double availableWater, double Rn)
        {
            // Transpiration in kilos of water per second
            double ekg = latentHeatOfVapourisation * availableWater / 3600;
            double rtw = (DeltaAirVP * Rbh * (Rn - ThermalRadiation - ekg) + Vpd * sAir) / (ekg * g);
            return rtw;
        }

        /// <summary>
        /// Calculates the hourly water requirements
        /// </summary>
        /// <param name="rtw">Resistance to water</param>
        /// <param name="rn">Radiation</param>
        public double HourlyWaterUse(double rtw, double rn)
        {
            // TODO: Make this work with the timestep model

            // dummy variables
            double a_lump = DeltaAirVP * (rn - ThermalRadiation) + Vpd * sAir / Rbh;
            double b_lump = DeltaAirVP + g * rtw / Rbh;
            double latentHeatLoss = a_lump / b_lump;

            return (latentHeatLoss / latentHeatOfVapourisation) * 3600;
        }

        /// <summary>
        /// Calculates the total CO2 conductance across the leaf
        /// </summary>
        /// <param name="rtw">Resistance to water</param>
        public double TotalCO2Conductance(double rtw)
        {
            // Limited water gsCO2
            var gsCO2 = Temperature.AirMolarDensity * (Weather.AirPressure / (rtw - (1 / Gbw))) / n;
            var boundaryCO2Resistance = 1 / GbCO2;
            var stomatalCO2Resistance = 1 / gsCO2;
            return 1 / (boundaryCO2Resistance + stomatalCO2Resistance);
        }

        /// <summary>
        /// Finds the leaf temperature after the water interaction
        /// </summary>
        public double LeafTemperature(double rtw, double rn)
        {
            // dummy variables
            double a = g * (rn - ThermalRadiation) * rtw / sAir - Vpd;
            double d = DeltaAirVP + g * rtw / Rbh;

            double deltaT = a / d;

            return Temperature.AirTemperature + deltaT;
        }

        #endregion
    }
}
