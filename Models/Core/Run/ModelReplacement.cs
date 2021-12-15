namespace Models.Core.Run
{
    using Models.Storage;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// This class encapsulates an instruction to replace a model.
    /// </summary>
    [Serializable]
    public class ModelReplacement : IReplacement
    {
        /// <summary>
        /// Model path to use to find the model to replace. If null, then
        /// multiple replacements are made using the model name for matching.
        /// </summary>
        private string path;

        /// <summary>The value to Model path to use to find the model to replace.</summary>
        private IModel replacement;

        /// <summary>A list of existing model and its replacement model for all replacements made.</summary>
        private List<(IModel, IModel)> replacements = new List<(IModel, IModel)>();

        /// <summary>Constructor</summary>
        /// <param name="pathOfModel">Model path to use to find the model to replace. If null, then multiple replacements are made using the model name for matching.</param>
        /// <param name="modelReplacement">The value to Model path to use to find the model to replace.</param>
        public ModelReplacement(string pathOfModel, IModel modelReplacement)
        {
            path = pathOfModel;
            replacement = modelReplacement;
        }

        /// <summary>Perform the actual replacement.</summary>
        /// <param name="simulation">The simulation to perform the replacements on.</param>
        public void Replace(IModel simulation)
        {
            if (path == null)
            {
                // Path will be null when a Replacements node has models under it.
                // Temporarily remove DataStore because we don't want to do any
                // replacements under DataStore.
                DataStore dataStore = simulation.FindChild<DataStore>();
                if (dataStore != null)
                    simulation.Children.Remove(dataStore);

                // Do replacements.
                foreach (IModel match in simulation.FindAllDescendants(replacement.Name).ToList())
                {
                    var replacementModel = Apsim.Clone(replacement);
                    replacements.Add((match, replacementModel));
                    ReplaceModel(match, replacementModel);
                }

                // Reinstate DataStore.
                if (dataStore != null)
                    simulation.Children.Add(dataStore);
            }
            else
            {
                IModel match = simulation.FindByPath(path)?.Value as IModel;
                if (match == null)
                    throw new Exception("Cannot find a model on path: " + path);
                var replacementModel = Apsim.Clone(replacement);
                replacements.Add((match, replacementModel));
                ReplaceModel(match, replacementModel);

                // In a multi-paddock context, we want to attempt to
                // replace the model in all paddocks.
                foreach (IModel paddock in simulation.FindAllDescendants<Zone>().ToList())
                {
                    match = paddock.FindByPath(path)?.Value as IModel;
                    if (match != null)
                    {
                        replacementModel = Apsim.Clone(replacement);
                        replacements.Add((match, replacementModel));
                        ReplaceModel(match, replacementModel);
                    }
                }
            }
        }

        /// <summary>
        /// Under the previous replacement.
        /// </summary>
        public void Undo()
        {
            // Need to undo changes in reverse order, in case any of them
            // overwrite each other.
            foreach (var replacement in replacements.Reverse<(IModel, IModel)>())
                ReplaceModel(replacement.Item2, replacement.Item1);
        }

        /// <summary>Perform the actual replacement.</summary>
        private void ReplaceModel(IModel existingModel, IModel newModel)
        {
            // Fixme - this code should be in Structure.cs.
            int index = existingModel.Parent.Children.IndexOf(existingModel);
            existingModel.Parent.Children.Insert(index, newModel as Model);
            newModel.Parent = existingModel.Parent;
            newModel.Name = existingModel.Name;
            newModel.Enabled = existingModel.Enabled;

            // If a resource model (e.g. maize) is copied into replacements, and its
            // property values changed, these changed values will be overriden with the
            // 'accepted' values from the official maize model when the simulation is
            // run, because the model's resource name is not null. This can be manually
            // rectified by editing the json, but such an intervention shouldn't be
            // necessary.
            if (newModel is ModelCollectionFromResource resourceModel)
                resourceModel.ResourceName = null;

            existingModel.Parent.Children.Remove(existingModel as Model);
            Apsim.ClearCaches(existingModel);

            // Don't call newModel.Parent.OnCreated(), because if we're replacing
            // a child of a resource model, the resource model's OnCreated event
            // will make it reread the resource string and replace this child with
            // the 'official' child from the resource.
            newModel.OnCreated();
            foreach (var model in newModel.FindAllDescendants().ToList())
                model.OnCreated();
        }

        /// <summary>
        /// Determine value-equality to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        public override bool Equals(object obj)
        {
            if (obj is ModelReplacement model)
            {
                return path == model.path && replacement.Equals(model.replacement);
            }
            return false;
        }

        /// <summary>
        /// Get a hash code for this model replacement instance.
        /// Different instance which are equal in value should return
        /// the same hash code.
        /// </summary>
        public override int GetHashCode()
        {
            return (path, replacement).GetHashCode();
        }
    }
}
