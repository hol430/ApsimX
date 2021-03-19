#if NETCOREAPP
using System;
using System.Collections.Generic;
using UserInterface.Classes;
using UserInterface.EventArguments;
using UserInterface.Interfaces;
using Gtk;
using APSIM.Shared.Utilities;
using System.Linq;
using System.Globalization;
using System.Reflection;

namespace UserInterface.Views
{
    /// <summary>
    /// The gtk3 upgrade UI. Allows a user to upgrade to
    /// a newer (or older) version of APSIM.
    /// </summary>
    public class UpgradeView : IUpgradeView
    {
        private enum Columns
        {
            ReleaseDate,
            IssueNumber,
            IssueTitle,
            IssueURL,
            ReleaseURL,
            VersionNumber
        }

        /// <summary>
        /// The main window holding the controls.
        /// </summary>
        private Window window;

        /// <summary>
        /// The main stack widget.
        /// </summary>
        private Stack mainStack;

        /// <summary>
        /// Parent widget for the page0 (license info) area.
        /// </summary>
        private Widget page0;

        /// <summary>
        /// The textview widget which displays the license.
        /// </summary>
        private TextView licenseTextView;

        /// <summary>
        /// The markdown view which displays the license in <see cref="licenseTextView" />.
        /// </summary>
        private MarkdownView licenseView;

        /// <summary>
        /// The 'I agree to the terms...' check button.
        /// </summary>
        private CheckButton chkLicenseAgree;

        /// <summary>
        /// Main widget for the second page (user details).
        /// </summary>
        private Widget page1;

        /// <summary>
        /// The first name text input widget.
        /// </summary>
        private Entry entryFirstName;

        /// <summary>
        /// The last name text input widget.
        /// </summary>
        private Entry entryLastName;

        /// <summary>
        /// The email text input widget.
        /// </summary>
        private Entry entryEmail;

        /// <summary>
        /// The organisation text input widget.
        /// </summary>
        private Entry entryOrganisation;

        /// <summary>
        /// The countries combo box.
        /// </summary>
        private ComboBox comboCountries;

        /// <summary>
        /// Main widget for the third page (upgrade selection).
        /// </summary>
        private Widget page2;

        /// <summary>
        /// The version info label ('you are currently on version x...').
        /// </summary>
        private Label lblVersionInfo;

        /// <summary>
        /// The tree control which displays the list of upgrades.
        /// </summary>
        private Gtk.TreeView upgradesTree;

        /// <summary>
        /// The previous button - takes the user to the previous part of the upgrade process.
        /// </summary>
        private Button btnPrevious;

        /// <summary>
        /// The next button - takes the user to the next part of the upgrade process.
        /// </summary>
        private Button btnNext;


        /// <summary>
        /// Called when the user wants is ready to perform an upgrade.
        /// </summary>
        public event EventHandler<UpgradeArgs> DoUpgrade;

        /// <summary>
        /// Called when the user wants to view detailed info about an upgrade.
        /// </summary>
        public event EventHandler<UpgradeArgs> ViewDetails;

        /// <summary>
        /// First name as input by the user.
        /// </summary>
        public string FirstName
        {
            get => entryFirstName.Text;
            set => entryFirstName.Text = value;
        }

        /// <summary>
        /// Last name as input by the user.
        /// </summary>
        public string LastName
        {
            get => entryLastName.Text;
            set => entryLastName.Text = value;
        }

        /// <summary>
        /// Email address as input by the user.
        /// </summary>
        public string Email
        {
            get => entryEmail.Text;
            set => entryEmail.Text = value;
        }

        /// <summary>
        /// Organisation as input by the user.
        /// </summary>
        public string Organisation
        {
            get => entryOrganisation.Text;
            set => entryOrganisation.Text = value;
        }

        /// <summary>
        /// Country name selected by user.
        /// </summary>
        public string Country
        {
            get
            {
                int index = comboCountries.Active;
                if (index >= 0 && index < Constants.Countries.Length)
                    return Constants.Countries[index];
                return null;
            }
            set
            {
                int index = Array.IndexOf(Constants.Countries, value);
                if (index >= 0)
                    comboCountries.Active = index;
            }
        }

        public PersonalDetails Details
        {
            get
            {
                return new PersonalDetails(FirstName, LastName, Email, Country, Organisation);
            }
            set
            {
                FirstName = value.FirstName;
                LastName = value.LastName;
                Email = value.EmailAddress;
                Country = value.Country;
                Organisation = value.Organisation;
            }
        }

