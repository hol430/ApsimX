using System;
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
        /// Calculates the CO2 assimilated by the partial canopy during photosynthesis,
        /// and the water used by the process
        /// </summary>
        public void DoPhotosynthesis(ITemperature temperature, WaterParameters Params)
        {
            assimilation.Initialise(temperature.AirTemperature);

            // Determine initial results
            assimilation.UpdateAssimilation(Params);

            // Store the initial results in case the subsequent updates fail
            CO2AssimilationRate = assimilation.GetCO2Rate();
            WaterUse = assimilation.GetWaterUse();
            
            if (CO2AssimilationRate == 0 || WaterUse == 0) return;

            // Only update assimilation if the initial value is large enough
            if (CO2AssimilationRate >= 0.5)
            {
                for (int n = 0; n < 3; n++)
                {
                    assimilation.UpdateAssimilation(Params);

                    // If the additional updates fail, the minimum amongst the initial values is taken
                    if (assimilation.GetCO2Rate() == 0 || assimilation.GetWaterUse() == 0) return;                    
                }
            }

            // If three iterations pass without failing, update the values to the final result
            CO2AssimilationRate = assimilation.GetCO2Rate();
            WaterUse = assimilation.GetWaterUse();
        }
    }
}
