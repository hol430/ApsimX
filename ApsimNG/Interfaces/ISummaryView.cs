using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EventArguments;

namespace UserInterface.Views
{
    /// <summary>An interface for a summary view.</summary>
    interface ISummaryView
    {
        /// <summary>Occurs when the name of the simulation is changed by the user</summary>
        event EventHandler SimulationNameChanged;

        /// <summary>Gets or sets the currently selected simulation name.</summary>
        string SimulationName { get; set; }

        /// <summary>
        /// List of names of models whose output we want to display.
        /// </summary>
        List<string> SelectedComponentNames { get; }

        /// <summary>
        /// List of names of models whose output we can filter.
        /// </summary>
        List<string> ModelNames { get; set; }

        /// <summary>Gets or sets the simulation names.</summary>
        IEnumerable<string> SimulationNames { get; set; }

        /// <summary>Sets the content of the summary window.</summary>
        /// <param name="content">The html content</param>
        void SetSummaryContent(string content);

        /// <summary>
        /// Invoked when the user wishes to copy data.
        /// This is currently only used on Windows, as the other web 
        /// browsers are capable of handling the copy event themselves.
        /// </summary>
        event EventHandler<CopyEventArgs> Copy;

        /// <summary>
        /// Invoked when the user changes the model on which they wish to filter output.
        /// </summary>
        event EventHandler ModelFilterChanged;
    }
}
