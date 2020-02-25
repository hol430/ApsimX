using System;
using Models.Core;

namespace Models.Functions.SupplyFunctions.DCAPST
{
    /// <summary>
    /// Defines the pathway functions for a CCM canopy
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ITotalCanopy))]
    public class AssimilationCCM : Assimilation
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

        /// <summary>
        /// ATP production electron transport factor
        /// </summary>
        [Description("ATP production electron transport factor")]
        [Units("")]
        public double ATPProductionElectronTransportFactor { get; set; }

        /// <summary>
        /// Fraction of photosystem II activity in the bundle sheath
        /// </summary>
        [Description("Photosystem II activity fraction")]
        [Units("")]
        public double PS2ActivityFraction { get; set; }

        /// <inheritdoc/>
        public override void UpdateIntercellularCO2(AssimilationPathway pathway, double gt, double waterUseMolsSecond)
        {
            pathway.IntercellularCO2 = ((gt - waterUseMolsSecond / 2.0) * AirCO2 - pathway.CO2Rate) / (gt + waterUseMolsSecond / 2.0);
        }

        /// <inheritdoc/>
        protected override void UpdateMesophyllCO2(AssimilationPathway pathway)
        {
            pathway.MesophyllCO2 = pathway.IntercellularCO2 - pathway.CO2Rate / pathway.Leaf.GmT;
        }

        /// <inheritdoc/>
        protected override void UpdateChloroplasticO2(AssimilationPathway pathway)
        {
            pathway.ChloroplasticO2 = PS2ActivityFraction * pathway.CO2Rate / (DiffusivitySolubilityRatio * pathway.Gbs) + AirO2;
        }

        /// <inheritdoc/>
        protected override void UpdateChloroplasticCO2(AssimilationPathway pathway, AssimilationFunction func)
        {
            var a = (pathway.MesophyllCO2 * func.X[3] + func.X[4] - func.X[5] * pathway.CO2Rate - func.MesophyllRespiration - func.X[6]);
            pathway.ChloroplasticCO2 = pathway.MesophyllCO2 + a * func.X[7] / pathway.Gbs;
        }

        /// <inheritdoc/>
        protected override AssimilationFunction GetAc1Function(AssimilationPathway pathway)
        {
            var x = new double[9];

            x[0] = pathway.Leaf.VcMaxT;
            x[1] = pathway.Leaf.Kc / pathway.Leaf.Ko;
            x[2] = pathway.Leaf.Kc;
            x[3] = pathway.Leaf.VpMaxT / (pathway.MesophyllCO2 + pathway.Leaf.Kp);
            x[4] = 0.0;
            x[5] = 0.0;
            x[6] = pathway.ChloroplasticCO2 * pathway.Leaf.VcMaxT / (pathway.ChloroplasticCO2 + pathway.Leaf.Kc * (1 + pathway.ChloroplasticO2 / pathway.Leaf.Ko));
            x[7] = 1.0;
            x[8] = 1.0;

            var func = new AssimilationFunction()
            {
                X = x,

                MesophyllRespiration = pathway.Leaf.GmRd,
                HalfRubiscoSpecificityReciprocal = pathway.Leaf.Gamma,
                FractionOfDiffusivitySolubilityRatio = 0.1 / DiffusivitySolubilityRatio,
                BundleSheathConductance = pathway.Gbs,
                Oxygen = AirO2,
                Respiration = pathway.Leaf.RdT
            };

            return func;
        }

        /// <inheritdoc/>
        protected override AssimilationFunction GetAc2Function(AssimilationPathway pathway)
        {
            var x = new double[9];

            x[0] = pathway.Leaf.VcMaxT;
            x[1] = pathway.Leaf.Kc / pathway.Leaf.Ko;
            x[2] = pathway.Leaf.Kc;
            x[3] = 0.0;
            x[4] = pathway.Vpr;
            x[5] = 0.0;
            x[6] = pathway.ChloroplasticCO2 * pathway.Leaf.VcMaxT / (pathway.ChloroplasticCO2 + pathway.Leaf.Kc * (1 + pathway.ChloroplasticO2 / pathway.Leaf.Ko));
            x[7] = 1.0;
            x[8] = 1.0;

            var func = new AssimilationFunction()
            {
                X = x,

                MesophyllRespiration = pathway.Leaf.GmRd,
                HalfRubiscoSpecificityReciprocal = pathway.Leaf.Gamma,
                FractionOfDiffusivitySolubilityRatio = 0.1 / DiffusivitySolubilityRatio,
                BundleSheathConductance = pathway.Gbs,
                Oxygen = AirO2,
                Respiration = pathway.Leaf.RdT
            };

            return func;
        }

        /// <inheritdoc/>
        protected override AssimilationFunction GetAjFunction(AssimilationPathway pathway)
        {
            var x = new double[9];

            x[0] = (1 - MesophyllElectronTransportFraction) * ATPProductionElectronTransportFactor * pathway.Leaf.J / 3.0;
            x[1] = 7.0 / 3.0 * pathway.Leaf.Gamma;
            x[2] = 0.0;
            x[3] = 0.0;
            x[4] = MesophyllElectronTransportFraction * ATPProductionElectronTransportFactor * pathway.Leaf.J / ExtraATPCost;
            x[5] = 0.0;
            x[6] = pathway.ChloroplasticCO2 * (1 - MesophyllElectronTransportFraction) * ATPProductionElectronTransportFactor * pathway.Leaf.J / (3 * pathway.ChloroplasticCO2 + 7 * pathway.Leaf.Gamma * pathway.ChloroplasticO2);
            x[7] = 1.0;
            x[8] = 1.0;

            var func = new AssimilationFunction()
            {
                X = x,

                MesophyllRespiration = pathway.Leaf.GmRd,
                HalfRubiscoSpecificityReciprocal = pathway.Leaf.Gamma,
                FractionOfDiffusivitySolubilityRatio = 0.1 / DiffusivitySolubilityRatio,
                BundleSheathConductance = pathway.Gbs,
                Oxygen = AirO2,
                Respiration = pathway.Leaf.RdT
            };

            return func;
        }
    }
}
