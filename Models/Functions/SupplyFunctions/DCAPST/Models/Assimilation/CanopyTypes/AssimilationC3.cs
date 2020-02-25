﻿using System;
using Models.Core;

namespace Models.Functions.SupplyFunctions.DCAPST
{
    /// <summary>
    /// Defines the pathway functions for a C3 canopy
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ITotalCanopy))]
    public class AssimilationC3 : Assimilation
    {
        /// <inheritdoc/>
        protected override AssimilationFunction GetAc1Function(AssimilationPathway pathway, LeafTemperatureResponseModel leaf)
        {
            var x = new double[9];

            x[0] = leaf.VcMaxT;
            x[1] = leaf.Kc / leaf.Ko;
            x[2] = leaf.Kc;
            x[3] = 0.0;
            x[4] = 0.0;
            x[5] = 0.0;
            x[6] = 0.0;
            x[7] = 0.0;
            x[8] = 0.0;

            var param = new AssimilationFunction()
            {
                X = x,

                MesophyllRespiration = leaf.GmRd,
                HalfRubiscoSpecificityReciprocal = leaf.Gamma,
                FractionOfDiffusivitySolubilityRatio = 0.0,
                BundleSheathConductance = 1.0,
                Oxygen = AirO2,
                Respiration = leaf.RdT
            };

            return param;
        }

        /// <inheritdoc/>
        protected override AssimilationFunction GetAc2Function(AssimilationPathway pathway, LeafTemperatureResponseModel leaf)
        {
            throw new Exception("The C3 model does not use the Ac2 pathway");
        }

        /// <inheritdoc/>
        protected override AssimilationFunction GetAjFunction(AssimilationPathway pathway, LeafTemperatureResponseModel leaf)
        {
            var x = new double[9];

            x[0] = leaf.J / 4.0;
            x[1] = 2.0 * leaf.Gamma;
            x[2] = 0.0;
            x[3] = 0.0;
            x[4] = 0.0;
            x[5] = 0.0;
            x[6] = 0.0;
            x[7] = 0.0;
            x[8] = 0.0;

            var func = new AssimilationFunction()
            {                
                MesophyllRespiration = leaf.GmRd,
                HalfRubiscoSpecificityReciprocal = leaf.Gamma,
                FractionOfDiffusivitySolubilityRatio = 0.0,
                BundleSheathConductance = 1.0,
                Oxygen = AirO2,
                Respiration = leaf.RdT
            };

            return func;
        }
    }
}
