using System;
using System.Collections.Generic;
using System.Linq;

using Models.Core;

namespace Models.Functions.SupplyFunctions.DCAPST
{
    /// <summary>
    /// Models a subsection of the canopy (used for distinguishing between sunlit and shaded)
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ITotalCanopy))]
    public class PartialCanopy : Model, IPartialCanopy
    {
        [Link]
        IAssimilation assimilation = null;

        /// <summary>
        /// Models the leaf water interaction
        /// </summary>
        [Link]
        ILeafWaterInteraction LeafWater = null;

        /// <summary>
        /// Models how the leaf responds to different temperatures
        /// </summary>
        public LeafTemperatureResponseModel Leaf { get; set; }

        /// <inheritdoc/>
        public ParameterRates At25C { get; private set; } = new ParameterRates();        

        /// <summary>
        /// The possible assimilation pathways
        /// </summary>
        protected List<AssimilationPathway> Pathways => Children.OfType<AssimilationPathway>().ToList();        

        /// <inheritdoc/>
        public double LAI { get; set; }

        /// <inheritdoc/>
        public double AbsorbedRadiation { get; set; }

        /// <inheritdoc/>
        public double PhotonCount { get; set; }

        /// <inheritdoc/>
        public double CO2AssimilationRate { get; set; }

        /// <inheritdoc/>
        public double WaterUse { get; set; }

        /// <summary>
        /// Finds the CO2 assimilation rate
        /// </summary>
        public double GetCO2Rate() => Pathways.Min(p => p.CO2Rate);

        /// <summary>
        /// Finds the water used during CO2 assimilation
        /// </summary>
        public double GetWaterUse() => Pathways.Min(p => p.WaterUse);

        /// <summary>
        /// Initialises the assimilation pathways
        /// </summary>
        public void Initialise(double temperature)
        {
            Pathways.ForEach(p =>
            {
                p.Temperature = temperature;
                p.MesophyllCO2 = assimilation.AirCO2 * assimilation.IntercellularToAirCO2Ratio;
                p.ChloroplasticCO2 = p.MesophyllCO2 + 20;
                p.ChloroplasticO2 = 210000;
            });
        }

        /// <summary>
        /// Calculates the CO2 assimilated by the partial canopy during photosynthesis,
        /// and the water used by the process
        /// </summary>
        public void DoPhotosynthesis(ITemperature temperature, WaterParameters Params)
        {
            Initialise(temperature.AirTemperature);

            // Determine initial results
            UpdateAssimilation(Params);

            // Store the initial results in case the subsequent updates fail
            CO2AssimilationRate = GetCO2Rate();
            WaterUse = GetWaterUse();
            
            if (CO2AssimilationRate == 0 || WaterUse == 0) return;

            // Only update assimilation if the initial value is large enough
            if (CO2AssimilationRate >= 0.5)
            {
                for (int n = 0; n < 3; n++)
                {
                    UpdateAssimilation(Params);

                    // If the additional updates fail, the minimum amongst the initial values is taken
                    if (GetCO2Rate() == 0 || GetWaterUse() == 0) return;                    
                }
            }

            // If three iterations pass without failing, update the values to the final result
            CO2AssimilationRate = GetCO2Rate();
            WaterUse = GetWaterUse();
        }

        /// <summary>
        /// Recalculates the assimilation values for each pathway
        /// </summary>
        public void UpdateAssimilation(WaterParameters water) => Pathways.ForEach(p => UpdatePathway(water, p));       

        /// <summary>
        /// Updates the state of an assimilation pathway
        /// </summary>
        private void UpdatePathway(WaterParameters water, AssimilationPathway pathway)
        {
            if (pathway == null) return;

            Leaf.SetConditions(At25C, pathway.Temperature, PhotonCount);
            LeafWater.SetConditions(pathway.Temperature, water.BoundaryHeatConductance);

            double resistance;

            var func = assimilation.GetFunction(pathway, Leaf);
            if (!water.limited) /* Unlimited water calculation */
            {
                pathway.IntercellularCO2 = assimilation.IntercellularToAirCO2Ratio * assimilation.AirCO2;

                func.Ci = pathway.IntercellularCO2;
                func.Rm = 1 / Leaf.GmT;

                pathway.CO2Rate = func.Value();

                resistance = LeafWater.UnlimitedWaterResistance(pathway.CO2Rate, assimilation.AirCO2, pathway.IntercellularCO2);
                pathway.WaterUse = LeafWater.HourlyWaterUse(resistance, AbsorbedRadiation);
            }
            else /* Limited water calculation */
            {
                pathway.WaterUse = water.maxHourlyT * water.fraction;
                var WaterUseMolsSecond = pathway.WaterUse / 18 * 1000 / 3600;

                resistance = LeafWater.LimitedWaterResistance(pathway.WaterUse, AbsorbedRadiation);
                var Gt = LeafWater.TotalCO2Conductance(resistance);

                func.Ci = assimilation.AirCO2 - WaterUseMolsSecond * assimilation.AirCO2 / (Gt + WaterUseMolsSecond / 2.0);
                func.Rm = 1 / (Gt + WaterUseMolsSecond / 2) + 1.0 / Leaf.GmT;

                pathway.CO2Rate = func.Value();

                assimilation.UpdateIntercellularCO2(pathway, Gt, WaterUseMolsSecond);
            }
            assimilation.UpdatePartialPressures(pathway, Leaf, func);

            // New leaf temperature
            pathway.Temperature = (LeafWater.LeafTemperature(resistance, AbsorbedRadiation) + pathway.Temperature) / 2.0;

            // If the assimilation is not sensible zero the values
            if (double.IsNaN(pathway.CO2Rate) || pathway.CO2Rate <= 0.0 || double.IsNaN(pathway.WaterUse) || pathway.WaterUse <= 0.0)
            {
                pathway.CO2Rate = 0;
                pathway.WaterUse = 0;
            }
        }
    }
}
