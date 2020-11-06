using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Models.Soils
{
    /// <summary>
    /// Data structure that holds parameters and variables specific to each pore component in the soil horizon
    /// </summary>
    [Serializable]
    public class HourlyData : Model
    {
        /// <summary>
        /// Irrigation applied
        /// </summary>
        [JsonIgnore]
        public double[] Irrigation { get; set; }
        /// <summary>
        /// Rainfall occured
        /// </summary>
        [JsonIgnore]
        public double[] Rainfall { get; set; }
        /// <summary>
        /// Drainage occured
        /// </summary>
        [JsonIgnore]
        public double[] Drainage { get; set; }
        /// <summary>
        /// Infiltration occured
        /// </summary>
        [JsonIgnore]
        public double[] Infiltration { get; set; }
        /// <summary>
        /// leaching of no3 occured
        /// </summary>
        [JsonIgnore]
        public double[] LeachNO3 { get; set; }
        /// <summary>
        /// leaching of urea occured
        /// </summary>
        [JsonIgnore]
        public double[] LeachUrea { get; set; }
        /// <summary>
        /// Initialise arays on construction
        /// </summary>
        public HourlyData()
        {
            Irrigation = new double[24];
            Rainfall = new double[24];
            Drainage = new double[24];
            Infiltration = new double[24];
            LeachNO3 = new double[24];
            LeachUrea = new double[24];

        }
    }
}
