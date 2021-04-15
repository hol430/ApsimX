using System;

namespace UserInterface.Interfaces
{
    internal interface ICancellableDialog : IDisposable
    {
        /// <summary>
        /// Text shown in the main area of the dialog.
        /// </summary>
        string Text { get; set; }

        /// <summary>
        /// Text shown in the dialog's window decorator/title bar.
        /// </summary>
        string Title { get; set; }

        /// <summary>
        /// Called if the dialog is cancelled (ie cancel button is clicked).
        /// </summary>
        event EventHandler Cancelled;

        /// <summary>
        /// Run the dialog (blocks the main thread).
        /// </summary>
        void Run();
    }
}
