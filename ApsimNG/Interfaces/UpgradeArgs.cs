using System;
using UserInterface.Classes;

namespace UserInterface.EventArguments
{
    /// <summary>
    /// Event arguments for an event involving an apsim upgrade.
    /// </summary>
    public class UpgradeArgs : EventArgs
    {
        /// <summary>
        /// The upgrade version.
        /// </summary>
        public Upgrade Version { get; private set; }

        /// <summary>
        /// Constructs an instance of <see cref="UpgradeArgs" />.
        /// </summary>
        /// <param name="version">The upgrade version.</param>
        public UpgradeArgs(Upgrade version) : base()
        {
            Version = version;
        }
    }
}