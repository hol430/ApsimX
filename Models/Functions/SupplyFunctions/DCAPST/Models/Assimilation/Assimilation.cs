using System;
using System.Collections.Generic;
using System.Linq;

using Models.Core;

namespace Models.Functions.SupplyFunctions.DCAPST
{
    /// <summary>
    /// Tracks the state of an assimilation type
    /// </summary>
    public abstract class Assimilation : Model, IAssimilation
    {
        /// <inheritdoc/>
        [Description("Partial pressure of O2 in air")]
        [Units("μbar")]
        public double AirO2 { get; set; }

        /// <inheritdoc/>
        [Description("Partial pressure of CO2 in air")]
        [Units("μbar")]
        public double AirCO2 { get; set; }

        /// <inheritdoc/>
        [Description("Ratio of intercellular CO2 to air CO2")]
        [Units("")]
        public double IntercellularToAirCO2Ratio { get; set; }

        /// <inheritdoc/>
        [Description("Diffusivity solubility ratio")]
        [Units("")]
        public double DiffusivitySolubilityRatio { get; set; }

        /// <inheritdoc/>
        [Description("PEP regeneration")]
        [Units("")]
        public double PEPRegeneration { get; set; }

        /// <inheritdoc/>
        [Description("Bundle sheath conductance")]
        [Units("")]
        public double BundleSheathConductance { get; set; }

        /// <summary>
        /// Factory method for accessing the different possible terms for assimilation
        /// </summary>
        public AssimilationFunction GetFunction(AssimilationPathway pathway, TemperatureResponse leaf)
        {
            if (pathway.Type == PathwayType.Ac1) return GetAc1Function(pathway, leaf);
            else if (pathway.Type == PathwayType.Ac2) return GetAc2Function(pathway, leaf);
            else return GetAjFunction(pathway, leaf);
        }        

        /// <summary>
        /// Updates the intercellular CO2 parameter
        /// </summary>
        public virtual void UpdateIntercellularCO2(AssimilationPathway pathway, double gt, double waterUseMolsSecond) 
        { /*C4 & CCM overwrite this.*/ }

        /// <summary>
        /// 
        /// </summary>
        public void UpdatePartialPressures(AssimilationPathway pathway, TemperatureResponse leaf, AssimilationFunction function)
        {
            UpdateMesophyllCO2(pathway, leaf);
            UpdateChloroplasticO2(pathway);
            UpdateChloroplasticCO2(pathway, function);
        }

        /// <summary>
        /// Updates the mesophyll CO2 parameter
        /// </summary>
        protected virtual void UpdateMesophyllCO2(AssimilationPathway pathway, TemperatureResponse leaf) 
        { /*C4 & CCM overwrite this.*/ }

        /// <summary>
        /// Updates the chloroplastic O2 parameter
        /// </summary>
        protected virtual void UpdateChloroplasticO2(AssimilationPathway pathway) 
        { /*CCM overwrites this.*/ }

        /// <summary>
        /// Updates the chloroplastic CO2 parameter
        /// </summary>
        protected virtual void UpdateChloroplasticCO2(AssimilationPathway pathway, AssimilationFunction func) 
        { /*CCM overwrites this.*/ }

        /// <summary>
        /// Retrieves a function describing assimilation along the Ac1 pathway
        /// </summary>
        protected abstract AssimilationFunction GetAc1Function(AssimilationPathway pathway, TemperatureResponse leaf);

        /// <summary>
        /// Retrieves a function describing assimilation along the Ac2 pathway
        /// </summary>
        protected abstract AssimilationFunction GetAc2Function(AssimilationPathway pathway, TemperatureResponse leaf);

        /// <summary>
        /// Retrieves a function describing assimilation along the Aj pathway
        /// </summary>
        protected abstract AssimilationFunction GetAjFunction(AssimilationPathway pathway, TemperatureResponse leaf);
    }
}
