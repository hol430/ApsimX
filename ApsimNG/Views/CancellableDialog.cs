using System;
using Gtk;
using UserInterface.Extensions;
using UserInterface.Interfaces;

namespace UserInterface.Views
{
    internal class CancellableDialog : ViewBase, ICancellableDialog, IDisposable
    {
        private Label label;
        private Dialog dialog;

        /// <summary>
        /// Text shown in the main area of the dialog.
        /// </summary>
        public string Text { get => label.Text; set => label.Text = value ?? ""; }

        /// <summary>
        /// Text shown in the dialog's window decorator/title bar.
        /// </summary>
        public string Title { get => dialog.Title; set => dialog.Title = value ?? ""; }

        /// <summary>
        /// Called if the dialog is cancelled (ie cancel button is clicked).
        /// </summary>
        public event EventHandler Cancelled;

        /// <summary>
        /// Creates a CancellableDialog instance.
        /// </summary>
        public CancellableDialog()
        {
            label = new Label();
            dialog = new Dialog();
#if NETCOREAPP
            dialog.ContentArea.PackStart(label, true, true, 5);
#else
            dialog.VBox.PackStart(label, true, true, 5);
#endif
            dialog.AddButton("Cancel", ResponseType.Cancel);
        }

        /// <summary>
        /// Run the dialog (blocks the main thread).
        /// </summary>
        public void Run()
        {
            dialog.ShowAll();
            ResponseType response = (ResponseType)dialog.Run();
            if (response == ResponseType.Cancel)
                Cancelled?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Dispose of any unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            dialog.Remove(label);
            dialog.Cleanup();
            label.Cleanup();
            dialog = null;
            label = null;
        }
    }
}
