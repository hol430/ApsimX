namespace Models.Core.Run
{
    /// <summary>
    /// Defines an interface for performing a replacement of something
    /// in a simulation model.
    /// </summary>
    public interface IReplacement
    {
        /// <summary>Perform the actual replacement.</summary>
        /// <param name="simulation">The simulation to perform the replacements on.</param>
        void Replace(IModel simulation);

        /// <summary>
        /// Revert a simulation to the state it was in before the replacement.
        /// </summary>
        void Undo();
    }

}
