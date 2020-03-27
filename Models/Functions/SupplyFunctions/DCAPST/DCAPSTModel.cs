﻿using System;
using System.Linq;

using Models.Core;
using Models.PMF;
using Models.PMF.Organs;
using Models.Interfaces;
using Models.Soils;

namespace Models.Functions.SupplyFunctions.DCAPST
{
    /// <summary>
    /// Models daily biomass growth due to photosynthetic activity
    /// </summary>
    [Serializable]
    [Description("Calculates daily biomass growth due to photosynthetic activity, using the DCAPST model." +
        "DCAPST only activates once the LAI reaches 0.5, prior to that the normal RUE model is used." +
        "DCAPST stays in use even if the LAI falls below 0.5 again.")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Model))]
    public class DCAPSTModel : Model, IDCAPSTModel
    {
        #region Links

        /// <summary>
        /// The solar geometry
        /// </summary>
        [Link]
        ISolarGeometry Solar = null;

        /// <summary>
        /// The solar radiation
        /// </summary>
        [Link]
        ISolarRadiation Radiation = null;

        /// <summary>
        /// The environmental temperature
        /// </summary>
        [Link]
        ITemperature Temperature = null;

        /// <summary>
        /// The canopy undergoing photosynthesis
        /// </summary>
        [Link]
        ICanopyAttributes Canopy = null;

        /// <summary>
        /// The part of the canopy in sunlight
        /// </summary>
        [Link(ByName = true)]
        IAssimilationArea Sunlit = null;

        /// <summary>
        /// The part of the canopy in shade
        /// </summary>
        [Link(ByName = true)]
        IAssimilationArea Shaded = null;

        /// <summary>
        /// The root-shoot ratio
        /// </summary>
        [Link(ByName = true)]
        IFunction RootShoot = null;

        /// <summary>
        /// Describes how electron transport rate changes with temperature
        /// </summary>
        [Link(ByName = true)]
        TemperatureResponseParameters ElectronTransportRate = null;

        /// <summary>
        /// Describes how Mesophyll conductance changes with temperature
        /// </summary>
        [Link(ByName = true)]
        TemperatureResponseParameters MesophyllCO2Conductance = null;

        [Link]
        SorghumArbitrator arbitrator = null;

        #endregion

        #region Fields

        /* Do not change these variables or make them public until the code has been 
         * fully adjusted to use the timestep model, i.e. all calculations which are 
         * assumed to operate over 1 hour (3600 seconds)
         */

        /// <summary>
        /// Daily photosynthesis start time (hours)
        /// </summary>
        private readonly double start = 6.0;

        /// <summary>
        /// Daily photosynthesis end time (hours)
        /// </summary>
        private readonly double end = 18.0;

        /// <summary>
        /// Frequency with which to calculate biomass accumulation (hours)
        /// </summary>
        private readonly double timestep = 1.0;
        
        /// <summary>
        /// Total number of timesteps
        /// </summary>
        private int iterations;

        #endregion

        #region Properties

        /// <summary>
        /// Biochemical Conversion and Maintenance Respiration
        /// </summary>
        [Description("Biochemical conversion and maintenance respiration")]
        [Units("")]
        public double B { get; set; } = 0.409;

        /// <summary>
        /// The peak potential biomass growth in a day
        /// </summary>
        public double PotentialBiomass { get; private set; }

        /// <summary>
        /// The actual biomass growth in a day
        /// </summary>
        public double ActualBiomass { get; private set; }

        /// <summary>
        /// Maximum water demand of the canopy
        /// </summary>
        public double WaterDemanded { get; private set; }

        /// <summary>
        /// Total water supplied to the canopy
        /// </summary>
        public double WaterSupplied { get; private set; }

        /// <summary>
        /// Total daily intercepted radiation
        /// </summary>
        public double InterceptedRadiation { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// Calculates the potential and actual biomass growth of a canopy across the span of a day,
        /// as well as the water requirements for both cases.
        /// </summary>        
        public void DailyRun()
        {
            // TODO: Check where this value comes from
            double maxTranspiration = 100;

            iterations = (int)Math.Floor(1.0 + ((end - start) / timestep));

            Solar.InitialiseDay();
            Canopy.InitialiseDay();

            // POTENTIAL CALCULATIONS
            // Note: In the potential case, we assume unlimited water and therefore supply = demand
            CalculatePotential(out double intercepted, out double[] assimilations, out double[] sunlitDemand, out double[] shadedDemand);
            var waterDemands = sunlitDemand.Zip(shadedDemand, (x, y) => x + y).ToArray();
            var potential = assimilations.Sum();
            var totalDemand = waterDemands.Sum();

            // ACTUAL CALCULATIONS
            // Limit water to supply available from Apsim
            double maxHourlyT = Math.Min(waterDemands.Max(), maxTranspiration);
            waterDemands = waterDemands.Select(w => w > maxHourlyT ? maxHourlyT : w).ToArray();

            //var available = water.ESW.Sum();
            double available = arbitrator.WatSupply;
            var limitedSupply = CalculateWaterSupplyLimits(available, maxHourlyT, waterDemands);

            var actual = (available > totalDemand) ? potential : CalculateActual(limitedSupply, sunlitDemand, shadedDemand);

            var hrs_to_seconds = 3600;

            ActualBiomass = actual * hrs_to_seconds / 1000000 * 44 * B / (1 + RootShoot.Value());
            PotentialBiomass = potential * hrs_to_seconds / 1000000 * 44 * B / (1 + RootShoot.Value());
            WaterDemanded = totalDemand;
            WaterSupplied = (available < totalDemand) ? limitedSupply.Sum() : waterDemands.Sum();
            InterceptedRadiation = intercepted;
        }

        /// <summary>
        /// Attempt to initialise models based on the current time, and test if they are sensible
        /// </summary>
        private bool TryInitiliase(double time)
        {
            Temperature.UpdateAirTemperature(time);
            Radiation.UpdateRadiationValues(time);
            var sunAngle = Solar.SunAngle(time);            
            Canopy.DoSolarAdjustment(sunAngle);

            return IsSensible();
        }

        /// <summary>
        /// Tests if the basic conditions for photosynthesis to occur are met
        /// </summary>
        private bool IsSensible()
        {
            bool[] tempConditions = new bool[4]
            {
                Temperature.AirTemperature > ElectronTransportRate.TMax,
                Temperature.AirTemperature < ElectronTransportRate.TMin,
                Temperature.AirTemperature > MesophyllCO2Conductance.TMax,
                Temperature.AirTemperature < MesophyllCO2Conductance.TMin
            };

            bool invalidTemp = tempConditions.Any(b => b == true);
            bool invalidRadn = Radiation.Total <= double.Epsilon;

            if (invalidTemp || invalidRadn)
                return false;
            else
                return true;
        }

        /// <summary>
        /// Determine the total potential biomass for the day under ideal conditions
        /// </summary>
        public void CalculatePotential(out double intercepted, out double[] assimilations, out double[] sunlitDemand, out double[] shadedDemand)
        {
            // Water demands
            intercepted = 0.0;
            sunlitDemand = new double[iterations];
            shadedDemand = new double[iterations];
            assimilations = new double[iterations];

            for (int i = 0; i < iterations; i++)
            {
                double time = start + i * timestep;

                // Note: double arrays default value is 0.0, which is the intended case if initialisation fails
                if (!TryInitiliase(time)) continue;

                intercepted += Radiation.Total * Canopy.GetInterceptedRadiation() * 3600;

                Canopy.DoTimestepUpdate();

                sunlitDemand[i] = Sunlit.WaterUse;
                shadedDemand[i] = Shaded.WaterUse;
                assimilations[i] = Sunlit.CO2AssimilationRate + Shaded.CO2AssimilationRate;
            }
        }

        /// <summary>
        /// Determine the total biomass that can be assimilated under the actual conditions 
        /// </summary>
        public double CalculateActual(double[] waterSupply, double[] sunlitDemand, double[] shadedDemand)
        {
            double assimilation = 0.0;
            for (int i = 0; i < iterations; i++)
            {
                double time = start + i * timestep;

                // Note: double array values default to 0.0, which is the intended case if initialisation fails
                if (!TryInitiliase(time)) continue;

                double total = sunlitDemand[i] + shadedDemand[i];
                Canopy.DoTimestepUpdate(waterSupply[i], sunlitDemand[i] / total, shadedDemand[i] / total);

                assimilation += Sunlit.CO2AssimilationRate + Shaded.CO2AssimilationRate;
            }
            return assimilation;
        }

        /// <summary>
        /// In the case where there is greater water demand than supply allows, the water supply limit for each hour
        /// must be calculated. 
        /// 
        /// This is done by adjusting the maximum rate of water supply each hour, until the total water demand across
        /// the day is within some tolerance of the actual water available, as we want to make use of all the 
        /// accessible water.
        /// </summary>
        private double[] CalculateWaterSupplyLimits(double soilWaterAvail, double maxHourlyT, double[] demand)
        {
            double initialDemand = demand.Sum();
            if (soilWaterAvail < 0.0001) return demand.Select(d => 0.0).ToArray();
            if (initialDemand < soilWaterAvail) return demand;
            
            double maxDemandRate = maxHourlyT;
            double minDemandRate = 0;
            double averageDemandRate = 0;

            double dailyDemand = initialDemand;

            // While the daily demand is outside some tolerance of the available water
            while (dailyDemand < (soilWaterAvail - 0.000001) || (0.000001 + soilWaterAvail) < dailyDemand)
            {
                averageDemandRate = (maxDemandRate + minDemandRate) / 2;

                // Find the total daily demand when the hourly rate is limited to the average rate
                dailyDemand = demand.Select(d => d > averageDemandRate ? averageDemandRate : d).Sum();

                // Find the total daily demand when the hourly rate is limited to the maximum rate
                var maxDemand = demand.Select(d => d > maxDemandRate ? maxDemandRate : d).Sum();

                // If there is more water available than is being demanded, adjust the minimum demand upwards
                if (dailyDemand < soilWaterAvail) minDemandRate = averageDemandRate;
                // Else, there is less water available than is being demanded, so adjust the maximum demand downwards
                else maxDemandRate = averageDemandRate;
            }
            return demand.Select(d => d > averageDemandRate ? averageDemandRate : d).ToArray();
        }

        #endregion
    }
}