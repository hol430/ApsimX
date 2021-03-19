#if NETCOREAPP
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using APSIM.Shared.Utilities;
using UserInterface.Classes;
using UserInterface.EventArguments;
using UserInterface.Interfaces;
using UserInterface.Views;
using Utility;

namespace UserInterface.Presenters
{
    public class UpgradeManager
    {
        private IUpgradeView view = new UpgradeView();

        public UpgradeManager()
        {
            GetUpgrades().ContinueWith(t => 
            {
                view.Populate(t.Result);
                return t.Result;
            });
            view.DoUpgrade += OnUpgrade;
            view.ViewDetails += OnGetInfo;
            view.Details = new PersonalDetails(Configuration.Settings.FirstName,
                                               Configuration.Settings.LastName,
                                               Configuration.Settings.Email,
                                               Configuration.Settings.Country,
                                               Configuration.Settings.Organisation);
        }

        public void Show()
        {
            view.Show();
        }

        /// <summary>
        /// Populate the view with a list of upgrades returned from a task.
        /// </summary>
        /// <param name="task"></param>
        private IEnumerable<Upgrade> PopulateView(Task<IEnumerable<Upgrade>> task)
        {
            view.Populate(task.Result);
            return task.Result;
        }

        /// <summary>
        /// Get the list of available upgrades (asynchronously).
        /// </summary>
        private async Task<IEnumerable<Upgrade>> GetUpgrades()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            //if (oldVersions.Active && allUpgrades.Length < 1)
            //    url = "https://apsimdev.apsim.info/APSIM.Builds.Service/Builds.svc/GetUpgradesSinceIssue?issueID=-1");
            //else if (!oldVersions.Active && upgrades.Length < 1)
            string url = $"https://apsimdev.apsim.info/APSIM.Builds.Service/Builds.svc/GetUpgradesSinceIssue?issueID={version.Revision}";
            return await WebUtilities.CallRESTServiceAsync<Upgrade[]>(url);
        }

        /// <summary>
        /// Called when the user wants to view more info about a specific version.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnGetInfo(object sender, UpgradeArgs args)
        {
            ProcessUtilities.ProcessStart(args.Version.IssueURL);
        }

        /// <summary>
        /// Called when the user wants to upgrade to a specific version.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnUpgrade(object sender, UpgradeArgs args)
        {
            Console.WriteLine($"Upgrading to version {args.Version.ReleaseDate:yyyy.MM.dd}.{args.Version.IssueNumber}");
        }
    }
}
#endif
