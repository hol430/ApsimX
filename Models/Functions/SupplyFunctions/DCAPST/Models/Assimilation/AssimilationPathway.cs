using System;
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
    [ValidParent(ParentType = typeof(IAssimilation))]
    public class AssimilationPathway : Model
    {
        /// <summary>
        /// The parameters describing the canopy
        /// </summary>
        [Link]
        protected ICanopyParameters Canopy;

        /// <summary>
        /// The parameters describing the pathways
        /// </summary>
        [Link]
        protected IPathwayParameters Pathway;

        /// <summary>
        /// The current pathway type
        /// </summary>
        [Description("Pathway type")]
        public PathwayType Type { get; set; }

        /// <summary>
        /// Models how the leaf responds to different temperatures
        /// </summary>
        [Link(Type = LinkType.Child)]
        public LeafTemperatureResponseModel Leaf { get; set; }

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
