namespace Models.Core.Run
{
    /// <summary>An interface for a post simulation tool</summary>
    public interface IPostSimulationTool : IModel
    {
        /// <summary>
        /// Run the post-simulation tool.
        /// </summary>
        void Run();
    }
}
