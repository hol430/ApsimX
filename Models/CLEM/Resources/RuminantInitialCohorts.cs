﻿using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Models.CLEM.Activities;
using Models.Core;
using Models.Core.Attributes;
using Models.CLEM.Interfaces;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Holder for all initial ruminant cohorts
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyMultiModelView")]
    [PresenterName("UserInterface.Presenters.PropertyMultiModelPresenter")]
    [ValidParent(ParentType = typeof(RuminantType))]
    [Description("This holds the list of initial cohorts for a given (parent) ruminant herd or type.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantInitialCohorts.htm")]
    public class RuminantInitialCohorts : CLEMModel
    {
        /// <summary>
        /// Records if a warning about set weight occurred
        /// </summary>
        public bool WeightWarningOccurred = false;

        /// <summary>
        /// Constructor
        /// </summary>
        protected RuminantInitialCohorts()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResourceLevel2;
        }

        /// <summary>
        /// Create the individual ruminant animals for this Ruminant Type (Breed)
        /// </summary>
        /// <returns>A list of ruminants</returns>
        public List<Ruminant> CreateIndividuals()
        {
            List<ISetRuminantAttribute> initialCohortAttributes = this.FindAllChildren<ISetRuminantAttribute>().ToList();
            List<Ruminant> individuals = new List<Ruminant>();
            foreach (RuminantTypeCohort cohort in this.FindAllChildren<RuminantTypeCohort>())
            {
                individuals.AddRange(cohort.CreateIndividuals(initialCohortAttributes));
            }
            return individuals;
        }

        #region descriptive summary

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerClosingTags(bool formatForParentControl)
        {
            string html = "</table>";
            if(WeightWarningOccurred)
            {
                html += "</br><span class=\"errorlink\">Warning: Initial weight differs from the expected normalised weight by more than 20%</span>";
            }
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerOpeningTags(bool formatForParentControl)
        {
            WeightWarningOccurred = false;
            return "<table><tr><th>Name</th><th>Gender</th><th>Age</th><th>Weight</th><th>Norm.Wt.</th><th>Number</th><th>Suckling</th><th>Sire</th></tr>";
        }

        #endregion
    }
}



