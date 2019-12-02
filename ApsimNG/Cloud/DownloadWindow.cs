using System;
using System.Collections.Generic;
using UserInterface.Interfaces;
using Utility;
using Gtk;
using ApsimNG.Cloud.Azure;
using UserInterface.Views;
using UserInterface.EventArguments;

namespace ApsimNG.Cloud
{
    /// <summary>
    /// View to ask user for some options regarding the download of files from a cloud platform.
    /// Once the user has chosen their options and pressed download, this class will pass the user's
    /// preferences into its presenter's DownloadResults method.
    /// </summary>
    class DownloadWindow : ViewBase
    {
        /// <summary>
        /// Whether 'debug' (.stdout) files should be downloaded.
        /// </summary>
        private CheckButton includeDebugFiles;

        /// <summary>
        /// Wether results should be unzipped.
        /// </summary>
        private CheckButton unzipResults;

        /// <summary>
        /// Whether results should be saved after being combined into a .csv file.
        /// </summary>
        private CheckButton keepRawOutputs;

        /// <summary>
        /// Whether the results should be combined into a .csv file.
        /// </summary>
        private CheckButton generateCsv;

        /// <summary>
        /// Whether results should be downloaded.
        /// </summary>
        private CheckButton chkDownloadResults;

        /// <summary>
        /// Button to initiate the download.
        /// </summary>
        private Button btnDownload;

        /// <summary>
        /// Button to change the download directory.
        /// </summary>
        private Button btnChangeOutputDir;

        /// <summary>
        /// Input field to show/edit the download directory.
        /// </summary>
        private Entry entryOutputDir;

        /// <summary>
        /// Primary container, which holds all other controls in the window.
        /// </summary>
        private VBox vboxPrimary;

        /// <summary>
        /// Window which holds the primary vbox.
        /// </summary>
        private Window window;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public DownloadWindow(ViewBase owner) : base(owner)
        {
            Initialise();
        }

        /// <summary>
        /// Adds the controls (buttons, checkbuttons, progress bars etc.) to the window.
        /// </summary>
        private void Initialise()
        {
            vboxPrimary = new VBox();
            HBox downloadDirectoryContainer = new HBox();

            // Checkbox initialisation
            includeDebugFiles = new CheckButton("Include Debugging Files");

            chkDownloadResults = new CheckButton("Download results")
            {
                Active = true,
                TooltipText = "Results will be downloaded if and only if this option is enabled."
            };
            chkDownloadResults.Toggled += DownloadResultsToggle;

            unzipResults = new CheckButton("Unzip results")
            {
                Active = true,
                TooltipText = "Check this option to automatically unzip the results."
            };
            unzipResults.Toggled += UnzipToggle;

            generateCsv = new CheckButton("Collate Results")
            {
                Active = true,
                TooltipText = "Check this option to automatically combine results into a CSV file."
            };
            generateCsv.Toggled += GenerateCsvToggle;

            keepRawOutputs = new CheckButton("Keep raw output files")
            {
                Active = true,
                TooltipText = "By default, the raw output files are deleted after being combined into a CSV. Check this option to keep the raw outputs."
            };
            unzipResults.Active = false;

            // Button initialisation
            btnDownload = new Button("Download");
            btnDownload.Clicked += OnDownloadClicked;

            btnChangeOutputDir = new Button("...");
            btnChangeOutputDir.Clicked += ChangeOutputDir;

            entryOutputDir = new Entry((string)AzureSettings.Default["OutputDir"]) { Sensitive = false };
            entryOutputDir.WidthChars = entryOutputDir.Text.Length;

            downloadDirectoryContainer.PackStart(new Label("Output Directory: "), false, false, 0);
            downloadDirectoryContainer.PackStart(entryOutputDir, true, true, 0);
            downloadDirectoryContainer.PackStart(btnChangeOutputDir, false, false, 0);
            
            // Put all form controls into the primary vbox
            vboxPrimary.PackStart(includeDebugFiles);
            vboxPrimary.PackStart(chkDownloadResults);
            vboxPrimary.PackStart(unzipResults);
            vboxPrimary.PackStart(generateCsv);
            vboxPrimary.PackStart(keepRawOutputs);
            vboxPrimary.PackStart(downloadDirectoryContainer);            

            // This empty label will put a gap between the controls above it and below it.
            vboxPrimary.PackStart(new Label(""));

            vboxPrimary.PackEnd(btnDownload, false, false, 0);

            Frame primaryContainer = new Frame("Download Settings");
            primaryContainer.Add(vboxPrimary);

            window = new Window("Download cloud jobs");

            window.Add(primaryContainer);
            window.ShowAll();
        }

        public event AsyncEventHandler Download;

        /// <summary>
        /// Export results to a .csv file?
        /// </summary>
        public bool ExportToCsv
        {
            get
            {
                return generateCsv.Active;
            }
        }

        /// <summary>
        /// Download debug (.stdout) files?
        /// </summary>
        public bool IncludeDebugFiles
        {
            get
            {
                return includeDebugFiles.Active;
            }
        }

        /// <summary>
        /// Extract results after downloading?
        /// </summary>
        public bool ExtractResults
        {
            get
            {
                return unzipResults.Active;
            }
        }

        /// <summary>
        /// Downloads the currently selected jobs, taking into account the settings.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private async void OnDownloadClicked(object sender, EventArgs args)
        {
            try
            {
                btnDownload.Clicked -= OnDownloadClicked;
                btnChangeOutputDir.Clicked -= ChangeOutputDir;

                await Download?.Invoke(this, EventArgs.Empty);

                window.Destroy();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Opens a GUI asking the user for a default download directory, and saves their choice.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeOutputDir(object sender, EventArgs e)
        {
            try
            {
                IFileDialog fileChooser = new FileDialog()
                {
                    Action = FileDialog.FileActionType.SelectFolder,
                    Prompt = "Choose a download folder"
                };
                string downloadDirectory = fileChooser.GetFile();
                if (!string.IsNullOrEmpty(downloadDirectory))
                {
                    entryOutputDir.Text = downloadDirectory;
                    AzureSettings.Default["OutputDir"] = downloadDirectory;
                    AzureSettings.Default.Save();
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
        
        /// <summary>
        /// Event handler for toggling the generate CSV checkbox. Disables the keep raw outputs checkbox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GenerateCsvToggle(object sender, EventArgs e)
        {
            try
            {
                keepRawOutputs.Sensitive = !keepRawOutputs.Sensitive;
                if (!keepRawOutputs.Sensitive)
                    keepRawOutputs.Active = false;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Event handler for toggling the unzip results checkbox. Disables the generate CSV checkbox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UnzipToggle(object sender, EventArgs e)
        {
            try
            {
                generateCsv.Sensitive = !generateCsv.Sensitive;
                if (!generateCsv.Sensitive)
                    generateCsv.Active = false;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Event handler for toggling the download results checkbox. Disables the unzip results checkbox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DownloadResultsToggle(object sender, EventArgs e)
        {
            try
            {
                unzipResults.Sensitive = !unzipResults.Sensitive;
                if (!unzipResults.Sensitive)
                    unzipResults.Active = false;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}