        /// <summary>
        /// Initialises the view but does not show it.
        /// </summary>
        public UpgradeView()
        {
            var builder = new Builder("ApsimNG.Resources.Glade.UpgradeView.Gtk3.glade");
            window = (Window)builder.GetObject("window");
            mainStack = (Stack)builder.GetObject("mainStack");
            page0 = (Widget)builder.GetObject("page0");
            licenseTextView = (TextView)builder.GetObject("licenseView");
            chkLicenseAgree = (CheckButton)builder.GetObject("chkLicenseAgree");
            page1 = (Widget)builder.GetObject("page1");
            entryFirstName = (Entry)builder.GetObject("entryFirstName");
            entryLastName = (Entry)builder.GetObject("entryLastName");
            entryEmail = (Entry)builder.GetObject("entryEmail");
            entryOrganisation = (Entry)builder.GetObject("entryOrganisation");
            comboCountries = (ComboBox)builder.GetObject("comboCountries");
            page2 = (Widget)builder.GetObject("page2");
            lblVersionInfo = (Label)builder.GetObject("lblVersionInfo");
            upgradesTree = (Gtk.TreeView)builder.GetObject("upgradesTree");
            btnNext = (Button)builder.GetObject("btnNext");
            btnPrevious = (Button)builder.GetObject("btnPrevious");

            licenseView = new MarkdownView(ViewBase.MasterView as ViewBase, licenseTextView);
            licenseView.Text = ReflectionUtilities.GetResourceAsString("ApsimNG.LICENSE.md");

            // Populate the countries combo box with a list of valid country names.
            ListStore countries = new ListStore(typeof(string));
            foreach (string country in Constants.Countries)
                countries.AppendValues(country);
            comboCountries.Model = countries;

            // Add a cell renderer to the combo box.
            CellRendererText cell = new CellRendererText();
            comboCountries.PackStart(cell, false);
            comboCountries.AddAttribute(cell, "text", 0);

            upgradesTree.AppendColumn(new TreeViewColumn("Version", new CellRendererText(), "text", (int)Columns.VersionNumber));
            upgradesTree.AppendColumn(new TreeViewColumn("Description", new CellRendererText(), "text", (int)Columns.IssueTitle));
            upgradesTree.PopupMenu += OnPopup;
            upgradesTree.ButtonPressEvent += OnUpgradesTreeButtonPress;

            window.DefaultWidth = 1024;
            window.DefaultHeight = 768;

            btnNext.Clicked += OnNextClicked;
            btnPrevious.Clicked += OnPreviousClicked;
        }

        [GLib.ConnectBefore]
        private void OnUpgradesTreeButtonPress(object o, ButtonPressEventArgs args)
        {
            try
            {
                // If we ever move to Gtk >= 3.4 we can update this to
                // if (args.Event.TriggersContextMenu())
                if (args.Event.Button == 3 && args.Event.Type == Gdk.EventType.ButtonPress)
                    OnPopup(o, null);
            }
            catch (Exception err)
            {
                ViewBase.MasterView.ShowError(err);
            }
        }

        private void OnPopup(object sender, PopupMenuArgs args)
        {
            try
            {
                if (sender is Widget tree)
                {
                    Menu contextMenu = new Menu();
                    MenuItem item = new MenuItem("View Details");
                    item.Activated += OnGetInfo;
                    contextMenu.Append(item);
                    contextMenu.ShowAll();
                    contextMenu.AttachToWidget(tree, DetachMenu);
                    contextMenu.Popup();
                }
            }
            catch (Exception err)
            {
                ViewBase.MasterView.ShowError(err);
            }
        }

        private void DetachMenu(Widget attach_widget, Menu menu)
        {
            menu.Detach();
        }

