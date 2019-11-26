using System;
using Gtk;
using System.IO;
using System.ComponentModel;
using ApsimNG.Cloud;
using ApsimNG.Cloud.Azure;
using ApsimNG.Interfaces;

namespace UserInterface.Views
{   
    public class NewAzureJobView : ViewBase, INewCloudJobView
    {
        private Entry entryName;
        private RadioButton radioApsimDir;
        private RadioButton radioApsimZip;
        private Entry entryApsimDir;
        private Entry entryApsimZip;
        private Button btnApsimDir;
        private Button btnApsimZip;
        private Entry entryOutputDir;
        private ComboBox comboCoreCount;
        private CheckButton chkEmail;
        private Entry entryEmail;
        private CheckButton chkDownload;
        private CheckButton chkSummarise;
        private CheckButton chkSaveModels;
        private Entry entryModelPath;
        private Button btnModelPath;
        private Label lblStatus;
        private Button btnOK;

        public NewAzureJobView(ViewBase owner) : base(owner)
        {
            // this vbox holds both alignment objects (which in turn hold the frames)
            VBox vboxPrimary = new VBox(false, 10);

            // this is the alignment object which holds the azure job frame
            Alignment primaryContainer = new Alignment(0f, 0f, 0f, 0f);
            primaryContainer.LeftPadding = primaryContainer.RightPadding = primaryContainer.TopPadding = primaryContainer.BottomPadding = 5;

            // Azure Job Frame
            Frame frmAzure = new Frame("Azure Job");

            Alignment alignTblAzure = new Alignment(0.5f, 0.5f, 1f, 1f);
            alignTblAzure.LeftPadding = alignTblAzure.RightPadding = alignTblAzure.TopPadding = alignTblAzure.BottomPadding = 5;

            // Azure table - contains all fields in the azure job frame
            Table tblAzure = new Table(4, 2, false);
            tblAzure.RowSpacing = 5;
            // Job Name
            Label lblName = new Label("Job Description/Name:");
            lblName.Xalign = 0;
            lblName.Yalign = 0.5f;

            entryName = new Entry();

            tblAzure.Attach(lblName, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            tblAzure.Attach(entryName, 1, 2, 0, 1, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Fill, 20, 0);

            // Number of cores
            Label lblCores = new Label("Number of CPU cores to use:");
            lblCores.Xalign = 0;
            lblCores.Yalign = 0.5f;

            // use the same core count options as in MARS (16, 32, 48, 64, ... , 128, 256)
            comboCoreCount = ComboBox.NewText();
            for (int i = 16; i <= 128; i += 16) comboCoreCount.AppendText(i.ToString());
            comboCoreCount.AppendText("256");

            comboCoreCount.Active = 0;

            // combo boxes cannot be aligned, so it is placed in an alignment object, which can be aligned
            Alignment comboAlign = new Alignment(0f, 0.5f, 0.25f, 1f);
            comboAlign.Add(comboCoreCount);

            tblAzure.Attach(lblCores, 0, 1, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            tblAzure.Attach(comboAlign, 1, 2, 1, 2, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Fill, 20, 0);

            // User doesn't get to choose a model via the form anymore. It comes from the context of the right click
            
            // Model selection frame
            Frame frmModelSelect = new Frame("Model Selection");

            // Alignment to ensure a 5px border around the inside of the frame
            Alignment alignModel = new Alignment(0f, 0f, 1f, 1f);
            alignModel.LeftPadding = alignModel.RightPadding = alignModel.TopPadding = alignModel.BottomPadding = 5;
            Table tblModel = new Table(2, 3, false);
            tblModel.ColumnSpacing = 5;
            tblModel.RowSpacing = 10;

            chkSaveModels = new CheckButton("Save model files");
            chkSaveModels.Toggled += OnToggleSaveModels;
            entryModelPath = new Entry();
            btnModelPath = new Button("...");
            btnModelPath.Clicked += OnChooseDirectory;
            
            chkSaveModels.Active = true;
            chkSaveModels.Active = false;
            
            HBox hboxModelpath = new HBox();
            hboxModelpath.PackStart(entryModelPath, true, true, 0);
            hboxModelpath.PackStart(btnModelPath, false, false, 5);
            
            tblAzure.Attach(chkSaveModels, 0, 1, 2, 3, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            tblAzure.Attach(hboxModelpath, 1, 2, 2, 3, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Fill, 0, 0);

            //Apsim Version Selection frame/table		
            Frame frmVersion = new Frame("APSIM Next Generation Version Selection");
            Table tblVersion = new Table(2, 3, false);            
            tblVersion.ColumnSpacing = 5;
            tblVersion.RowSpacing = 10;

            // Alignment to ensure a 5px border on the inside of the frame
            Alignment alignVersion = new Alignment(0f, 0f, 1f, 1f);
            alignVersion.LeftPadding = alignVersion.RightPadding = alignVersion.TopPadding = alignVersion.BottomPadding = 5;

            // use Apsim from a directory

            radioApsimDir = new RadioButton("Use APSIM Next Generation from a directory");
            radioApsimDir.Toggled += OnChangeApsimXSource;
            // populate this input field with the directory containing this executable		
            entryApsimDir = new Entry(Directory.GetParent(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)).ToString());
            btnApsimDir = new Button("...");
            btnApsimDir.Clicked += OnChooseDirectory;
            tblVersion.Attach(radioApsimDir, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            tblVersion.Attach(entryApsimDir, 1, 2, 0, 1, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Fill, 0, 0);
            tblVersion.Attach(btnApsimDir, 2, 3, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);

            // use a zipped version of Apsim

            radioApsimZip = new RadioButton(radioApsimDir, "Use a zipped version of APSIM Next Generation");
            radioApsimZip.Toggled += OnChangeApsimXSource;
            entryApsimZip = new Entry();
            btnApsimZip = new Button("...");
            btnApsimZip.Clicked += OnChooseZipFile;

            tblVersion.Attach(radioApsimZip, 0, 1, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            tblVersion.Attach(entryApsimZip, 1, 2, 1, 2, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Fill, 0, 0);
            tblVersion.Attach(btnApsimZip, 2, 3, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);

            alignVersion.Add(tblVersion);
            frmVersion.Add(alignVersion);

            tblAzure.Attach(frmVersion, 0, 2, 3, 4, AttachOptions.Fill, AttachOptions.Fill, 0, 0);

            // toggle the default radio button to ensure appropriate entries/buttons are greyed out by default
            radioApsimDir.Active = true;
            radioApsimZip.Active = true;
            radioApsimDir.Active = true;

            // add azure job table to azure alignment, and add that to the azure job frame
            alignTblAzure.Add(tblAzure);
            frmAzure.Add(alignTblAzure);

            // Results frame
            Frame frameResults = new Frame("Results");
            // Alignment object to ensure a 10px border around the inside of the results frame		
            Alignment alignFrameResults = new Alignment(0f, 0f, 1f, 1f);
            alignFrameResults.LeftPadding = alignFrameResults.RightPadding = alignFrameResults.TopPadding = alignFrameResults.BottomPadding = 10;
            Table tblResults = new Table(4, 3, false);
            tblResults.ColumnSpacing = 5;
            tblResults.RowSpacing = 5;

            // Auto send email
            chkEmail = new CheckButton("Send email  upon completion to:");
            entryEmail = new Entry();

            tblResults.Attach(chkEmail, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            tblResults.Attach(entryEmail, 1, 2, 0, 1, (AttachOptions.Expand | AttachOptions.Fill), AttachOptions.Fill, 0, 0);

            // Auto download results
            chkDownload = new CheckButton("Automatically download results once complete");
            chkSummarise = new CheckButton("Summarise Results");

            tblResults.Attach(chkDownload, 0, 1, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            tblResults.Attach(chkSummarise, 1, 3, 1, 2, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Fill, 0, 0);

            // Output dir

            Label lblOutputDir = new Label("Output Directory:");
            lblOutputDir.Xalign = 0;
            entryOutputDir = new Entry((string)AzureSettings.Default["OutputDir"]);

            Button btnOutputDir = new Button("...");
            btnOutputDir.Clicked += OnChooseDirectory;

            tblResults.Attach(lblOutputDir, 0, 1, 2, 3, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            tblResults.Attach(entryOutputDir, 1, 2, 2, 3, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            tblResults.Attach(btnOutputDir, 2, 3, 2, 3, AttachOptions.Fill, AttachOptions.Fill, 0, 0);

            Alignment alignNameTip = new Alignment(0f, 0f, 1f, 1f);
            Label lblNameTip = new Label("(note: if you close Apsim before the job completes, the results will not be automatically downloaded)");
            alignNameTip.Add(lblNameTip);

            tblResults.Attach(alignNameTip, 0, 3, 3, 4, AttachOptions.Fill, AttachOptions.Fill, 0, 0);

            alignFrameResults.Add(tblResults);
            frameResults.Add(alignFrameResults);

            // OK/Cancel buttons
            
            btnOK = new Button("OK");
            btnOK.Clicked += new EventHandler(OnOKClicked); // fixme - never disconnected
            Button btnCancel = new Button("Cancel");
            btnCancel.Clicked += new EventHandler(OnCancelClicked);
            HBox hbxButtons = new HBox(true, 0);
            hbxButtons.PackEnd(btnCancel, false, true, 0);
            hbxButtons.PackEnd(btnOK, false, true, 0);
            Alignment alignButtons = new Alignment(1f, 0f, 0.2f, 0f);            
            alignButtons.Add(hbxButtons);
            lblStatus = new Label("");
            lblStatus.Xalign = 0f;

            // Add Azure frame to primary vbox.
            vboxPrimary.PackStart(frmAzure, false, true, 0);

            // Add results frame to primary vbox.
            vboxPrimary.PackStart(frameResults, false, true, 0);
            vboxPrimary.PackStart(alignButtons, false, true, 0);
            vboxPrimary.PackStart(lblStatus, false, true, 0);

            // Add primary vbox to alignment.
            primaryContainer.Add(vboxPrimary);

            mainWidget = primaryContainer;
        }

        /// <summary>
        /// Invoked when the user clicks the OK button.
        /// </summary>
        public event EventHandler OKClicked;

        /// <summary>
        /// Invoked when the user clicks the Cancel button.
        /// </summary>
        public event EventHandler CancelClicked;

        /// <summary>
        /// Name or description of the job.
        /// </summary>
        public string JobName
        {
            get
            {
                return entryName.Text;
            }
            set
            {
                Application.Invoke(delegate
                {
                    entryName.Text = value;
                });
            }
        }

        /// <summary>
        /// Job submission status.
        /// </summary>
        public string Status
        {
            get
            {
                return lblStatus.Text;
            }
            set
            {
                Application.Invoke(delegate
                {
                    lblStatus.Text = value;
                });
            }
        }

        /// <summary>
        /// Number of VM cores to use to run the job.
        /// </summary>
        public int NumCores
        {
            get
            {
                return int.Parse(comboCoreCount.ActiveText);
            }
        }

        /// <summary>
        /// Send email upon job completion?
        /// </summary>
        public bool SendEmail
        {
            get
            {
                return chkEmail.Active;
            }
        }

        /// <summary>
        /// Email address.
        /// </summary>
        public string EmailAddress
        {
            get
            {
                return entryEmail.Text;
            }
            set
            {
                entryEmail.Text = value;
            }
        }

        /// <summary>
        /// Keep temporary .apsimx files after job submission?
        /// </summary>
        public bool SaveModelFiles
        {
            get
            {
                return chkSaveModels.Active;
            }
        }

        /// <summary>
        /// Optional path to which .apsimx files will be saved.
        /// </summary>
        public string ModelFilePath
        {
            get
            {
                return entryModelPath.Text;
            }
        }

        /// <summary>
        /// Used to specify apsim version - if true, use a directory,
        /// otherwise, use a .zip file.
        /// </summary>
        public bool ApsimFromDirectory
        {
            get
            {
                return radioApsimDir.Active;
            }
        }

        /// <summary>
        /// Specifies path to apsim on disk. Can be .zip or directory.
        /// </summary>
        /// <remarks>Should the distinction between dir/zip occur here?</remarks>
        public string ApsimPath
        {
            get
            {
                return ApsimFromDirectory ? entryApsimDir.Text : entryApsimZip.Text;
            }
        }

        /// <summary>
        /// Iff true, results will be automatically downloaded when the
        /// job finishes.
        /// </summary>
        public bool AutoDownload
        {
            get
            {
                return chkDownload.Active;
            }
        }

        /// <summary>
        /// Path to which results will be downloaded.
        /// </summary>
        public string OutputPath
        {
            get
            {
                return entryOutputDir.Text;
            }
        }

        /// <summary>
        /// Invoked when the user clicks the cancel button. Fires an
        /// event to be handled by the presenter.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCancelClicked(object sender, EventArgs e)
        {
            try
            {
                CancelClicked?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Bundles up the user's settings and sends the data to the presenter to submit the job.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnOKClicked(object sender, EventArgs e)
        {
            try
            {
                OKClicked?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the user changes the radio option for ApsimX
        /// source (zip file, directory, ...). Disables the input
        /// fields associated with the other radio buttons.
        /// </summary>
        private void OnChangeApsimXSource(object sender, EventArgs e)
        {
            try
            {
                entryApsimDir.IsEditable = !radioApsimZip.Active;
                entryApsimDir.Sensitive = !radioApsimZip.Active;
                btnApsimDir.Sensitive = !radioApsimZip.Active;

                entryApsimZip.IsEditable = radioApsimZip.Active;
                entryApsimZip.Sensitive = radioApsimZip.Active;
                btnApsimZip.Sensitive = radioApsimZip.Active;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
        
        /// <summary>
        /// Invoked when the user toggles the 'save model files'
        /// check button. Disables the model path text input.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnToggleSaveModels(object sender, EventArgs e)
        {
            try
            {
                entryModelPath.IsEditable = chkSaveModels.Active;
                entryModelPath.Sensitive = chkSaveModels.Active;
                btnModelPath.Sensitive = chkSaveModels.Active;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the user wants to select an ApsimX .zip file.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnChooseZipFile(object sender, EventArgs args)
        {
            try
            {
                entryApsimZip.Text = AskUserForFileName("Please select a zipped file", Utility.FileDialog.FileActionType.Open, "Zip file (*.zip) | *.zip");
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the user wants to use a file chooser dialog to
        /// select a directory in which to save the generated .apsimx
        /// files.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnChooseDirectory(object sender, EventArgs args)
        {
            try
            {
                entryModelPath.Text = AskUserForFileName("Select a folder", Utility.FileDialog.FileActionType.SelectFolder, string.Empty);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}
