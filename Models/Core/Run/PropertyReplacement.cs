namespace Models.Core.Run
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// This class encapsulates an instruction to replace a property value.
    /// </summary>
    [Serializable]
    public class PropertyReplacement : IReplacement
    {
        /// <summary>
        /// Model path to use to find the model to replace. If null, then
        /// multiple replacements are made using the model name for matching.
        /// </summary>
        private string path;

        /// <summary>The value to Model path to use to find the model to replace.</summary>
        private object replacement;

        /// <summary>The variable and previous value for all replacements made.</summary>
        private List<(IVariable, object)> replacements = new List<(IVariable, object)>();

        /// <summary>Constructor</summary>
        /// <param name="pathOfModel">Model path to use to find the model to replace. If null, then multiple replacements are made using the model name for matching.</param>
        /// <param name="propertyValueReplacement">The value to Model path to use to find the model to replace.</param>
        public PropertyReplacement(string pathOfModel, object propertyValueReplacement)
        {
            path = pathOfModel;
            replacement = propertyValueReplacement;
        }

        /// <summary>Perform the actual replacement.</summary>
        /// <param name="simulation">The simulation to perform the replacements on.</param>
        public void Replace(IModel simulation)
        {
            if (path == null)
                throw new Exception("No path specified for property replacement.");

            var variable = simulation.FindByPath(path);
            if (variable == null)
                throw new Exception($"Unable to apply property replacement: Unable to resolve path '{path}'.");
            replacements.Add((variable, variable.Value));
            variable.Value = replacement;

            // In a multi-paddock context, we want to attempt to
            // change the property value in all paddocks.
            foreach (Zone paddock in simulation.FindAllDescendants<Zone>())
            {
                variable = paddock.FindByPath(path);
                if (variable != null)
                {
                    replacements.Add((variable, variable.Value));
                    variable.Value = replacement;
                }
            }
        }

        /// <summary>
        /// Under the previous replacement.
        /// </summary>
        public void Undo()
        {
            foreach (var replacement in replacements)
                replacement.Item1.Value = replacement.Item2;
        }

        /// <summary>
        /// Check value-equality with another property replacement instance.
        /// </summary>
        /// <param name="obj">The second object instance.</param>
        public override bool Equals(object obj)
        {
            if (obj is PropertyReplacement property)
            {
                if (path != property.path)
                    return false;
                if (replacement == null && property.replacement == null)
                    return true;
                if (replacement == null || property.replacement == null)
                    return false;
                return replacement.Equals(property.replacement);
            }
            return false;
        }

        /// <summary>
        /// Get a hash code for this property replacement instance.
        /// </summary>
        public override int GetHashCode()
        {
            return (path, replacement).GetHashCode();
        }
    }
}
