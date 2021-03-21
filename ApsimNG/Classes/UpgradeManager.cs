#if NETCOREAPP
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
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
            string version = $"{args.Version.ReleaseDate:yyyy.MM.dd}.{args.Version.IssueNumber}";
            Upgrade upgrade = args.Version;
            try
            {
                WriteUpgradeRegistration(version);
            }
            catch (Exception err)
            {
                throw new Exception("Encountered an error while updating registration information. Please try again later.", err);
            }

            WebClient web = new WebClient();

            string tempSetupFileName = Path.Combine(Path.GetTempPath(), "APSIMSetup.exe");

            string sourceURL;
            if (ProcessUtilities.CurrentOS.IsMac)
            {
                sourceURL = Path.ChangeExtension(upgrade.ReleaseURL, "dmg");
                tempSetupFileName = Path.ChangeExtension(tempSetupFileName, "dmg");
            }
            else if (ProcessUtilities.CurrentOS.IsLinux)
            {
                sourceURL = System.IO.Path.ChangeExtension(upgrade.ReleaseURL, "deb");
                tempSetupFileName = System.IO.Path.ChangeExtension(tempSetupFileName, "deb");
            }
            else
                sourceURL = upgrade.ReleaseURL;

            if (File.Exists(tempSetupFileName))
                File.Delete(tempSetupFileName);

            try
            {
                var waitDlg = new Gtk.MessageDialog(window1, Gtk.DialogFlags.Modal,
                    Gtk.MessageType.Info, Gtk.ButtonsType.Cancel, "Downloading file. Please wait...");
                waitDlg.Title = "APSIM Upgrade";
                web.DownloadFileCompleted += Web_DownloadFileCompleted;
                web.DownloadProgressChanged += OnDownloadProgressChanged;
                web.DownloadFileAsync(new Uri(sourceURL), tempSetupFileName);
                if (waitDlg.Run() == (int)Gtk.ResponseType.Cancel)
                    web.CancelAsync();
            }
            catch (Exception err)
            {
                ViewBase.MasterView.ShowMsgDialog("Cannot download this release. Error message is: \r\n" + err.Message, "Error", MessageType.Error, ButtonsType.Ok, window1);
            }
            finally
            {
                if (waitDlg != null)
                {
                    web.DownloadProgressChanged -= OnDownloadProgressChanged;
                    waitDlg.Cleanup();
                    waitDlg = null;
                }
                if (window1 != null && window1.GetGdkWindow() != null)
                    window1.GetGdkWindow().Cursor = null;
            }
        }

        /// <summary>
        /// Invoked when the download progress changes.
        /// Updates the progress bar.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            try
            {
                Gtk.Application.Invoke(delegate
                {
                    try
                    {
                        double progress = 100.0 * e.BytesReceived / e.TotalBytesToReceive;
                        waitDlg.Text = string.Format("Downloading file: {0:0.}%. Please wait...", progress);
                    }
                    catch (Exception err)
                    {
                        ShowError(err);
                    }
                });
            }
            catch (Exception err)
            {
                err = new Exception("Error updating download progress", err);
                ShowError(err);
            }
        }

        private void Web_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                if (waitDlg != null)
                {
                    waitDlg.Cleanup();
                    waitDlg = null;
                }
                if (!e.Cancelled && !string.IsNullOrEmpty(tempSetupFileName) && versionNumber != null)
                {
                    try
                    {
                        if (e.Error != null) // On Linux, we get to this point even when errors have occurred
                            throw e.Error;

                        if (File.Exists(tempSetupFileName))
                        {
                            // Copy the separate upgrader executable to the temp directory.
                            string sourceUpgraderFileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Updater.exe");
                            string upgraderFileName = Path.Combine(Path.GetTempPath(), "Updater.exe");

                            // Check to see if upgrader is already running for whatever reason.
                            // Kill them if found.
                            foreach (Process process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(upgraderFileName)))
                                process.Kill();

                            // Delete the old upgrader.
                            if (File.Exists(upgraderFileName))
                                File.Delete(upgraderFileName);
                            // Copy in the new upgrader.
                            File.Copy(sourceUpgraderFileName, upgraderFileName, true);

                            // Run the upgrader.
                            string binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                            string ourDirectory = Path.GetFullPath(Path.Combine(binDirectory, ".."));
                            string newDirectory = Path.GetFullPath(Path.Combine(ourDirectory, "..", "APSIM" + versionNumber));
                            string arguments = StringUtilities.DQuote(ourDirectory) + " " +
                                               StringUtilities.DQuote(newDirectory);

                            ProcessStartInfo info = new ProcessStartInfo();
                            if (ProcessUtilities.CurrentOS.IsMac)
                            {
                                info.FileName = "mono";
                                info.Arguments = upgraderFileName + " " + arguments;
                            }
                            else
                            {
                                info.FileName = upgraderFileName;
                                info.Arguments = arguments;
                            }
                            info.WorkingDirectory = Path.GetTempPath();
                            Process.Start(info);
                            window1.GetGdkWindow().Cursor = null;

                            // Shutdown the user interface
                            window1.Cleanup();
                            tabbedExplorerView.Close();
                        }
                    }
                    catch (Exception err)
                    {
                        window1.GetGdkWindow().Cursor = null;
                        Application.Invoke(delegate
                        {
                            ViewBase.MasterView.ShowMsgDialog(err.Message, "Installation Error", MessageType.Error, ButtonsType.Ok, window1);
                        });
                    }
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Write to the registration database.
        /// </summary>
        private void WriteUpgradeRegistration(string version)
        {
            string url = "https://apsimdev.apsim.info/APSIM.Registration.Service/Registration.svc/AddRegistration";
            PersonalDetails details = view.Details;
            url += $"?firstName={details.FirstName}";

            url = WebUtilities.AddToURL(url, "lastName", details.LastName);
            url = WebUtilities.AddToURL(url, "organisation", details.Organisation);
            url = WebUtilities.AddToURL(url, "country", details.Country);
            url = WebUtilities.AddToURL(url, "email", details.EmailAddress);
            url = WebUtilities.AddToURL(url, "product", "APSIM Next Generation");
            url = WebUtilities.AddToURL(url, "version", version);
            url = WebUtilities.AddToURL(url, "platform", GetPlatform());
            url = WebUtilities.AddToURL(url, "type", "Upgrade");

            try
            {
                WebUtilities.CallRESTService<object>(url);
            }
            catch
            {
                // Retry once.
                WebUtilities.CallRESTService<object>(url);
            }
        }

        /// <summary>
        /// Gets the platform name used when writing to registration database.
        /// </summary>
        private string GetPlatform()
        {
            if (ProcessUtilities.CurrentOS.IsWindows)
                return "Windows";
            else if (ProcessUtilities.CurrentOS.IsMac)
                return "Mac";
            else if (ProcessUtilities.CurrentOS.IsLinux)
                return "Linux";
            return "?";
        }
    }
}
#endif
