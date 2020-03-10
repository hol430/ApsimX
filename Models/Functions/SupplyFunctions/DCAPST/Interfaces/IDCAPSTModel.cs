using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Functions.SupplyFunctions.DCAPST
{
    /// <summary>
    /// Describes a photosynthesis model
    /// </summary>
    public interface IDCAPSTModel
    {
        /// <summary>
        /// Calculates the daily photosynthesis
        /// </summary>
        void DailyRun();
    }
}
