using System;

namespace UserInterface.Classes
{
    /// <summary>
    /// Represents an APSIM Upgrade/Version.
    /// </summary>
    public class Upgrade
    {
        public DateTime ReleaseDate { get; set; }
        public int IssueNumber { get; set; }
        public string IssueTitle { get; set; }
        public string IssueURL { get; set; }
        public string ReleaseURL { get; set; }
    }
}