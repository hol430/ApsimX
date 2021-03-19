using System;
using System.Collections.Generic;
using UserInterface.Classes;
using UserInterface.EventArguments;

namespace UserInterface.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface IUpgradeView
    {
        /// <summary>
        /// Personal details input by the user.
        /// </summary>
        PersonalDetails Details { get; set; }

        /// <summary>
        /// Populate the view with a list of upgrades.
        /// </summary>
        /// <remarks>
        /// This can be called on a background thread. Implementors
        /// must ensure that this does not cause problems.
        /// </remarks>
        /// <param name="upgrades">Available upgrades.</param>
        void Populate(IEnumerable<Upgrade> upgrades);

        /// <summary>
        /// Called when the user wants is ready to perform an upgrade.
        /// </summary>
        event EventHandler<UpgradeArgs> DoUpgrade;

        /// <summary>
        /// Called when the user wants to view detailed info about an upgrade.
        /// </summary>
        event EventHandler<UpgradeArgs> ViewDetails;

        /// <summary>
        /// Show the view.
        /// </summary>
        void Show();
    }
}