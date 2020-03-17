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
    [ValidParent(ParentType = typeof(ICanopyStructure))]
    public class AssimilationArea : Model, IAssimilationArea
    {
        /// <summary>
        /// The temperature
        /// </summary>
        [Link]
        ITemperature Temperature = null;

        /// <summary>
        /// The assimilation
        /// </summary>
        [Link]
        IAssimilation Assimilation = null;

        /// <summary>
        /// Models the leaf water interaction
        /// </summary>
        [Link]
        IWaterInteraction LeafWater = null;

        /// <summary>
        /// Models how the leaf responds to different temperatures
        /// </summary>
        [Link]
        TemperatureResponse Leaf = null;

        /// <summary>
        /// The possible assimilation pathways
        /// </summary>
        private List<AssimilationPathway> Pathways => Children.OfType<AssimilationPathway>().ToList();        

        /// <inheritdoc/>
        public ParameterRates At25C { get; private set; } = new ParameterRates();

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
        private void Initialise()
        {
            Pathways.ForEach(p =>
            {
                p.Temperature = Temperature.AirTemperature;
                p.MesophyllCO2 = Assimilation.AirCO2 * Assimilation.IntercellularToAirCO2Ratio;
                p.ChloroplasticCO2 = p.MesophyllCO2 + 20;
                p.ChloroplasticO2 = 210000;
            });
        }

        /// <summary>
        /// Calculates the CO2 assimilated by the partial canopy during photosynthesis,
        /// and the water used by the process
        /// </summary>
        public void DoPhotosynthesis(WaterParameters Params)
        {
            Initialise();

            // Determine initial results
            UpdateAssimilation(Params);

            // Store the initial results in case the subsequent updates fail
            CO2AssimilationRate = GetCO2Rate();
            WaterUse = GetWaterUse();

            // Only attempt to converge result if there is sufficient assimilation
            if (CO2AssimilationRate < 0.5 || WaterUse == 0) return;

            // Repeat calculation 3 times to let solution converge
            for (int n = 0; n < 3; n++)
            {
                UpdateAssimilation(Params);

                // If the additional updates fail,stop the process (meaning the initial results used)
                if (GetCO2Rate() == 0 || GetWaterUse() == 0) return;
            }

            // Update results only if convergence succeeds
            CO2AssimilationRate = GetCO2Rate();
            WaterUse = GetWaterUse();
        }

        /// <summary>
        /// Recalculates the assimilation values for each pathway
        /// </summary>
        private void UpdateAssimilation(WaterParameters water) => Pathways.ForEach(p => UpdatePathway(water, p));       

        /// <summary>
        /// Updates the state of an assimilation pathway
        /// </summary>
        private void UpdatePathway(WaterParameters water, AssimilationPathway pathway)
        {
            if (pathway == null) return;

            Leaf.SetConditions(At25C, pathway.Temperature, PhotonCount);
            LeafWater.SetConditions(pathway.Temperature, water.BoundaryHeatConductance);

            double resistance;

            var func = Assimilation.GetFunction(pathway, Leaf);
            if (!water.limited) /* Unlimited water calculation */
            {
                pathway.IntercellularCO2 = Assimilation.IntercellularToAirCO2Ratio * Assimilation.AirCO2;

                func.Ci = pathway.IntercellularCO2;
                func.Rm = 1 / Leaf.GmT;

                pathway.CO2Rate = func.Value();

                resistance = LeafWater.UnlimitedWaterResistance(pathway.CO2Rate, Assimilation.AirCO2, pathway.IntercellularCO2);
                pathway.WaterUse = LeafWater.HourlyWaterUse(resistance, AbsorbedRadiation);
            }
            else /* Limited water calculation */
            {
                var molecularWeightWater = 18;
                var kg_to_g = 1000;
                var hrs_to_seconds = 3600;

                pathway.WaterUse = water.maxHourlyT * water.fraction;
                var WaterUseMolsSecond = pathway.WaterUse / molecularWeightWater * kg_to_g / hrs_to_seconds;

                resistance = LeafWater.LimitedWaterResistance(pathway.WaterUse, AbsorbedRadiation);
                var Gt = LeafWater.TotalCO2Conductance(resistance);

                func.Ci = Assimilation.AirCO2 - WaterUseMolsSecond * Assimilation.AirCO2 / (Gt + WaterUseMolsSecond / 2.0);
                func.Rm = 1 / (Gt + WaterUseMolsSecond / 2) + 1.0 / Leaf.GmT;

                pathway.CO2Rate = func.Value();

                Assimilation.UpdateIntercellularCO2(pathway, Gt, WaterUseMolsSecond);
            }
            Assimilation.UpdatePartialPressures(pathway, Leaf, func);

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
