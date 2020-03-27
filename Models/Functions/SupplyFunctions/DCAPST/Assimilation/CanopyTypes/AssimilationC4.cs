using System;
using Models.Core;

namespace Models.Functions.SupplyFunctions.DCAPST
{
    /// <summary>
    /// Defines the pathway functions for a C4 canopy
    /// </summary>
    [Serializable]
    [Description("Models how a C4 crop assimilates biomass")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ICanopyAttributes))]
    public class AssimilationC4 : Assimilation
    {       
        /// <summary>
        /// Mesophyll electron transport fraction
        /// </summary>
        [Description("Mesophyll electron transport fraction")]
        [Units("")]
        public double MesophyllElectronTransportFraction { get; set; }

        /// <summary>
        /// Extra ATP cost
        /// </summary>
        [Description("Extra ATP cost")]
        [Units("")]
        public double ExtraATPCost { get; set; }

        /// <inheritdoc/>
        public override void UpdateIntercellularCO2(AssimilationPathway pathway, double gt, double waterUseMolsSecond)
        {
            pathway.IntercellularCO2 = ((gt - waterUseMolsSecond / 2.0) * AirCO2 - pathway.CO2Rate) / (gt + waterUseMolsSecond / 2.0);
        }

        /// <inheritdoc/>
        protected override void UpdateMesophyllCO2(AssimilationPathway pathway, TemperatureResponse leaf)
        {
            pathway.MesophyllCO2 = pathway.IntercellularCO2 - pathway.CO2Rate / leaf.GmT;
        }

        /// <inheritdoc/>
        protected override AssimilationFunction GetAc1Function(AssimilationPathway pathway, TemperatureResponse leaf)
        {
            var x = new double[9];

            x[0] = leaf.VcMaxT;
            x[1] = leaf.Kc / leaf.Ko;
            x[2] = leaf.Kc;
            x[3] = leaf.VpMaxT / (pathway.MesophyllCO2 + leaf.Kp);
            x[4] = 0.0;
            x[5] = 1.0;
            x[6] = 0.0;
            x[7] = 1.0;
            x[8] = 1.0;

            var func = new AssimilationFunction()
            {
                X = x,

                MesophyllRespiration = leaf.GmRd,
                HalfRubiscoSpecificityReciprocal = leaf.Gamma,
                FractionOfDiffusivitySolubilityRatio = 0.1 / DiffusivitySolubilityRatio,
                BundleSheathConductance = pathway.Gbs,
                Oxygen = AirO2,
                Respiration = leaf.RdT
            };

            return func;
        }

        /// <inheritdoc/>
        protected override AssimilationFunction GetAc2Function(AssimilationPathway pathway, TemperatureResponse leaf)
        {
            var x = new double[9];

            x[0] = leaf.VcMaxT;
            x[1] = leaf.Kc / leaf.Ko;
            x[2] = leaf.Kc;
            x[3] = 0.0;
            x[4] = pathway.Vpr;
            x[5] = 1.0;
            x[6] = 0.0;
            x[7] = 1.0;
            x[8] = 1.0;

            var func = new AssimilationFunction()
            {
                X = x,

                MesophyllRespiration = leaf.GmRd,
                HalfRubiscoSpecificityReciprocal = leaf.Gamma,
                FractionOfDiffusivitySolubilityRatio = 0.1 / DiffusivitySolubilityRatio,
                BundleSheathConductance = pathway.Gbs,
                Oxygen = AirO2,
                Respiration = leaf.RdT
            };

            return func;
        }

        /// <inheritdoc/>
        protected override AssimilationFunction GetAjFunction(AssimilationPathway pathway, TemperatureResponse leaf)
        {
            var x = new double[9];

            x[0] = (1.0 - MesophyllElectronTransportFraction) * leaf.J / 3.0;
            x[1] = 7.0 / 3.0 * leaf.Gamma;
            x[2] = 0.0;
            x[3] = 0.0;
            x[4] = MesophyllElectronTransportFraction * leaf.J / ExtraATPCost;
            x[5] = 1.0;
            x[6] = 0.0;
            x[7] = 1.0;
            x[8] = 1.0;

            var func = new AssimilationFunction()
            {
                X = x,

                MesophyllRespiration = leaf.GmRd,
                HalfRubiscoSpecificityReciprocal = leaf.Gamma,
                FractionOfDiffusivitySolubilityRatio = 0.1 / DiffusivitySolubilityRatio,
                BundleSheathConductance = pathway.Gbs,
                Oxygen = AirO2,
                Respiration = leaf.RdT
            };

            return func;
        }
    }
}
