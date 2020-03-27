﻿using System;
using Models.Core;

namespace Models.Functions.SupplyFunctions.DCAPST
{
    /// <summary>
    /// The possible types of assimilation pathways
    /// </summary>
    public enum PathwayType 
    { 
        /// <summary>
        /// Ac1 assimilation pathway
        /// </summary>
        Ac1, 

        /// <summary>
        /// Ac2 assimilation pathway
        /// </summary>
        Ac2, 

        /// <summary>
        /// Aj assimilation pathway
        /// </summary>
        Aj 
    }

    /// <summary>
    /// Models an assimilation pathway
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IAssimilationArea))]
    public class AssimilationPathway : Model
    {
        /// <summary>
        /// The assimilation
        /// </summary>
        [Link]
        IAssimilation Assimilation = null;

        /// <summary>
        /// The part of the canopy this pathway belongs to
        /// </summary>
        [Link(Type = LinkType.Ancestor)]
        IAssimilationArea Area = null;

        /// <summary>
        /// The current pathway type
        /// </summary>
        [Description("Pathway type")]
        public PathwayType Type { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double Temperature { get; set; } = 0;

        /// <summary>
        /// Bundle sheath conductance
        /// </summary>
        public double Gbs => Assimilation.BundleSheathConductance * Area.LAI;

        /// <summary>
        /// PEP regeneration
        /// </summary>
        public double Vpr => Assimilation.PEPRegeneration * Area.LAI;

        /// <summary>
        /// The rate at which CO2 is assimilated
        /// </summary>
        public double CO2Rate { get; set; }

        /// <summary>
        /// The water required to maintain the CO2 rate
        /// </summary>
        public double WaterUse { get; set; }

        /// <summary>
        /// Intercellular airspace CO2 partial pressure (microbar)
        /// </summary>
        public double IntercellularCO2 { get; set; }

        /// <summary>
        /// Mesophyll CO2 partial pressure (microbar)
        /// </summary>
        public double MesophyllCO2 { get; set; }
        
        /// <summary>
        /// Chloroplastic CO2 partial pressure at the site of Rubisco carboxylation (microbar)
        /// </summary>
        public double ChloroplasticCO2 { get; set; }

        /// <summary>
        /// Chloroplastic O2 partial pressure at the site of Rubisco carboxylation (microbar)
        /// </summary>
        public double ChloroplasticO2 { get; set; }
    }
}