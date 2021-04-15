﻿using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Activities
{
    /// <summary>Mark specified individual ruminants for sale.</summary>
    /// <summary>This activity is in addition to those identified in RuminantActivityManage</summary>
    /// <version>1.0</version>
    /// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity marks the specified individuals for sale by RuminantAcitivtyBuySell.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantMarkForSale.htm")]
    public class RuminantActivityMarkForSale: CLEMRuminantActivityBase
    {
        private LabourRequirement labourRequirement;

        /// <summary>
        /// Sale flag to use
        /// </summary>
        [Description("Sale reason to apply")]
        [System.ComponentModel.DefaultValueAttribute("MarkedSale")]
        [GreaterThanEqualValue(4, ErrorMessage = "A sale reason must be provided")]
        public MarkForSaleReason SaleFlagToUse { get; set; }

        /// <summary>
        /// Overwrite any currently recorded sale flag
        /// </summary>
        [Description("Overwrite existing sale flag")]
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool OverwriteFlag { get; set; }

        private int filterGroupsCount = 0;
        private int numberToTag = 0;
        private bool labourShortfall = false;
        private HerdChangeReason changeReason;

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // get all ui tree herd filters that relate to this activity
            this.InitialiseHerd(true, true);
            changeReason = (HerdChangeReason)SaleFlagToUse;
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            numberToTag = NumberToTag();
            return null;
        }

        private int NumberToTag()
        {
            List<Ruminant> herd = CurrentHerd(false);

            filterGroupsCount = FindAllChildren<RuminantGroup>().Count();
            int number = 0;
            if (filterGroupsCount > 0)
            {
                number = 0;
                foreach (RuminantGroup item in FindAllChildren<RuminantGroup>())
                {
                    number += herd.Filter(item).Where(a => OverwriteFlag || a.SaleFlag == HerdChangeReason.None).Count();
                }
            }
            else
            {
                number = herd.Count();
            }

            return number;
        }

        /// <summary>
        /// Determines how much labour is required from this activity based on the requirement provided
        /// </summary>
        /// <param name="requirement">The details of how labour are to be provided</param>
        /// <returns></returns>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            //double adultEquivalents = herd.Sum(a => a.AdultEquivalent);
            double daysNeeded = 0;
            double numberUnits = 0;
            labourRequirement = requirement;
            switch (requirement.UnitType)
            {
                case LabourUnitType.Fixed:
                    daysNeeded = requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perHead:
                    numberUnits = numberToTag / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                    {
                        numberUnits = Math.Ceiling(numberUnits);
                    }
                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                default:
                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
            }
            return new GetDaysLabourRequiredReturnArgs(daysNeeded, "Mark", this.PredictedHerdName);
        }

        /// <summary>
        /// The method allows the activity to adjust resources requested based on shortfalls (e.g. labour) before they are taken from the pools
        /// </summary>
        public override void AdjustResourcesNeededForActivity()
        {
            labourShortfall = false;
            if (LabourLimitProportion < 1 & (labourRequirement != null && labourRequirement.LabourShortfallAffectsActivity))
            {
                switch (labourRequirement.UnitType)
                {
                    case LabourUnitType.Fixed:
                    case LabourUnitType.perHead:
                        numberToTag = Convert.ToInt32(numberToTag * LabourLimitProportion, CultureInfo.InvariantCulture);
                        labourShortfall = true;
                        break;
                    default:
                        throw new ApsimXException(this, "Labour requirement type " + labourRequirement.UnitType.ToString() + " is not supported in DoActivity method of [a=" + this.Name + "]");
                }
            }
            return;
        }

        /// <summary>An event handler to call for changing stocking based on prediced pasture biomass</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalMark")]
        private void OnCLEMAnimalMark(object sender, EventArgs e)
        {
            if (this.TimingOK)
            {
                // recluculate numbers and ensure it is not less than number calculated
                int updatedNumberToTag = NumberToTag(); 
                if (updatedNumberToTag < numberToTag)
                {
                    numberToTag = updatedNumberToTag;
                }

                List<Ruminant> herd = CurrentHerd(false);
                if (numberToTag > 0)
                {
                    foreach (RuminantGroup item in FindAllChildren<RuminantGroup>())
                    {
                        foreach (Ruminant ind in herd.Filter(item).Where(a => OverwriteFlag || a.SaleFlag == HerdChangeReason.None).Take(numberToTag))
                        {
                            this.Status = (labourShortfall)?ActivityStatus.Partial:ActivityStatus.Success;
                            ind.SaleFlag = changeReason;
                            numberToTag--;
                        }
                    }
                    if(filterGroupsCount == 0)
                    {
                        foreach (Ruminant ind in herd.Where(a => OverwriteFlag || a.SaleFlag == HerdChangeReason.None).Take(numberToTag))
                        {
                            this.Status = (labourShortfall) ? ActivityStatus.Partial : ActivityStatus.Success;
                            ind.SaleFlag = changeReason;
                            numberToTag--;
                        }
                    }
                }
                else
                {
                    this.Status = ActivityStatus.NotNeeded;
                }
            }
            else
            {
                this.Status = ActivityStatus.Ignored;
            }
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            // nothing to do. This is performed in the AnimalMark event.
        }

        /// <summary>
        /// Method to determine resources required for initialisation of this activity
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForinitialisation()
        {
            return null;
        }

        /// <summary>
        /// Resource shortfall event handler
        /// </summary>
        public override event EventHandler ResourceShortfallOccurred;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShortfallOccurred(EventArgs e)
        {
            ResourceShortfallOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// Resource shortfall occured event handler
        /// </summary>
        public override event EventHandler ActivityPerformed;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }

        #region descriptive summary

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            return $"\r\n<div class=\"activityentry\">Flag individuals for sale as [{SaleFlagToUse}] in the following groups:</div>";
        } 
        #endregion
    }
}
