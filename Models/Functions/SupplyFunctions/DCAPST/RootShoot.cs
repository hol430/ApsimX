using System;
using System.Linq;

using Models.Core;
using Models.PMF.Phen;

namespace Models.Functions.SupplyFunctions.DCAPST
{
    // TODO: Implement IFunction? It matches the pattern

    /// <summary>
    /// Provides the root-shoot ratio through the UI
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(DCAPSTModel))]
    public class RootShoot : Model, IFunction
    {
        [Link]
        Phenology phenology = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arrayIndex"></param>
        /// <returns></returns>
        public double Value(int arrayIndex = -1)
        {
            switch (phenology.CurrentPhase.Name)
            {
                case ("Emerging"):
                    return Emerging;

                case ("Juvenile"):
                    return Juvenile;

                case ("JuvenileToFloralInit"):
                    return Juvenile;

                case ("FloralInitToFlagLeaf"):
                    return FloralInitialisation;

                case ("FlagLeafToFlowering"):
                    return LeafFlag;

                case ("FloweringToGrainFilling"):
                    return Flowering;

                default:
                    return 0;
            }
        }

        /// <summary>
        /// The emerging ratio
        /// </summary>
        [Description("Emerging ratio")]
        public double Emerging { get; set; }

        /// <summary>
        /// The juvenile ratio
        /// </summary>
        [Description("Juvenile ratio")]
        public double Juvenile { get; set; }

        /// <summary>
        /// The floral initialisation ratio
        /// </summary>
        [Description("Floral initialisation ratio")]
        public double FloralInitialisation { get; set; }

        /// <summary>
        /// The leaf flag ratio
        /// </summary>
        [Description("Leaf flag ratio")]
        public double LeafFlag { get; set; }

        /// <summary>
        /// The flowering ratio
        /// </summary>
        [Description("Flowering ratio")]
        public double Flowering { get; set; }        
    }
}