        private void OnGetInfo(object sender, EventArgs e)
        {
            try
            {
                ViewDetails?.Invoke(this, new UpgradeArgs(SelectedUpgrade()));
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Populate the view with a list of upgrades.
        /// </summary>
        /// <param name="upgrades">Available upgrades.</param>
        public void Populate(IEnumerable<Upgrade> upgrades)
        {
            Application.Invoke((_, __) =>
            {
                // Columns are date, issue #, issue title, issue URL, release URL, version #
                ListStore model = new ListStore(Enumerable.Repeat(typeof(string), 6).ToArray());
                foreach (Upgrade upgrade in upgrades)
                {
                    string versionNumber = $"{upgrade.ReleaseDate:yyyy.MM.dd}.{upgrade.IssueNumber}";
                    model.AppendValues(upgrade.ReleaseDate.ToString("yyyy-MM-dd", CultureInfo.CurrentCulture),
                                    upgrade.IssueNumber,
                                    upgrade.IssueTitle,
                                    upgrade.IssueURL,
                                    upgrade.ReleaseURL,
                                    versionNumber);
                }
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                lblVersionInfo.Text = $"You are currently using version {version}. Newer versions are listed below.";
                if (upgradesTree.Model is IDisposable disposable)
                    disposable.Dispose();
                upgradesTree.Model = model;
            });
        }

        /// <summary>
        /// Show the view.
        /// </summary>
        public void Show()
        {
            window.ShowAll();
        }

        /// <summary>
        /// Returns the currently selected upgrade, or null if
        /// the tree control is not visible or if no upgrade is
        /// currently selected.
        /// </summary>
        private Upgrade SelectedUpgrade()
        {
            if (mainStack.VisibleChild != page2 || upgradesTree.Model == null)
                return null;
            upgradesTree.GetCursor(out TreePath path, out _);
            if (path != null && upgradesTree.Model.GetIter(out TreeIter iter, path))
                return GetUpgrade(iter);
            return null;
        }

        /// <summary>
        /// Return the upgrade object stored in the tree model at the
        /// given location.
        /// </summary>
        /// <param name="iter">A TreeIter instance (the location).</param>
        private Upgrade GetUpgrade(TreeIter iter)
        {
            if (iter.Equals(TreeIter.Zero))
                return null;
            string dateString = (string)upgradesTree.Model.GetValue(iter, (int)Columns.ReleaseDate);
            string issueNumber = (string)upgradesTree.Model.GetValue(iter, (int)Columns.IssueNumber);
            string issueTitle = (string)upgradesTree.Model.GetValue(iter, (int)Columns.IssueTitle);
            string issueUrl = (string)upgradesTree.Model.GetValue(iter, (int)Columns.IssueURL);
            string releaseUrl = (string)upgradesTree.Model.GetValue(iter, (int)Columns.ReleaseURL);
            DateTime date = DateTime.ParseExact(dateString, "yyyy-MM-dd", CultureInfo.CurrentCulture);
            return new Upgrade()
            {
                ReleaseDate = date,
                IssueNumber = int.Parse(issueNumber, CultureInfo.CurrentCulture),
                IssueTitle = issueTitle,
                IssueURL = issueUrl,
                ReleaseURL = releaseUrl
            };
        }

        /// <summary>
        /// Display an error message to the user.
        /// </summary>
        /// <param name="error">Error message to be displayed.</param>
        private void ShowError(Exception error)
        {
            ViewBase.MasterView.ShowError(error);
            ShowMessage(error.Message);
        }

        /// <summary>
        /// Show a message to the user 
        /// </summary>
        /// <param name="message">Message to be displayed.</param>
        private void ShowMessage(string message)
        {
            DialogFlags flags = DialogFlags.DestroyWithParent | DialogFlags.Modal;
            object[] buttonData = new object[] { "OK", ResponseType.Ok };
            using (Dialog dialog = new Dialog("Error", window, flags, buttonData))
            {
                dialog.ContentArea.PackStart(new Label(message), true, true, 5);
                dialog.ContentArea.Margin = 10;
                dialog.ShowAll();
                dialog.Run();
            }
        }

        /// <summary>
        /// Called when the next button is clicked.
        /// Takes the user to the next part of the upgrade process.
        /// </summary>
        /// <param name="sender">Sending object.</param>
        /// <param name="args">Event data.</param>
        private void OnNextClicked(object sender, EventArgs args)
        {
            try
            {
                Widget currentWidget = mainStack.VisibleChild;
                if (currentWidget == page0)
                {
                    if (chkLicenseAgree.Active)
                        mainStack.VisibleChild = page1;
                    else
                        throw new Exception("You must agree to the license terms before upgrading");
                }
                else if (currentWidget == page1)
                {
                    Details.Validate();
                    mainStack.VisibleChild = page2;
                    btnNext.Label = "Upgrade";
                }
                else if (currentWidget == page2)
                {
                    Upgrade selection = SelectedUpgrade();
                    upgradesTree.GetCursor(out TreePath path, out _);
                    if (selection != null && path != null && upgradesTree.Model.GetIter(out TreeIter iter, path))
                    {
                        string version = (string)upgradesTree.Model.GetValue(iter, (int)Columns.VersionNumber);
                        string question = $"Are you sure you want to upgrade to version {version}?";
                        var resp = (ResponseType)ViewBase.MasterView.ShowMsgDialog(question, "Confirm Upgrade", MessageType.Question, ButtonsType.YesNo, window);
                        if (resp == ResponseType.Yes)
                            DoUpgrade?.Invoke(this, new UpgradeArgs(selection));
                    }
                    else
                        ShowMessage("Please select an upgrade");
                }
                else
                    throw new Exception("should never happen");
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Called when the previous button is clicked.
        /// Takes the user to the previous part of the upgrade process.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnPreviousClicked(object sender, EventArgs args)
        {
            try
            {
                Widget page = mainStack.VisibleChild;
                if (page == page2)
                {
                    mainStack.VisibleChild = page1;
                    btnNext.Label = "Next";
                }
                else if (page == page1)
                    mainStack.VisibleChild = page0;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}
#endif